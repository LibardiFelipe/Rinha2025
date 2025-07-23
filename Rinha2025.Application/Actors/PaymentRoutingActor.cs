using Akka.Actor;
using Microsoft.Extensions.Logging;
using Rinha2025.Application.Actors.Base;
using Rinha2025.Domain.Events;
using Rinha2025.Domain.Utils;

namespace Rinha2025.Application.Actors
{
    public sealed class PaymentRoutingActor : TickActor
    {
        private readonly IActorRef _healthMonitorActor;
        private readonly IActorRef _defaultProcessorPool;
        private readonly IActorRef _fallbackProcessorPool;
        private IActorRef? _bestProcessorPool;

        private const int MAX_INTEGRATION_ATTEMPTS = 25;
        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(5);
        private Queue<PaymentReceivedEvent>? _retryQueue;

        public PaymentRoutingActor(
            IActorRef healthMonitorActor,
            IActorRef defaultProcessorPool,
            IActorRef fallbackProcessorPool)
            : base(tickInitialDelay: s_retryInterval, tickInterval: s_retryInterval)
        {
            _healthMonitorActor = healthMonitorActor;
            _defaultProcessorPool = defaultProcessorPool;
            _fallbackProcessorPool = fallbackProcessorPool;

            Receive<HealthUpdatedEvent>(evt =>
                _bestProcessorPool = GetBestProcessor(evt));

            Receive<PaymentReceivedEvent>(evt =>
            {
                if (_bestProcessorPool is null)
                {
                    _retryQueue ??= [];
                    _retryQueue.Enqueue(evt);
                    return;
                }

                _bestProcessorPool.Tell(evt);
            });

            Receive<Result<PaymentReceivedEvent>>(result =>
            {
                if (!result.IsSuccess && result.Content is not null)
                {
                    var evt = result.Content;
                    _retryQueue ??= [];
                    _retryQueue.Enqueue(evt);
                }
            });
        }

        protected override Task TickAsync()
        {
            var count = _retryQueue?.Count ?? 0;
            for (var i = 0; i < count; i++)
            {
                var entry = _retryQueue!.Dequeue();
                if (entry.IntegrationAttempts >= MAX_INTEGRATION_ATTEMPTS)
                {
                    Context.System.DeadLetters.Tell(entry, Self);
                    continue;
                }

                Self.Tell(entry.IncrementIntegrationAttemps());
            }

            return Task.CompletedTask;
        }

        protected override void PreStart()
        {
            _healthMonitorActor.Tell(HealthMonitorActor.NewListener.Instance);
            base.PreStart();
        }

        private IActorRef? GetBestProcessor(HealthUpdatedEvent evt)
        {
            var defaultHealth = evt.DefaultHealth;
            var fallbackHealth = evt.FallbackHealth;

            if (defaultHealth.IsFailing && fallbackHealth.IsFailing)
                return null;

            if (defaultHealth.IsFailing)
                return _fallbackProcessorPool;

            return _defaultProcessorPool;
        }
    }
}

using Akka.Actor;
using MinimalArchitecture.Template.Application.Actors.Base;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Messages;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class PaymentRoutingActor : TickActor
    {
        private readonly IActorRef _healthMonitorActor;
        private readonly IActorRef _defaultProcessorPool;
        private readonly IActorRef _fallbackProcessorPool;
        private IActorRef? _bestProcessorPool;

        private const int MAX_INTEGRATION_ATTEMPTS = 5;
        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(10);
        private Queue<PaymentReceivedEvent>? _retryQueue;

        public PaymentRoutingActor(
            IActorRef healthMonitorActor, IActorRef defaultProcessorPool, IActorRef fallbackProcessorPool)
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

            var fallbackResponseWithOffset = fallbackHealth.MinResponseTime + 150;
            if (defaultHealth.MinResponseTime > fallbackResponseWithOffset)
                return _fallbackProcessorPool;

            return _defaultProcessorPool;
        }
    }
}

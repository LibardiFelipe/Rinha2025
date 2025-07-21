using Akka.Actor;
using Microsoft.Extensions.Logging;
using MinimalArchitecture.Template.Application.Actors.Base;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Messages;
using MinimalArchitecture.Template.Domain.Utils;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class PaymentRoutingActor : TickActor
    {
        private readonly ILogger<PaymentRoutingActor> _logger;
        private readonly IActorRef _healthMonitorActor;
        private readonly IActorRef _defaultProcessorPool;
        private readonly IActorRef _fallbackProcessorPool;
        private IActorRef? _bestProcessorPool;

        private const int MAX_INTEGRATION_ATTEMPTS = 5;
        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(10);
        private Queue<PaymentReceivedEvent>? _retryQueue;

        public PaymentRoutingActor(
            ILogger<PaymentRoutingActor> logger,
            IActorRef healthMonitorActor, IActorRef defaultProcessorPool, IActorRef fallbackProcessorPool)
            : base(tickInitialDelay: s_retryInterval, tickInterval: s_retryInterval)
        {
            _logger = logger;
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

            const int MAX_TOLERABLE_LATENCY_MS = 250;

            _logger.LogError("DefaultMinResponseTime: {Time}", defaultHealth.MinResponseTime);
            _logger.LogError("FallbackMinResponseTime: {Time}", fallbackHealth.MinResponseTime);

            if (defaultHealth.IsFailing || defaultHealth.MinResponseTime > MAX_TOLERABLE_LATENCY_MS)
            {
                if (fallbackHealth.IsFailing)
                    return null;

                return _fallbackProcessorPool;
            }

            var fallbackResponseWithOffset = fallbackHealth.MinResponseTime + 150;
            if (defaultHealth.MinResponseTime > fallbackResponseWithOffset)
                return _fallbackProcessorPool;

            return _defaultProcessorPool;
        }
    }
}

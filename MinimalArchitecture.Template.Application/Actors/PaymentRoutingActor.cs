using Akka.Actor;
using MinimalArchitecture.Template.Application.Actors.Base;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Messages;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed record FailedPaymentEvent(PaymentReceivedEvent Event, int Attempts);

    public sealed class PaymentRoutingActor : TimedActor<bool>
    {
        private readonly IActorRef _healthMonitorActor;
        private readonly IActorRef _defaultProcessorPool;
        private readonly IActorRef _fallbackProcessorPool;
        private IActorRef? _bestProcessorPool;

        private const int MAX_RETRIES = 5;
        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(10);
        private readonly Queue<PaymentReceivedEvent> _retryQueue = [];

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
                    _retryQueue.Enqueue(evt.WithIncreasedAttempts());
                    return;
                }

                _bestProcessorPool.Tell(evt);
            });
        }

        protected override bool Notification => true;

        protected override Task TickAsync()
        {
            var count = _retryQueue.Count;
            for (var i = 0; i < count; i++)
            {
                var entry = _retryQueue.Dequeue();
                if (entry.IntegrationAttempts >= MAX_RETRIES)
                {
                    Context.System.DeadLetters.Tell(entry, Self);
                    continue;
                }

                Self.Tell(entry);
            }

            return Task.CompletedTask;
        }

        protected override void PreStart()
        {
            _healthMonitorActor.Tell(HealthMonitorActor.NewListener.Instance);
            base.PreStart();
        }

        private IActorRef GetBestProcessor(HealthUpdatedEvent @event)
        {
            // TODO: Implementar a lógica real para a seleção
            return @event.DefaultHealth.IsFailing
                ? _fallbackProcessorPool
                : _defaultProcessorPool;
        }
    }
}

using Akka.Actor;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Messages;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class PaymentRoutingActor : ReceiveActor
    {
        private readonly IActorRef _healthMonitorActor;
        private readonly IActorRef _defaultProcessorPool;
        private readonly IActorRef _fallbackProcessorPool;
        private IActorRef? _bestProcessorPool;

        public PaymentRoutingActor(
            IActorRef healthMonitorActor, IActorRef defaultProcessorPool, IActorRef fallbackProcessorPool)
        {
            _healthMonitorActor = healthMonitorActor;
            _defaultProcessorPool = defaultProcessorPool;
            _fallbackProcessorPool = fallbackProcessorPool;

            Receive<HealthUpdatedEvent>(update =>
            {
                // TODO: Melhorar essa lógica
                _bestProcessorPool = update.DefaultHealth.IsFailing
                    ? _fallbackProcessorPool
                    : _defaultProcessorPool;
            });

            Receive<PaymentReceivedEvent>(update =>
                _bestProcessorPool?.Tell(update));
        }

        protected override void PreStart()
        {
            _healthMonitorActor.Tell(HealthMonitorActor.NewListener.Instance);
            base.PreStart();
        }
    }
}

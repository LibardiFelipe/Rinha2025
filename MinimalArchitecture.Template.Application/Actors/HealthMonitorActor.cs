using Akka.Actor;
using MinimalArchitecture.Template.Application.Actors.Base;
using MinimalArchitecture.Template.Domain.Messages;
using MinimalArchitecture.Template.Domain.Models;
using MinimalArchitecture.Template.Domain.Services;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class HealthMonitorActor : TimedActor<HealthUpdatedEvent>
    {
        private readonly IPaymentProcessorService _defaultProcessor;
        private readonly IPaymentProcessorService _fallbackProcessor;

        private ProcessorHealthModel _defaultHealth = ProcessorHealthModel.Failing;
        private ProcessorHealthModel _fallbackHealth = ProcessorHealthModel.Failing;

        public HealthMonitorActor(
            IDefaultPaymentProcessorService defaultProcessor, IFallbackPaymentProcessorService fallbackProcessor)
            : base(tickInitialDelay: TimeSpan.Zero, tickInterval: TimeSpan.FromSeconds(5))
        {
            _defaultProcessor = defaultProcessor;
            _fallbackProcessor = fallbackProcessor;
        }

        protected override HealthUpdatedEvent Notification =>
            new(_defaultHealth, _fallbackHealth);

        protected override void AddListener(IActorRef actorRef)
        {
            base.AddListener(actorRef);

            /* Já notifica o status atual depois de ser adicionado. */
            Sender.Tell(new HealthUpdatedEvent(_defaultHealth, _fallbackHealth));
        }

        protected override async Task TickAsync()
        {
            var defaultHealthTask = _defaultProcessor.GetHealthAsync();
            var fallbackHealthTask = _fallbackProcessor.GetHealthAsync();
            await Task.WhenAll(defaultHealthTask, fallbackHealthTask);

            _defaultHealth = defaultHealthTask.Result;
            _fallbackHealth = defaultHealthTask.Result;

            /* base.TickAsync só deve ser chamado
             * depois que o objeto de notificação
             * estiver pronto para ser enviado. */
            await base.TickAsync();
        }
    }
}

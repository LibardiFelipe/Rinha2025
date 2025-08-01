﻿using Rinha2025.Application.Actors.Base;
using Rinha2025.Domain.Events;
using Rinha2025.Domain.Models;
using Rinha2025.Domain.Services;

namespace Rinha2025.Application.Actors
{
    public sealed class HealthMonitorActor : NotifiableTickActor<HealthUpdatedEvent>
    {
        private readonly IPaymentProcessorService _defaultPaymentProcessor;
        private readonly IPaymentProcessorService _fallbackPaymentProcessor;

        private ProcessorHealthModel _defaultProcessorHealth = ProcessorHealthModel.Failing;
        private ProcessorHealthModel _fallbackProcessorHealth = ProcessorHealthModel.Failing;

        public HealthMonitorActor(
            IDefaultPaymentProcessorService defaultPaymentProcessor,
            IFallbackPaymentProcessorService fallbackPaymentProcessor)
            : base(tickInitialDelay: TimeSpan.Zero, tickInterval: TimeSpan.FromSeconds(5))
        {
            _defaultPaymentProcessor = defaultPaymentProcessor;
            _fallbackPaymentProcessor = fallbackPaymentProcessor;
        }

        protected override HealthUpdatedEvent Notification =>
            new(_defaultProcessorHealth, _fallbackProcessorHealth);

        protected override async Task TickAsync()
        {
            var defaultHealthTask = _defaultPaymentProcessor.GetHealthAsync();
            var fallbackHealthTask = _fallbackPaymentProcessor.GetHealthAsync();
            await Task.WhenAll(defaultHealthTask, fallbackHealthTask);

            _defaultProcessorHealth = defaultHealthTask.Result;
            _fallbackProcessorHealth = defaultHealthTask.Result;

            /* base.TickAsync só deve ser chamado
             * depois que o objeto de notificação
             * estiver pronto para ser enviado.
             * TODO: Pensar em uma solução melhor... */
            await base.TickAsync();
        }
    }
}

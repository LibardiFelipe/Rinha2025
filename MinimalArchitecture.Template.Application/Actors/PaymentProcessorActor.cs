using Akka.Actor;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Services;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class PaymentProcessorActor : ReceiveActor
    {
        private readonly string _processorName;
        private readonly IPaymentProcessorService _paymentProcessor;

        public PaymentProcessorActor(
            string processorName, IPaymentProcessorService paymentProcessor)
        {
            _processorName = processorName;
            _paymentProcessor = paymentProcessor;

            Receive<PaymentReceivedEvent>(evt =>
            {

            });
        }
    }
}

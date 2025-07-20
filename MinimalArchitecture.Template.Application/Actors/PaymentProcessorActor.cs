using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Microsoft.Extensions.DependencyInjection;
using MinimalArchitecture.Template.Domain.Events;
using MinimalArchitecture.Template.Domain.Repositories;
using MinimalArchitecture.Template.Domain.Services;
using MinimalArchitecture.Template.Domain.Utils;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class PaymentProcessorActor : ReceiveActor
    {
        private readonly string _processorName;
        private IActorRef? _routingActor;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentProcessorService _paymentProcessor;

        public PaymentProcessorActor(
            string processorName,
            IServiceProvider serviceProvider,
            IPaymentProcessorService paymentProcessor)
        {
            _processorName = processorName;
            _paymentProcessor = paymentProcessor;

            using var scope = serviceProvider.CreateScope();
            _paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

            Receive<IActorRef>(evt =>
                _routingActor = evt);

            var writer = StartStream();
            Receive<PaymentReceivedEvent>(writer.Tell);

            Receive<Result<PaymentReceivedEvent>>(result =>
            {
                /* Se falhou, lança novamente para o routing */
                if (!result.IsSuccess && result.Content is not null)
                    _routingActor.Tell(result.Content.IncrementProcessingAttemps());
            });
        }

        private IActorRef StartStream()
        {
            var materializer = Context.Materializer();
            var (mainWriter, mainSource) = Source
                .ActorRef<PaymentReceivedEvent>(1000, OverflowStrategy.DropTail)
                .PreMaterialize(materializer);

            var failureSink = Sink.ActorRef<Result<PaymentReceivedEvent>>(
                actorRef: Self,
                onCompleteMessage: PoisonPill.Instance,
                onFailureMessage: ex => Result<PaymentReceivedEvent>.Failure(content: null));

            mainSource
                .SelectAsyncUnordered(
                    parallelism: 50, evt => _paymentProcessor.ProcessAsync(evt, CancellationToken.None))
                .DivertTo(failureSink, result => !result.IsSuccess)
                .GroupedWithin(100, TimeSpan.FromMilliseconds(10))
                .SelectAsync(20, evt => _paymentRepository.InserBatchAsync(evt.Select(e => e.Content)!))
                .To(Sink.Ignore<IEnumerable<PaymentReceivedEvent>>()) // TODO: Tratar problemas no insert?
                .Run(materializer);

            return mainWriter;
        }
    }
}

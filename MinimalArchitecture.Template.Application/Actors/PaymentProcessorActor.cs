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
        private IActorRef? _routingActor;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentProcessorService _paymentProcessor;

        private const int MAX_PROCESSING_ATTEMPTS = 5;

        public PaymentProcessorActor(
            IServiceProvider serviceProvider,
            IPaymentProcessorService paymentProcessor)
        {
            _paymentProcessor = paymentProcessor;

            using var scope = serviceProvider.CreateScope();
            _paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

            var writer = CreateWriterStream();
            Receive<PaymentReceivedEvent>(writer.Tell);

            Receive<Result<PaymentReceivedEvent>>(result =>
            {
                /* Se falhou, lança novamente para o routing */
                if (!result.IsSuccess && result.Content is not null)
                {
                    var entry = result.Content;
                    if (entry.ProcessingAttempts >= MAX_PROCESSING_ATTEMPTS)
                    {
                        Context.System.DeadLetters.Tell(entry, Self);
                        return;
                    }

                    _routingActor?.Tell(
                        Result<PaymentReceivedEvent>.Failure(entry.IncrementProcessingAttemps()));
                }
            });
        }

        protected override void PreStart()
        {
            _routingActor = Context.ActorSelection("/user/routing-pool")
                .ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            base.PreStart();
        }

        private IActorRef CreateWriterStream()
        {
            var materializer = Context.Materializer();
            var (mainWriter, mainSource) = Source
                .ActorRef<PaymentReceivedEvent>(bufferSize: 1000, OverflowStrategy.DropTail)
                .PreMaterialize(materializer);

            var failureSink = Sink.ActorRef<Result<PaymentReceivedEvent>>(
                actorRef: Self,
                onCompleteMessage: PoisonPill.Instance,
                onFailureMessage: ex => Result<PaymentReceivedEvent>.Failure(content: null));

            mainSource
                .SelectAsyncUnordered(parallelism: 50, evt =>
                    _paymentProcessor.ProcessAsync(evt))
                .DivertTo(failureSink, result =>
                    !result.IsSuccess)
                .GroupedWithin(n: 100, TimeSpan.FromMilliseconds(20))
                .SelectAsync(parallelism: 25, evt =>
                    _paymentRepository.InserBatchAsync(evt.Select(e => e.Content)!))
                .To(Sink.Ignore<IEnumerable<PaymentReceivedEvent>>()) // TODO: Tratar problemas no insert?
                .Run(materializer);

            return mainWriter;
        }
    }
}

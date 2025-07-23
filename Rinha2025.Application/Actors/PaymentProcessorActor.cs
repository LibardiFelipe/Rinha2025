using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Microsoft.Extensions.DependencyInjection;
using Rinha2025.Domain.Events;
using Rinha2025.Domain.Repositories;
using Rinha2025.Domain.Services;
using Rinha2025.Domain.Utils;

namespace Rinha2025.Application.Actors
{
    public sealed class PaymentProcessorActor : ReceiveActor
    {
        private IActorRef? _routingActor;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentProcessorService _paymentProcessor;

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
                if (!result.IsSuccess && result.Content is not null)
                {
                    var entry = result.Content;
                    _routingActor?.Tell(
                        Result<PaymentReceivedEvent>.Failure(entry));
                }
            });
        }

        protected override void PreStart()
        {
            _routingActor = Context.ActorSelection("/user/routing-pool")
                .ResolveOne(TimeSpan.FromMinutes(1)).Result;
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
                .SelectAsyncUnordered(parallelism: 16, evt =>
                    _paymentProcessor.ProcessAsync(evt))
                .DivertTo(failureSink, result =>
                    !result.IsSuccess)
                .GroupedWithin(n: 300, TimeSpan.FromMilliseconds(20))
                .SelectAsync(parallelism: 20, evt =>
                    _paymentRepository.InserBatchAsync(evt.Select(e => e.Content)!))
                .To(Sink.Ignore<IEnumerable<PaymentReceivedEvent>>()) // TODO: Tratar problemas no insert?
                .Run(materializer);

            return mainWriter;
        }
    }
}

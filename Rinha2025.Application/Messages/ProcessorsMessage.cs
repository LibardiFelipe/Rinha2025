using Akka.Actor;

namespace Rinha2025.Application.Messages
{
    public sealed class ProcessorsMessage
    {
        public ProcessorsMessage(IActorRef defaultProcessor, IActorRef fallbackProcessor)
        {
            DefaultProcessor = defaultProcessor;
            FallbackProcessor = fallbackProcessor;
        }

        public IActorRef DefaultProcessor { get; init; }
        public IActorRef FallbackProcessor { get; init; }
    }
}

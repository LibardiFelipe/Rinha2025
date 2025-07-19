using Akka.Actor;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class IntegrationActor : ReceiveActor
    {
        private readonly IActorRef _timerActor;

        public IntegrationActor(IActorRef timerActor)
        {
            _timerActor = timerActor;

            Receive<object>(update =>
            {
                Console.WriteLine("Recebi um update.");
            });
        }

        protected override void PreStart()
        {
            _timerActor.Tell(Commands.SubscribeInstance);
            base.PreStart();
        }
    }
}

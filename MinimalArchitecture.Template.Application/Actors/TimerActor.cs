using Akka.Actor;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class TimerActor : ReceiveActor, IWithTimers
    {
        private readonly List<IActorRef> _subscribers = [];

        public TimerActor()
        {
            Receive<Commands.Subscribe>(_ =>
            {
                _subscribers.Add(Sender);
                Sender.Tell(new object());
            });

            ReceiveAsync<Commands.Listen>(async _ =>
            {
                await FetchAsync();
                foreach (var subscriber in _subscribers)
                    subscriber.Tell(new object());
            });
        }

        public ITimerScheduler Timers { get; set; } = null!;

        private static Task FetchAsync()
        {
            return Task.CompletedTask;
        }
    }
}

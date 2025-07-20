using Akka.Actor;

namespace MinimalArchitecture.Template.Application.Actors.Base
{
    public abstract class NotifiableTickActor<TNotificationContent> : ReceiveActor, IWithTimers
    {
        private readonly List<IActorRef> _liteners = [];

        protected NotifiableTickActor(TimeSpan tickInitialDelay, TimeSpan tickInterval)
        {
            Receive<NewListener>(listen =>
                AddListener(Sender));

            ReceiveAsync<Tick>(async _ =>
            {
                await TickAsync();
                foreach (var listener in _liteners)
                    listener.Tell(Notification);
            });

            Timers.StartPeriodicTimer(
                "tick", Tick.Instance, tickInitialDelay, tickInterval);
        }

        protected NotifiableTickActor(TimeSpan tickInterval)
            : this(TimeSpan.Zero, tickInterval)
        { }

        public ITimerScheduler Timers { get; set; } = null!;

        protected virtual void AddListener(IActorRef actorRef)
        {
            _liteners.Add(actorRef);

            /* Já notifica o actor depois de ser adicionado */
            actorRef.Tell(Notification);
        }

        protected virtual Task TickAsync()
        {
            foreach (var listener in _liteners)
                listener.Tell(Notification);
            return Task.CompletedTask;
        }

        protected abstract TNotificationContent Notification { get; }

        public sealed class NewListener
        {
            public NewListener() { }
            public static NewListener Instance => new();
        }

        private sealed class Tick
        {
            public Tick() { }
            public static Tick Instance => new();
        }
    }
}

using Akka.Actor;

namespace Rinha2025.Application.Actors.Base
{
    public abstract class TickActor : ReceiveActor, IWithTimers
    {
        protected TickActor(TimeSpan tickInitialDelay, TimeSpan tickInterval)
        {
            ReceiveAsync<Tick>(async _ =>
                await TickAsync());

            Timers.StartPeriodicTimer(
                "tick", Tick.Instance, tickInitialDelay, tickInterval);
        }

        protected TickActor(TimeSpan tickInterval)
            : this(TimeSpan.Zero, tickInterval)
        { }

        public ITimerScheduler Timers { get; set; } = null!;

        protected abstract Task TickAsync();

        private sealed class Tick
        {
            public Tick() { }
            public static Tick Instance => new();
        }
    }
}

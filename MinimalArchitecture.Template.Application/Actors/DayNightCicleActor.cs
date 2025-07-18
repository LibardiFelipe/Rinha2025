using Akka.Actor;

namespace MinimalArchitecture.Template.Application.Actors
{
    public sealed class DayNightCicleActor : ReceiveActor, IWithTimers
    {
        public ITimerScheduler Timers { get; set; } = null!;
    }
}

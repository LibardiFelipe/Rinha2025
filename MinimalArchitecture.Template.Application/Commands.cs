namespace MinimalArchitecture.Template.Application
{
    public static class Commands
    {
        public sealed record Subscribe();
        public sealed record Listen();

        public static Subscribe SubscribeInstance => new();
        public static Listen ListenInstance => new();
    }
}

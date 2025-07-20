using System.Reflection;

namespace MinimalArchitecture.Template.Infrastructure
{
    public static class Metadata
    {
        public static Assembly Assembly =>
            typeof(Metadata).Assembly;
    }
}

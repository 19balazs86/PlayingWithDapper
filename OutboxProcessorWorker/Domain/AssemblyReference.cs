using System.Reflection;

namespace OutboxProcessorWorker.Domain;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}

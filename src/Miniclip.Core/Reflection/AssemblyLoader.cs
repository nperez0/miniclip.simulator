using System.Reflection;

namespace Miniclip.Core.Reflection;

public static class AssemblyLoader
{
    public static void EnsureReferencedAssembliesLoaded()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
            return;

        foreach (var name in entryAssembly.GetReferencedAssemblies())
            Assembly.Load(name);
    }
}

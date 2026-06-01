using System.Reflection;

namespace Miniclip.Core.Reflection;

public static class AssemblyLoader
{
    public static void EnsureReferencedAssembliesLoaded()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
            return;

        var loaded = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(a => a.FullName)
            .ToHashSet();

        var queue = new Queue<AssemblyName>(entryAssembly.GetReferencedAssemblies());

        while (queue.Count > 0)
        {
            var name = queue.Dequeue();
            if (!loaded.Add(name.FullName))
                continue;

            try
            {
                var assembly = Assembly.Load(name);
                foreach (var reference in assembly.GetReferencedAssemblies())
                    if (!loaded.Contains(reference.FullName))
                        queue.Enqueue(reference);
            }
            catch (Exception)
            {
                // Skip assemblies that cannot be loaded (e.g. native, unavailable)
            }
        }
    }
}

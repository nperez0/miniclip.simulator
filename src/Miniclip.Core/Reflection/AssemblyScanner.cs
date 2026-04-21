namespace Miniclip.Core.Reflection;

public static class AssemblyScanner
{
    public static IEnumerable<Type> GetConcreteTypes() =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

    public static IEnumerable<Type> GetImplementationsOf<TMarker>() =>
        GetConcreteTypes().Where(t => typeof(TMarker).IsAssignableFrom(t));

    public static IEnumerable<(Type ImplementorType, Type[] TypeArguments)> GetClosedImplementationsOf(Type openGenericInterface) =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
                .Select(i => (ImplementorType: t, TypeArguments: i.GetGenericArguments())));
}

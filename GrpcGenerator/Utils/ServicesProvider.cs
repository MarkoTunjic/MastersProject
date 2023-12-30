namespace GrpcGenerator.Utils;

public static class ServicesProvider
{
    private static IServiceProvider? _services;

    public static IServiceProvider GetServices()
    {
        if (_services == null) throw new ArgumentException("Services were not initialized");
        return _services;
    }

    public static void SetServices(IServiceProvider serviceProvider)
    {
        if (_services != null) throw new ArgumentException("Services were already initialized");
        _services = serviceProvider;
    }
}
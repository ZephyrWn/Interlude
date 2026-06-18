namespace Interlude.Infrastructure;

public sealed class ServiceRegistry
{
    private readonly Dictionary<Type, Func<ServiceProvider, object>> _registrations = [];

    public ServiceRegistry AddSingleton<TService>(Func<ServiceProvider, TService> factory)
        where TService : class
    {
        _registrations[typeof(TService)] = provider => factory(provider);
        return this;
    }

    public ServiceProvider Build() => new(_registrations);
}

public sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly Dictionary<Type, Func<ServiceProvider, object>> _registrations;
    private readonly Dictionary<Type, object> _instances = [];
    private bool _disposed;

    public ServiceProvider(Dictionary<Type, Func<ServiceProvider, object>> registrations)
    {
        _registrations = new Dictionary<Type, Func<ServiceProvider, object>>(registrations);
    }

    public object? GetService(Type serviceType)
    {
        if (_instances.TryGetValue(serviceType, out var existing))
        {
            return existing;
        }

        if (!_registrations.TryGetValue(serviceType, out var factory))
        {
            return null;
        }

        var created = factory(this);
        _instances[serviceType] = created;
        return created;
    }

    public TService GetRequiredService<TService>()
        where TService : class
    {
        return (TService)(GetService(typeof(TService))
            ?? throw new InvalidOperationException($"Service {typeof(TService).Name} is not registered."));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var disposable in _instances.Values.OfType<IDisposable>().Reverse())
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}

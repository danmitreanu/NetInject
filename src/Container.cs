using System.Reflection;

namespace NetInject;

public class Container
{
    private readonly Dictionary<Type, Dependency> _registered = new();
    private readonly List<Type> _registeredTypes = new();
    private readonly Dictionary<Type, object> _singletons = new();

    public Container() {}

    public void AddScoped<I, T>() where T : I => Add<I, T>(Life.Scoped);
    public void AddTransient<I, T>() where T : I => Add<I, T>(Life.Transient);
    public void AddSingleton<I, T>() where T : I => Add<I, T>(Life.Singleton);

    public I RequestRequired<I>() where I : class => RequestRequired<I>(new(), new());

    private I RequestRequired<I>(List<Type> types, Dictionary<Type, object> scoped) where I : class
    {
        var instance = RequestRequired(typeof(I), types, scoped) as I;
        if (instance is null)
            throw new NetInjectException($"Could not instantiate dependency {typeof(I).Name}.");

        return instance;
    }

    private object RequestRequired(Type iType, IEnumerable<Type> types, Dictionary<Type, object> scoped)
    {
        if (types.Contains(iType))
            throw new NetInjectException($"Cannot resolve dependency {iType.Name}. Detected circular dependency.");

        types = types.Append(iType);

        if (!_registered.ContainsKey(iType))
           throw new NetInjectException($"Cannot resolve dependency {iType.Name}. It is not registered.");

        var dep = _registered[iType];
        if (dep.InstantiationInfo is null)
        {
            int validCtors = GetInstantiationInfo(dep.Class, out var instInfo);
            dep.InstantiationInfo = instInfo ?? throw new NetInjectException(
                $"Dependency {iType.Name} requires exactly 1 valid constructor (found {validCtors})."
            );
        }

        List<object> instances = new();

        foreach (var depType in dep.InstantiationInfo.Dependencies)
        {
            var subdep = _registered[depType];
            if (dep.Lifetime == Life.Singleton && subdep.Lifetime != Life.Singleton)
                throw new NetInjectException(
                    string.Format(
                        "Initializing singleton {0} with dependency of shorter life {1} is not allowed.",
                        dep.Interface.Name, subdep.Interface.Name
                    )
                );

            switch (subdep.Lifetime)
            {
                case Life.Singleton:
                {
                    if (!_singletons.ContainsKey(subdep.Interface))
                        _singletons.Add(subdep.Interface, RequestRequired(subdep.Interface, types, scoped));

                    instances.Add(_singletons[subdep.Interface]);
                    break;
                }

                case Life.Scoped:
                {
                    if (!scoped.ContainsKey(subdep.Interface))
                        scoped.Add(subdep.Interface, RequestRequired(subdep.Interface, types, scoped));

                    instances.Add(scoped[subdep.Interface]);
                    break;
                }

                case Life.Transient:
                {
                    instances.Add(RequestRequired(subdep.Interface, types, scoped));
                    break;
                }
            }
        }

        return dep.InstantiationInfo.Constructor.Invoke(instances.ToArray());
    }

    private object? Request(Type iType, IEnumerable<Type> types, Dictionary<Type, object> scoped)
    {
        if (types.Contains(iType))
            return null;

        types = types.Append(iType);

        if (!_registered.ContainsKey(iType))
            return null;

        var dep = _registered[iType];
        if (dep.InstantiationInfo is null)
        {
            int validCtors = GetInstantiationInfo(dep.Class, out var instInfo);
            if (validCtors != 1 || instInfo is null)
                return null;

            dep.InstantiationInfo = instInfo;
        }

        List<object> instances = new();

        foreach (var depType in dep.InstantiationInfo.Dependencies)
        {
            var subdep = _registered[depType];
            if (dep.Lifetime == Life.Singleton && subdep.Lifetime != Life.Singleton)
                return null;

            switch (subdep.Lifetime)
            {
                case Life.Singleton:
                {
                    if (!_singletons.ContainsKey(subdep.Interface))
                    {
                        var req = Request(subdep.Interface, types, scoped);
                        if (req is null)
                            return null;

                        _singletons.Add(subdep.Interface, req);
                    }

                    instances.Add(_singletons[subdep.Interface]);
                    break;
                }

                case Life.Scoped:
                {
                    if (!scoped.ContainsKey(subdep.Interface))
                    {
                        var req = Request(subdep.Interface, types, scoped);
                        if (req is null)
                            return null;

                        scoped.Add(subdep.Interface, req);
                    }

                    instances.Add(scoped[subdep.Interface]);
                    break;
                }

                case Life.Transient:
                {
                    var req = Request(subdep.Interface, types, scoped);
                    if (req is null)
                        return null;

                    instances.Add(req);
                    break;
                }
            }
        }

        return dep.InstantiationInfo.Constructor.Invoke(instances.ToArray());
    }

    private void Add<I, T>(Life lifetime) where T : I
    {
        var i = typeof(I);

        if (_registered.Any(r => r.Key == i))
            throw new NetInjectException($"Cannot add dependency {i.Name} because it was already added.");

        _registered.Add(i, new(i, typeof(T), lifetime));
        _registeredTypes.Add(i);
    }

    private bool IsValidConstructor(ConstructorInfo ctor)
    {
        return !ctor.GetParameters().Any(p => !_registeredTypes.Contains(p.ParameterType));
    }

    private int GetInstantiationInfo(Type type, out InstantiationInfo? info)
    {
        var ctors = type.GetConstructors().Where(ctor => IsValidConstructor(ctor));
        int validCtors = ctors.Count();

        if (validCtors != 1)
        {
            info = null;
            return validCtors;
        }

        var ctor = ctors.Single();
        info = new(ctor, ctor.GetParameters().Select(p => p.ParameterType));
        return validCtors;
    }
}
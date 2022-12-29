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

    private I RequestRequired<I>(List<Type> types, Dictionary<Type, object> transients) where I : class
    {
        var instance = RequestRequired(typeof(I), types, transients) as I;
        if (instance is null)
            throw new Exception($"Could not instantiate dependency {typeof(I).Name}.");

        return instance;
    }

    private object RequestRequired(Type iType, IEnumerable<Type> types, Dictionary<Type, object> transients)
    {
        if (types.Contains(iType))
            throw new Exception($"Cannot resolve dependency {iType.Name}. Detected circular dependency.");

        types = types.Append(iType);

        Dependency dep;
        if (!_registered.ContainsKey(iType) || (dep = _registered[iType]).Class is null)
           throw new Exception($"Cannot resolve dependency {iType.Name}. It is not registered.");

        if (dep.InstantiationInfo is null)
            dep.InstantiationInfo = GetInstantiationInfo(dep.Class);

        List<object> instances = new();

        foreach (var depType in dep.InstantiationInfo.Dependencies)
        {
            var subdep = _registered[depType];

            switch (subdep.Lifetime)
            {
                case Life.Singleton:
                {
                    if (!_singletons.ContainsKey(subdep.Interface))
                        _singletons.Add(subdep.Interface, RequestRequired(subdep.Interface, types, transients));

                    instances.Add(_singletons[subdep.Interface]);
                    break;
                }

                case Life.Transient:
                {
                    if (!transients.ContainsKey(subdep.Interface))
                        transients.Add(subdep.Interface, RequestRequired(subdep.Interface, types, transients));

                    instances.Add(transients[subdep.Interface]);
                    break;
                }

                case Life.Scoped:
                {
                    instances.Add(RequestRequired(subdep.Interface, types, transients));
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
            throw new Exception($"Cannot add dependency {i.Name} because it was already added.");

        _registered.Add(i, new(i, typeof(T), lifetime));
        _registeredTypes.Add(i);
    }

    private bool IsValidConstructor(ConstructorInfo ctor)
    {
        return !ctor.GetParameters().Any(p => !_registeredTypes.Contains(p.ParameterType));
    }

    private InstantiationInfo GetInstantiationInfo(Type type)
    {
        var ctors = type.GetConstructors().Where(ctor => IsValidConstructor(ctor));
        int validCtors = ctors.Count();

        if (validCtors == 0)
            throw new Exception($"Cannot find any valid constructor for dependency {type.Name}.");

        if (validCtors != 1)
            throw new Exception($"Found too many valid constructors for dependency {type.Name}.");

        var ctor = ctors.Single();
        return new(ctor, ctor.GetParameters().Select(p => p.ParameterType));
    }
}
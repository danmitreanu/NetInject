# NetInject

A very lightweight .NET dependency injection library

## Usage

First, just create a container and add dependencies to it.

```c#
using NetInject;

Container c = new();
c.AddSingleton<IDependency1, Dependency1>();
c.AddTransient<IDependency2, Dependency2>();
c.AddScoped<IDependency3, Dependency3>();
```

Then, request a dependency and use it:

```c#
var dep1 = c.RequestRequired<IDependency1>();

dep1.DoSomething();
```

---

NetInject works similarly to the Microsoft-provided dependency injection used in ASP.NET Core. Dependencies are requested via an implementation's constructor:

```c#
public interface IDep { int Val { get; } }

public class Dep : IDep
{
    public int Val { get; init; }
    public Dep(ISomeOtherDep someOtherDep) // <- requests ISomeOtherDep
        => Val = someOtherDep.Val;
}

container.AddSingleton<ISomeOtherDep, SomeOtherDep>();
container.AddScoped<IDep, Dep>();
var dep = container.RequestRequired<IDep>();
Console.WriteLine(dep.Val);
```

Also, dependencies' lifetimes are just like in `Microsoft.Extensions.DependencyInjection`.

- Singletons: instantiated only once, when requested for the first time;
- Scoped: instantiated once per request for all dependencies and sub-dependencies;
- Transient: instantiated every time they are requested.
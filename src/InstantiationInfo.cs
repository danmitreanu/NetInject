using System.Reflection;

namespace NetInject;

internal class InstantiationInfo
{
    public IEnumerable<Type> Dependencies { get; set; }
    public ConstructorInfo Constructor { get; set; }

    public InstantiationInfo(ConstructorInfo ctor, IEnumerable<Type> dependencies)
    {
        Constructor = ctor;
        Dependencies = dependencies;
    }
}
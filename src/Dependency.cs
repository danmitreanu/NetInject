namespace NetInject;

internal class Dependency
{
    public Type Interface { get; set; }
    public Type Class { get; set; }
    public Life Lifetime { get; set; }
    public InstantiationInfo? InstantiationInfo { get; set; }

    public Dependency(Type i, Type c, Life lifetime)
    {
        Interface = i;
        Class = c;
        Lifetime = lifetime;
    }
}
namespace test;

public interface IController
{
    string GetResult(string method);
}

public class Controller : IController
{
    private readonly INotify _notify;

    public Controller(INotify notify)
    {
        Console.WriteLine("Controller was instantiated");
        _notify = notify;
    }

    public string GetResult(string method)
    {
        switch (method)
        {
            case "path1": return Path1();
            case "path2": return Path2();
            case "path3": return Path3();
            default: return string.Empty;
        }
    }

    public string Path1()
    {
        _notify.Send("Path1()");
        return "path1 result";
    }

    public string Path2()
    {
        _notify.Send("Path2()");
        return "path2 result";
    }

    public string Path3()
    {
        _notify.Send("Path3()");
        return "path3 results";
    }
}
namespace test;

public interface IRouter
{
    string Route(string path);
}

public class Router : IRouter
{
    private readonly IController _controller;
    private readonly INotify _notify;

    public Router(IController controller, INotify notify)
    {
        Console.WriteLine("Router was instantiated.");

        _controller = controller;
        _notify = notify;
    }

    public string Route(string path)
    {
        _notify.Send($"Routing {path}");
        return _controller.GetResult(path);
    }
}
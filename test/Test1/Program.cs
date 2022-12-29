using NetInject;
using test;

return Main();

int Main()
{
    Container c = new();
    c.AddSingleton<IRouter, Router>();
    c.AddTransient<IController, Controller>();
    c.AddScoped<INotify, Notify>();

    var router = c.RequestRequired<IRouter>();

    Console.WriteLine(router.Route("path1"));
    Console.WriteLine(router.Route("path2"));
    Console.WriteLine(router.Route("path3"));

    return 0;
}
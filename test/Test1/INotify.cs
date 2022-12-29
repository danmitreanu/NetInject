namespace test;

public interface INotify
{
    void Send(string message);
}

public class Notify : INotify
{
    public Notify()
    {
        Console.WriteLine("Notify was instantied");
    }

    public void Send(string message) => Console.WriteLine(message);
}
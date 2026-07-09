

public class ZergRushException : Exception
{
    public ZergRushException()
    {
    }

    public ZergRushException(string? message) : base(message)
    {
    }

    public ZergRushException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
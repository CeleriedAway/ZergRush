using System;

public class JsonSerializationException : Exception
{
    public JsonSerializationException(string message) : base(message)
    {
    }
}
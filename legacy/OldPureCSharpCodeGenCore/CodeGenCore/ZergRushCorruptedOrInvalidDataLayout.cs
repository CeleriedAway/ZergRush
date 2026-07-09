using ZergRush;

public class ZergRushCorruptedOrInvalidDataLayout : ZergRushException
{
    public ZergRushCorruptedOrInvalidDataLayout(string message) : base(message)
    {
    }

    public ZergRushCorruptedOrInvalidDataLayout()
    {
    }
}
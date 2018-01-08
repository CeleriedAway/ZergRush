using System;

#if !NET_4_6

public static class Tuple
{
    public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 second)
    {
        return new Tuple<T1, T2>(item1, second);
    }

    public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 second, T3 third)
    {
        return new Tuple<T1, T2, T3>(item1, second, third);
    }

    public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 second, T3 third, T4 fourth)
    {
        return new Tuple<T1, T2, T3, T4>(item1, second, third, fourth);
    }
}

public struct Tuple<T1, T2>
{
    public readonly T1 Item1;
    public readonly T2 Item2;

    public Tuple(T1 item1, T2 item2)
    {
        this.Item1 = item1;
        this.Item2 = item2;
    }

    public override string ToString()
    {
        return string.Format("Tuple({0}, {1})", Item1, Item2);
    }
}

public struct Tuple<T1, T2, T3>
{
    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;

    public Tuple(T1 item1, T2 item2, T3 item3)
    {
        this.Item1 = item1;
        this.Item2 = item2;
        this.Item3 = item3;
    }

    public override string ToString()
    {
        return string.Format("Tuple({0}, {1}, {2})", Item1, Item2, Item3);
    }
}

public struct Tuple<T1, T2, T3, T4>
{
    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;
    public readonly T4 Item4;

    public Tuple(T1 item1, T2 item2, T3 item3, T4 item4)
    {
        this.Item1 = item1;
        this.Item2 = item2;
        this.Item3 = item3;
        this.Item4 = item4;
    }
    
    public override string ToString()
    {
        return string.Format("Tuple({0}, {1}, {2}, {3})", Item1, Item2, Item3, Item4);
    }
}

#endif

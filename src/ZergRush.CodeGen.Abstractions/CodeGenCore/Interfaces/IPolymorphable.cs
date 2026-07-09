public interface IPolymorphable
{
    ushort GetClassId();
}

public interface ICloneInst
{
    object NewInst();
}

using ZergRush;

public interface ISimpleUpdatableFrom<in T>
{
    void UpdateFrom(T val);
}
public interface IUpdatableFrom<in T>
{
    //void UpdateFrom(T val);
    void UpdateFrom(T val, ZRUpdateFromHelper __helper);
}
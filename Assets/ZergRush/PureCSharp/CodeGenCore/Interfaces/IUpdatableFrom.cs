public interface IUpdatableFrom<in T>
{
    void UpdateFrom(T val);
}
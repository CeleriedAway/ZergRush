namespace ZergRush.Alive
{
    public interface IAddCopyList<T>
    {
        void AddCopy(T item, T refData, ZRUpdateFromHelper __helper);
    }
}
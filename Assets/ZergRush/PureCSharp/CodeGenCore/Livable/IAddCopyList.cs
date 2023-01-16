namespace ZergRush.Alive
{
    public interface IAddCopyList<T>
    {
        void InsertCopy(T item, T refData, ZRUpdateFromHelper __helper, int index);
    }
}
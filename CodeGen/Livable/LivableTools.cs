using System.Collections.Generic;

namespace ZergRush.Alive
{
    public interface IGenericPool
    {
        object PopGeneric();
        void PushGeneric(object obj);
    }
    public class Pool<T> : Stack<T>, IGenericPool where T : class
    {
        public new void Push(T t)
        {
            if (t != null) 
                base.Push(t);
        }

        public object PopGeneric()
        {
            return Pop();
        }

        public void PushGeneric(object obj)
        {
            Push((T) obj);
        }
    }
    
    public partial class ObjectPool
    {
    }

}
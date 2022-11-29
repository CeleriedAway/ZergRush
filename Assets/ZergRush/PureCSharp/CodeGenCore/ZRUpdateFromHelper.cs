using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ZergRush
{
    public class ZRUpdateFromHelper
    {
        ObjectIDGenerator generator = new ObjectIDGenerator();
        Dictionary<long, object> alreadyUpdated = new Dictionary<long, object>();

        public bool TryLoadAlreadyUpdated<T>(T source, ref T target)
        {
            var id = generator.GetId(source, out var firstTime);
            if (firstTime)
            {
                alreadyUpdated[id] = target;
                return false;
            }
            else
            {
                target = (T)alreadyUpdated[id];
                return true;
            }
        }
    }
}
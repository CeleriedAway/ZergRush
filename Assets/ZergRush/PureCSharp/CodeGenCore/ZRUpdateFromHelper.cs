using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ZergRush
{
    public class ZRUpdateFromHelper
    {
        ObjectIDGenerator generator = new ObjectIDGenerator();
        Dictionary<long, object> alreadyUpdated = new Dictionary<long, object>();
        HashSet<object> registeredTargets = new HashSet<object>();

        public bool TryLoadAlreadyUpdated<T>(T source, ref T target) where T : new()
        {
            var id = generator.GetId(source, out var firstTime);
            if (firstTime)
            {
                // there are cases where target is bounded to source instance and then encountered for different source
                // in this case we need to create new target instance to prevent already isud target instance override with different data
                // symptoms of this bug is same instance of object contains in alreadyUpdated dict for different source ids.
                if (registeredTargets.Add(target) == false)
                {
                    target = new T();
                }
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
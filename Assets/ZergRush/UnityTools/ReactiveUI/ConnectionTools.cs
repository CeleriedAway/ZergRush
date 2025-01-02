using UnityEngine;

namespace ZergRush
{
    public static class ConnectionTools
    {
        public static GameObject Instantiate(this Connections connections, GameObject prefab, 
            Transform parent, bool worldPosStay = false)
        {
            var obj = GameObject.Instantiate(prefab, parent, worldPosStay);
            connections += new AnonymousDisposable(() => {
                if (obj) GameObject.Destroy(obj);
            });
            return obj;
        }
        public static T Instantiate<T>(this Connections connections, T prefab, Transform parent, bool worldPosStay = false) where T : Component
        {
            var obj = GameObject.Instantiate(prefab, parent, worldPosStay);
            connections += new AnonymousDisposable(() => {
                if (obj) GameObject.Destroy(obj.gameObject);
            });
            return obj;
        }
        
        public static T Instantiate<T>(this Connections connections, T prefab) where T : Component
        {
            var obj = GameObject.Instantiate(prefab);
            connections += new AnonymousDisposable(() => {
                if (obj) GameObject.Destroy(obj.gameObject);
            });
            return obj;
        }
    }
}
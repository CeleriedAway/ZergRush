using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZergRush.ReactiveUI
{
    public class DistinctivePool<TView, TData> : IViewPool<TView, TData> where TView : ReusableView
    {
        Dictionary<PrefabRef<TView>, ViewPool<TView, TData>> pools = new Dictionary<PrefabRef<TView>, ViewPool<TView, TData>>();
        Func<TData, PrefabRef<TView>> prefabSelector;
        Transform parent;
        Action<TView> instantiateAction;
        List<Func<TView, float>> recycleAction = new List<Func<TView, float>>();
        PresentOptions options;
        
        public DistinctivePool(Transform parent, Func<TData, PrefabRef<TView>> prefabSelector, PresentOptions options)
        {
            this.prefabSelector = prefabSelector;
            this.parent = parent;
            this.options = options;
        }

        public void Preload(PrefabRef<TView> prefabRef)
        {
            Pool(prefabRef);
        }

        ViewPool<TView, TData> Pool(TData data)
        {
            var prefab = prefabSelector(data);
            return Pool(prefab);
        }
        
        ViewPool<TView, TData> Pool(PrefabRef<TView> prefabRef)
        {
            ViewPool<TView, TData> pool;
            if (pools.TryGetValue(prefabRef, out pool) == false)
            {
                var prefab = prefabRef.ExtractPrefab(parent);
                if (prefab == null) return null;
                
                // if we have pool for this concrete prefab already we use that pool
                // and also make this pool default for this prefab key
                if (pools.TryGetValue(prefab, out pool) == false)
                {
                    pool = new ViewPool<TView, TData>(parent, prefab);
                    pools[prefab] = pool;
                }
                pools[prefabRef] = pool;
                
                // TODO implement proper prefab logic here
                if (options.Has(PresentOptions.__UseLoadedViews)) Rui.FillPoolWithChildrenViews(pool, parent, prefabRef, prefab, options);
                recycleAction.ForEach(a => pool.AddRecycleAction(a));
                if (instantiateAction != null) pool.AddInstantiateAction(instantiateAction);
            }
            return pool;
        }
        
        public void EnsurePreloadedViewInstance(TView prefab, int count)
        {
            var pool = Pool(prefab);
            pool.EnsurePreloadedViewInstance(prefab, count);
        }

        public void AddViewToUse(TView prefab, TView view)
        {
            Pool(prefab).AddViewToUse(prefab, view);
        }

        public Vector2 SampleViewSize(TData data)
        {
            var viewPool = Pool(data);
            if (viewPool == null) return Vector2.zero;
            return viewPool.SampleViewSize(data);
        }

        public TView Get(TData data)
        {
            return Pool(data)?.Get(data);
        }

        public void Recycle(TView view, float delay)
        {
            pools[(TView)view.prefabRef].Recycle(view, delay);
        }

        public void AddRecycleAction(Func<TView, float> act)
        {
            recycleAction.Add(act);
            foreach (var pool in pools)
            {
                pool.Value.AddRecycleAction(act);
            }
        }

        public void AddInstantiateAction(Action<TView> act)
        {
            instantiateAction += act;
            foreach (var pool in pools)
            {
                pool.Value.AddInstantiateAction(act);
            }
        }
    }
}
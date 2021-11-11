using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Assertions;

namespace ZergRush.ReactiveUI
{
    public interface IViewPool<TView, TData>
    {
        TView Get(TData data);
        void Recycle(TView view, float delay);
        void AddRecycleAction(Func<TView, float> act);
        void AddInstantiateAction(Action<TView> act);
        void EnsurePreloadedViewInstance(TView prefab, int count);
        void AddViewToUse(TView prefab, TView view);
        Vector2 sampleViewSize(TData data);
    }

    public class ViewPoolProxyMap<TView, TData, TData2> : IViewPool<TView, TData>
    {
        public IViewPool<TView, TData2> viewPoolImplementation;
        public Func<TData, TData2> map;
        public TView Get(TData data)
        {
            return viewPoolImplementation.Get(map(data));
        }

        public void Recycle(TView view, float delay)
        {
            viewPoolImplementation.Recycle(view, delay);
        }

        public void AddRecycleAction(Func<TView, float> act)
        {
            viewPoolImplementation.AddRecycleAction(act);
        }

        public void AddInstantiateAction(Action<TView> act)
        {
            viewPoolImplementation.AddInstantiateAction(act);
        }

        public void EnsurePreloadedViewInstance(TView prefab, int count)
        {
            viewPoolImplementation.EnsurePreloadedViewInstance(prefab, count);
        }

        public void AddViewToUse(TView prefab, TView view)
        {
            viewPoolImplementation.AddViewToUse(prefab, view);
        }

        public Vector2 sampleViewSize(TData data)
        {
            return viewPoolImplementation.sampleViewSize(map(data));
        }
    }

    public class SimpleViewPool<TView> : ViewPool<TView, int> where TView : ReusableView
    {
        public TView Get() => Get(0);

        public SimpleViewPool(Transform parent, TView prefab) : base(parent, prefab)
        {
        }

        public SimpleViewPool(Transform parent, PrefabRef<TView> prefab, PresentOptions options) : base(parent, prefab, options)
        {
        }
    }

    public class ViewPool<TView, TData> : IViewPool<TView, TData>
        where TView : ReusableView
    {
        readonly List<TView> pool;
        readonly Transform parent;
        readonly TView prefab;
        Action<TView> instantiateAction;
        List<Func<TView, float>> recycleAction = new List<Func<TView, float>>();

        public ViewPool(Transform parent, TView prefab)
        {
            if (prefab == null) throw new ZergRushException($"prefab is null");
            this.parent = parent;
            this.prefab = prefab;
            pool = new List<TView>();
        }

        public ViewPool(Transform parent, PrefabRef<TView> prefab, PresentOptions options) : this(parent, prefab.ExtractPrefab(parent))
        {
            if ((options & PresentOptions.UseLoadedViews) != 0) Rui.FillPoolWithChildrenViews(this, parent, prefab, this.prefab, options);
        }

        public int Count => pool.Count;

        public void Clear()
        {
            pool.ForEach(v => GameObject.Destroy(v.gameObject));
            pool.Clear();
        }

        public void HideAll()
        {
            pool.ForEach(v => v.gameObject.SetActiveSafe(false));
        }

        public void AddInstantiateAction(Action<TView> action)
        {
            instantiateAction += action;
        }

        public void EnsurePreloadedViewInstance(TView prefab1, int count)
        {
            Assert.IsTrue(prefab1 == this.prefab);
            while (pool.Count < count)
            {
                pool.Add(Instantiate());
            }
        }

        public void AddViewToUse(TView p, TView view)
        {
            view.prefabRef = p;
            Recycle(view);
            //pool.Add(view);
        }

        public Vector2 sampleViewSize(TData data)
        {
            return prefab.rectTransform.rect.size;
        }

        public void AddRecycleAction(Func<TView, float> action)
        {
            recycleAction.Add(action);
        }

        protected TView Instantiate()
        {
            var obj = GameObject.Instantiate(prefab, parent, false);
            if (instantiateAction != null)
            {
                instantiateAction(obj);
            }
            var view = obj.GetComponent<TView>();
            view.prefabRef = prefab;
            return view;
        }

        public TView Get(TData data)
        {
            TView view;
            if (pool.Count > 0)
            {
                view = pool.TakeLast();
            }
            else
            {
                view = Instantiate();
            }
            view.OnBeforeUsed();
            if (view.autoDisableOnRecycle)
                view.gameObject.SetActive(true);
            return view;
        }

        public void Recycle(TView view, float delay)
        {
            if (recycleAction != null)
            {
                foreach (var func in recycleAction)
                {
                    var value = func(view);
                    delay = Mathf.Max(delay, value);
                }
            }
            if (delay == 0)
            {
                Recycle(view);
                return;
            }
            else
            {
                view.ExecuteWithDelay(delay, () =>
                {
                    view.DisconnectAll();
                    Recycle(view);
                });
            }
        }

        void Recycle(TView view)
        {
            if (view == null) return;
            
            view.OnRecycle();
            view.DisconnectAll();
            view.currentMoveAnimation.DisconnectSafe();            
            if (view.autoDisableOnRecycle)
                view.gameObject.SetActive(false);
            if (view.setImpossiblePositionOnRecycle)
                view.rectTransform.anchoredPosition = new Vector2(0xffff, 0xffff);
            pool.Add(view);
        }
    }
}

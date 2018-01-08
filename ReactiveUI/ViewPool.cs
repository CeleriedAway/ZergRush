#if UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZergRush.ReactiveUI
{
    public class ViewPool<TView> where TView : ReusableView
    {
        readonly List<TView> pool;
        readonly Transform parent;
        readonly TView prefab;
        Action<TView> instantiateAction;
        Action<TView> recycleAction;

        public ViewPool(Transform parent, TView prefab)
        {
            this.parent = parent;
            this.prefab = prefab;
            pool = new List<TView>();
        }

        public void AddInstantiateAction(Action<TView> action)
        {
            instantiateAction += action;
        }
        public void AddRecycleAction(Action<TView> action)
        {
            recycleAction += action;
        }

        public TView Get()
        {
            if (pool.Count > 0) return pool.TakeLast();
            var obj = GameObject.Instantiate(prefab, parent, false);
            if (instantiateAction != null)
            {
                instantiateAction(obj);
            }
            return obj.GetComponent<TView>();
        }

        public void Recycle(TView view, float delay)
        {
            if (delay == 0)
            {
                Recycle(view);
                return;
            }
            view.DisconnectAll();
            view.ExecuteWithDelay(delay, () => Recycle(view));
        }

        public void Recycle(TView view)
        {
            view.DisconnectAll();
            view.currentMoveAnimation.DisconnectSafe();
            view.rectTransform.localScale = Vector3.one;
            view.rectTransform.anchoredPosition = new Vector2(0xffff, 0xffff);
            if (recycleAction != null) recycleAction(view);
            pool.Add(view);
        }
    }

    public class LinearViewStorage<TView> where TView : ReusableView
    {
        public ViewPool<TView> pool;
        List<TView> loadedViews = new List<TView>();
        int firstLoadedIndex;

        int lastLoadedIndex { get { return firstLoadedIndex + loadedViews.Count - 1; } }

        public void EnsureLoadedInterval(int indexStart, int indexFinish, Action<TView, int> onViewLoaded, Func<TView, float> onViewUnload)
        {
            int indexUsedFirst = firstLoadedIndex;
            int indexUsedLast = lastLoadedIndex;

            /* Loaded cells completely out of sight. */
            if (loadedViews.Count == 0 || indexStart > indexUsedLast || indexFinish < indexUsedFirst)
            {
                ReloadAll(indexStart, indexFinish, onViewLoaded, onViewUnload);
                return;
            }

            if (indexStart == indexUsedFirst && indexFinish == indexUsedLast) return;

            /* Dealing with left side. */
            if (indexUsedFirst < indexStart)
            {
                for (int i = indexUsedFirst; i < indexStart; i++)
                {
                    RecycleAtLoadedIndex(0, onViewUnload);
                }
            }
            else if (indexUsedFirst > indexStart)
            {
                for (int i = indexStart; i < indexUsedFirst; i++)
                {
                    var view = pool.Get();
                    loadedViews.Insert(i - indexStart, view);
                    onViewLoaded(view, i);
                }
            }
            firstLoadedIndex = indexStart;

            /* Dealing with right side. */
            if (indexUsedLast > indexFinish)
            {
                for (int i = indexUsedLast; i > indexFinish; i--)
                {
                    RecycleAtLoadedIndex(loadedViews.Count - 1, onViewUnload);
                }
            }
            else if (indexFinish > indexUsedLast)
            {
                for (int i = indexUsedLast + 1; i <= indexFinish; i++)
                {
                    var view = pool.Get();
                    loadedViews.Add(view);
                    onViewLoaded(view, i);
                }
            }
        }

        void RecycleAtLoadedIndex(int index, Func<TView, float> onRecycle)
        {
            var view = loadedViews[index];
            if (onRecycle == null) pool.Recycle(view);
            else pool.Recycle(view, onRecycle(view));
            loadedViews.RemoveAt(index);
        }

        void ReloadAll(int indexStart, int indexFinish, Action<TView, int> onViewLoaded, Func<TView, float> onViewUnloaded)
        {
            UnloadAll(onViewUnloaded);
            firstLoadedIndex = indexStart;
            for (int i = indexStart; i <= indexFinish; i++)
            {
                var view = pool.Get();
                onViewLoaded(view, i);
                loadedViews.Add(view); 
            }
        }

        public bool IsLoaded(int index)
        {
            return loadedViews.Count != 0 && index >= firstLoadedIndex && index <= lastLoadedIndex;
        }

        public TView ViewAt(int index)
        {
            return loadedViews[index - firstLoadedIndex];
        }

        public void UnloadAll(Func<TView, float> onRecycle)
        {
            if (onRecycle != null)
            {
                foreach (var loadedView in loadedViews)
                {
                    pool.Recycle(loadedView, onRecycle(loadedView));
                }
            }
            else
            {
                foreach (var loadedView in loadedViews)
                {
                    pool.Recycle(loadedView);
                }
            }
            loadedViews.Clear();
        }

        public void PierceIndex(int index, Func<TView, float> onUnload)
        {
            if (index < firstLoadedIndex) firstLoadedIndex--;
            else if (index <= lastLoadedIndex)
            {
                RecycleAtLoadedIndex(index - firstLoadedIndex, onUnload);
            }
        }

        public void ReplaceIndex(int index, Action<TView, int> onViewCreate, Func<TView, float> onUnload)
        {
            if (index >= firstLoadedIndex && index <= lastLoadedIndex)
            {
                var view = loadedViews[index - firstLoadedIndex];
                if (onUnload != null)
                    pool.Recycle(view, onUnload(view));
                else
                    pool.Recycle(view);
                view = pool.Get();
                loadedViews[index - firstLoadedIndex] = view;
                if (onViewCreate != null)
                    onViewCreate(view, index);
            }
        }

        public void InjectAtIndex(int index, Action<TView, int> onViewCreate)
        {
            if (index < firstLoadedIndex) firstLoadedIndex++;
            else if (index <= lastLoadedIndex)
            {
                var view = pool.Get();
                if (onViewCreate != null)
                    onViewCreate(view, index);
                loadedViews.Insert(index - firstLoadedIndex, view);
            }
        }

        public void ForEachViewAfterIndex(int index, Action<TView, int> action)
        {
            for (int i = Mathf.Max(firstLoadedIndex, index); i <= lastLoadedIndex; i++)
            {
                action(ViewAt(i), i);
            }
        }

        public void ForEachLoadedView(Action<TView, int> action)
        {
            for (int i = 0; i < loadedViews.Count; i++)
            {
                action(loadedViews[i], i + firstLoadedIndex);
            }
        }
    }
}
#endif

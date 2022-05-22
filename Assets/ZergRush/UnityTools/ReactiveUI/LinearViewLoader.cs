using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZergRush.ReactiveUI
{
    // Takes pool and load views from data list and arrange them linear
    // Also posses knowledge about loaded interval for table view with reusable cells
    public class LinearViewLoader<TView, TData> where TView : ReusableView
    {
        public IViewPool<TView, TData> pool;
        protected List<TView> loadedViews = new List<TView>();
        int firstLoadedIndex;
        public Action<TData, TView> showAction;
        public Func<TView, float> unloadAction;

        public LinearViewLoader(IViewPool<TView, TData> pool, Action<TData, TView> show, Func<TView, float> unLoad = null)
        {
            this.pool = pool;
            this.unloadAction = unLoad; 
            this.showAction = show;
        }

        public List<TView> Views()
        {
            return loadedViews;
        }

        int lastLoadedIndex { get { return firstLoadedIndex + loadedViews.Count - 1; } }

        public void EnsureLoadedInterval(int indexStart, int indexFinish, IReadOnlyList<TData> data)
        {
            int indexUsedFirst = firstLoadedIndex;
            int indexUsedLast = lastLoadedIndex;

            /* Loaded cells completely out of sight. */
            if (loadedViews.Count == 0 || indexStart > indexUsedLast || indexFinish < indexUsedFirst)
            {
                ReloadAll(indexStart, indexFinish, data);
                return;
            }

            if (indexStart == indexUsedFirst && indexFinish == indexUsedLast) return;

            /* Dealing with left side. */
            if (indexUsedFirst < indexStart)
            {
                for (int i = indexUsedFirst; i < indexStart; i++)
                {
                    UnloadView(i);
                    firstLoadedIndex++;
                }
            }
            else if (indexUsedFirst > indexStart)
            {
                for (int i = indexUsedFirst - 1; i >= indexStart; i--)
                {
                    firstLoadedIndex--;
                    LoadView(i, data[i]);
                }
            }
            firstLoadedIndex = indexStart;

            /* Dealing with right side. */
            if (indexUsedLast > indexFinish)
            {
                for (int i = indexUsedLast; i > indexFinish; i--)
                {
                    UnloadView(i);
                }
            }
            else if (indexFinish > indexUsedLast)
            {
                for (int i = indexUsedLast + 1; i <= indexFinish; i++)
                {
                    LoadView(i, data[i]);
                }
            }
        }

        public TView LoadView(int index, TData data)
        {
            var view = pool.Get(data);
            view.indexInModel = index;
            //Debug.Log($"loading view {view.GetInstanceID()} at index {index}");
            var i = index - firstLoadedIndex;
            loadedViews.Insert(i, view);
            //Debug.Log($"loading {view.GetHashCode()} at {index}");
            showAction?.Invoke(data, view);
            return view;
        }

        public void UnloadView(int dataIndex)
        {
            var view = loadedViews[dataIndex - firstLoadedIndex];
            //Debug.Log($"unload view {view.GetInstanceID()} at index {dataIndex}");
            pool.Recycle(view, unloadAction == null ? 0 : unloadAction(view));
            //Debug.Log($"unloading {view.GetHashCode()} at {dataIndex}");
            loadedViews.RemoveAt(dataIndex - firstLoadedIndex);
        }

        public void ReloadAll(IEnumerable<TData> data)
        {
            UnloadAll(false);
            var d = data.ToList();
            for (int i = 0; i < d.Count; i++)
            {
                LoadView(i, d[i]);
            }
        }

        public void ReloadAll(int indexStart, int indexFinish, IReadOnlyList<TData> data)
        {
            UnloadAll(false);
            firstLoadedIndex = indexStart;
            for (int i = indexStart; i <= indexFinish; i++)
            {
                LoadView(i, data[i]);
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

        public void UnloadAll(bool instant)
        {
            foreach (var loadedView in loadedViews)
            {
                //Debug.Log($"unload view {loadedView.GetInstanceID()} at index {loadedView.indexInModel}");
                pool.Recycle(loadedView, instant || unloadAction == null ? 0 : unloadAction(loadedView));
            }
            loadedViews.Clear();
            firstLoadedIndex = 0;
        }

        public void PierceIndexIfLoaded(int index)
        {
            if (index < firstLoadedIndex) firstLoadedIndex--;
            else if (index <= lastLoadedIndex)
            {
                UnloadView(index);
            }
        }

        public void ReplaceIndexIfLoaded(int index, TData data)
        {
            if (index >= firstLoadedIndex && index <= lastLoadedIndex)
            {
                UnloadView(index);
                LoadView(index, data);
            }
        }

        public void InjectAtIndexIfLoaded(int index, TData data)
        {
            if (index < firstLoadedIndex) firstLoadedIndex++;
            else if (index <= lastLoadedIndex)
            {
                LoadView(index, data);
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
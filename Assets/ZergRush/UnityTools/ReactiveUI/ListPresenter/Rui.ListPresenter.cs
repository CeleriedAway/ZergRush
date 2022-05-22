using System;
using System.Collections.Generic;
using UnityEngine;
using ZergRush.ReactiveCore;
using Object = UnityEngine.Object;

namespace ZergRush.ReactiveUI
{
    public sealed class ListPresenter<TData, TView> : LinearViewLoader<TView, TData>
        where TView : ReusableView, IUpdatableFrom<TData>
    {
        public ListPresenter(
            Func<TData, PrefabRef<TView>> prefabSelector,
            Transform parent,
            Action<TData, TView> onShow = null,
            Func<TView, float> unload = null,
            Func<TData, int> visualIdGetter = null,
            Func<TData, bool> needView = null,
            bool updateOnShow = true,
            bool disableOnRecycle = true
        )
            : this(new DistinctivePool<TView, TData>(parent, prefabSelector, PresentOptions.None), onShow, unload, visualIdGetter, needView, updateOnShow, disableOnRecycle)
        {
        }
        public ListPresenter(
            TView prefab,
            Transform parent,
            Action<TData, TView> onShow = null,
            Func<TView, float> unload = null,
            Func<TData, int> visualIdGetter = null,
            Func<TData, bool> needView = null,
            bool updateOnShow = true,
            bool disableOnRecycle = true
        )
            : this(new ViewPool<TView, TData>(parent, prefab), onShow, unload, visualIdGetter, needView, updateOnShow, disableOnRecycle)
        {
        }

        public ListPresenter(IViewPool<TView, TData> pool,
            Action<TData, TView> onShow = null,
            Func<TView, float> unload = null,
            Func<TData, int> visualIdGetter = null,
            Func<TData, bool> needView = null,
            bool updateOnShow = true,
            bool disableOnRecycle = true
            )
            : base(pool, onShow, unload)
        {
            if (visualIdGetter == null && typeof(IPresentableData).IsAssignableFrom(typeof(TData)))
            {
                this.visualIdGetter = data => ((IPresentableData)data).GetVisualId();
            }
            else
            {
                this.visualIdGetter = visualIdGetter;
            }

            this.updateOnShow = updateOnShow;

            this.needView = needView;

            //            if (disableOnRecycle) pool.AddRecycleAction(v =>
            //            {
            //                v.SetActiveSafe(false);
            //                return 0;
            //            });
        }

        bool updateOnShow;
        Func<TData, int> visualIdGetter;
        Func<TData, bool> needView;

        List<int> loadedViewInfos = new List<int>();

        public TView ViewWithIndex(int index)
        {
            if (index >= loadedViews.Count) return null;
            return loadedViews[index];
        }

        public TView FindByID(int visualId)
        {
            for (var index = 0; index < loadedViews.Count; index++)
            {
                var v = loadedViewInfos[index];
                if (v == visualId)
                    return loadedViews[index];
            }

            return null;
        }

        List<TData> temp = new List<TData>();

        // That is may be an optimization
        List<TData> ToTemp(IEnumerable<TData> data)
        {
            temp.Clear();
            foreach (var data1 in data)
            {
                if (needView == null || needView(data1))
                    temp.Add(data1);
            }
            return temp;
        }

        public void UpdateFrom(IEnumerable<TData> dataEnumerable)
        {
            var canUseData = needView == null && dataEnumerable is IReadOnlyList<TData>;
            var data = canUseData ? (IReadOnlyList<TData>)dataEnumerable : ToTemp(dataEnumerable);

            int iView = 0, iData = 0;
            while (iData < data.Count && iView < loadedViews.Count)
            {
                var d = data[iData];
                if (visualIdGetter == null || loadedViewInfos[iView] == visualIdGetter(d))
                {
                    UpdateViewFromData(loadedViews[iView], d);
                    iView++;
                    iData++;
                }
                else
                {
                    UnloadView(iView);
                    if (visualIdGetter != null) { loadedViewInfos.RemoveAt(iView); }
                }
            }

            for (int i = loadedViews.Count - 1; i >= data.Count; i--)
            {
                UnloadView(i);
                if (visualIdGetter != null) { loadedViewInfos.RemoveAt(i); }
            }

            for (int i = loadedViews.Count; i < data.Count; i++)
            {
                TView view = LoadView(i, data[i]);
                if (updateOnShow) UpdateViewFromData(view, data[i]);
                if (visualIdGetter != null) { loadedViewInfos.Insert(i, visualIdGetter(data[i])); }
            }
        }

        void UpdateViewFromData(TView view, TData data)
        {
            try
            {
                view.UpdateFrom(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"exception during update {typeof(TView)}: {e.Message}\n{e.StackTrace}");
            }
        }

        int VisualId(TData data)
        {
            return visualIdGetter != null ? visualIdGetter(data) : 0;
        }
    }
}
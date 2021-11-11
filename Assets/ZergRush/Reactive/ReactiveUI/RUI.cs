using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using ZergRush.ReactiveCore;

namespace ZergRush.ReactiveUI
{
    [Flags, Serializable]
    public enum PresentOptions
    {
        None = 0,
        PreserveSiblingOrder = 1,
        UseLoadedViews = 2,
        UseChildWithSameTypeAsView = 4 | UseLoadedViews,
        UseChildWithSameNameAsView = 8 | UseLoadedViews,
        NeedLayout = 16
    }
    
    // You need to dispose this when table view is done.
    public class SimplePresentComponents<TView, TData> : Connections
        where TView : ReusableView
    {
        public IReactiveCollection<TData> collection;
        public LinearViewLoader<TView, TData> viewLoader;
        public IScrollViewLayout layout;
        public TableDelegates<TView> delegates;
        public PresentOptions options;
        public bool animationsEnabled;

        public SimplePresentComponents<TView, TData> GetPool(out IViewPool<TView, TData> pool)
        {
            pool = viewLoader.pool;
            return this;
        }

        public override void Dispose()
        {
            base.Dispose();
            layout?.Dispose();
        }
    }

    // You need to dispose this when table view is done.
    public class TableConnectionsAndComponents<TView, TData> : SimplePresentComponents<TView, TData>
        where TView : ReusableView
    {
        public IViewPort viewPort;
    }

    public static partial class Rui
    {
        public static bool Has(this PresentOptions self, PresentOptions option)
        {
            return (self & option) == option;
        }
        static void SetAnchorPositionInASaneWay(this RectTransform rt, Vector2 pos)
        {
            rt.anchoredPosition = pos;
        }

        static TableConnectionsAndComponents<TView, TData> ControlItemVisibilityAndRecycle<TView, TData>(
            TableConnectionsAndComponents<TView, TData> components)
            where TView : ReusableView
        {
            var viewStorage = components.viewLoader;
            var viewPort = components.viewPort;
            var layout = components.layout;
            var delegates = components.delegates ?? new TableDelegates<TView>();
            var collection = components.collection;

            components.addConnection = new AnonymousDisposable(() => { viewStorage.UnloadAll(true); });
            components.addConnection = components.collection.CountCell().Bind(c =>
            {
                layout.count = c;
                layout.RefreshSize();
            });

            // And show actions to set layouted position and show appear animations if any
            Action<TData, TView> setLayoutPos = (data, view) =>
            {
                //Debug.Log($"pos for index {view.indexInModel}, view {view.GetHashCode()}");
                layout.CorrectViewAnchors(view);
                view.rectTransform.SetAnchorPositionInASaneWay(layout.AncoredPositionForIndex(view.indexInModel));
            };
            setLayoutPos += viewStorage.showAction;
            viewStorage.showAction = setLayoutPos;

            // Refresh loaded view intervals during scrolling
            Action ensureVisibleViewsAreLoaded = () =>
            {
                var count = components.collection.Count;
                if (count == 0) return;
                int firstVisible, lastVisible;
                viewPort.CalculateVisibleIndexes(layout, out firstVisible, out lastVisible);
                if (firstVisible == -1)
                {
                    viewStorage.UnloadAll(true);
                }
                else
                {
                    lastVisible = Mathf.Min(lastVisible, count - 1);
                    viewStorage.EnsureLoadedInterval(firstVisible, lastVisible, collection);
                }
            };

            Action updateLayout = () =>
            {
                viewStorage.ForEachLoadedView((view, i) =>
                {
                    var newPos = layout.AncoredPositionForIndex(i);
                    if (view.rectTransform.anchoredPosition == newPos) return;
                    if (delegates.moveAnimation != null)
                    {
                        view.currentMoveAnimation.DisconnectSafe();
                        view.currentMoveAnimation = delegates.moveAnimation(view, newPos);
                    }
                    else
                    {
                        view.rectTransform.SetAnchorPositionInASaneWay(newPos);
                    }
                });
            };

            components.addConnection = layout.needUpdate.Subscribe(updateLayout);
            components.addConnection = layout.topShift.OnChanged(updateLayout);
            components.addConnection = viewPort.needRecalcVisibility.Subscribe(() =>
            {
                components.animationsEnabled = false;
                ensureVisibleViewsAreLoaded();
            });
            components.addConnection = collection.update.Subscribe(e =>
            {
                components.animationsEnabled = true;
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Insert:
                        viewStorage.InjectAtIndexIfLoaded(e.position, e.newItem);
                        ensureVisibleViewsAreLoaded();
                        // TODO correct reusable view index
                        updateLayout();
                        break;
                    case ReactiveCollectionEventType.Remove:
                        viewStorage.PierceIndexIfLoaded(e.position);
                        ensureVisibleViewsAreLoaded();
                        // TODO correct reusable view index
                        updateLayout();
                        break;
                    case ReactiveCollectionEventType.Set:
                        viewStorage.ReplaceIndexIfLoaded(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Reset:
                        viewStorage.UnloadAll(false);
                        ensureVisibleViewsAreLoaded();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

            components.animationsEnabled = false;
            ensureVisibleViewsAreLoaded();
            return components;
        }

        public static TableConnectionsAndComponents<TView, TData> PresentInScrollWithLayout<TView, TData>(
            TableConnectionsAndComponents<TView, TData> connectionsAndComponents,
            ReactiveScrollRect scroll)
            where TView : ReusableView
        {
            AdjustScrollRectContentAnchors(scroll.scroll, scroll.scroll.horizontal);
            connectionsAndComponents.viewPort = new ScrollRectViewPort(scroll, connectionsAndComponents.layout,
                connectionsAndComponents.connectionSink);
            return ControlItemVisibilityAndRecycle(connectionsAndComponents);
        }

        // Creates everithing except viewport.
        public static TableConnectionsAndComponents<TView, TData> CreateBasicTableComponents<TData, TView>(
            IReactiveCollection<TData> data,
            Transform parent,
            Action<TData, TView> show,
            IViewPool<TView, TData> pool = null, 
            PrefabRef<TView> prefab = default,
            Func<TData, PrefabRef<TView>> prefabSelector = null,
            IScrollViewLayout layout = null, // Linear layout is default
            TableDelegates<TView> delegates = null,
            Func<TData, IEventStream> updater = null,
            PresentOptions options = PresentOptions.None
                ) where TView : ReusableView
        {
            var components = new TableConnectionsAndComponents<TView, TData>();
            
            if (options.Has(PresentOptions.NeedLayout) && layout == null) layout = LinearLayout();
            components.layout = layout;
            
            if (pool == null)
            {
                if (prefabSelector != null)
                {
                    pool = new DistinctivePool<TView, TData>(parent, prefabSelector, options);
                }
                else
                {
                    var actualPrefab = prefab.ExtractPrefab(parent);
                    layout?.UpdatePrefabSizeInfo(actualPrefab.GetComponent<RectTransform>().rect.size);
                    pool = new ViewPool<TView, TData>(parent, actualPrefab);
                    if (options.Has(PresentOptions.UseLoadedViews))
                    {
                        FillPoolWithChildrenViews(pool, parent, prefab, actualPrefab, options);
                    }
                }
            }
            
            if (updater != null)
            {
                var showCopy = show;
                Action<TData, TView> showAndSubscribe = (item, view) =>
                {
                    showCopy(item, view);
                    view.connections += updater(item).Subscribe(() => showCopy(item, view));
                };
                show = showAndSubscribe;
            }
            delegates = delegates ?? new TableDelegates<TView>();
            if (delegates.onInsertAnimated != null) show += (d, view) =>
            {
                if (components.animationsEnabled) delegates.onInsertAnimated(view);
            };
            if (options.Has(PresentOptions.PreserveSiblingOrder))
            {
                show += (d, view) => { view.tr.SetSiblingIndex(view.indexInModel); };
            }

            var onRemove = delegates.onRemoveAnimated;
            if (onRemove != null)
            {
                var onRemoveCopy = onRemove;
                onRemove = v =>
                {
                    if (components.animationsEnabled) return onRemoveCopy(v);
                    else return 0;
                };
            }
            components.viewLoader = new LinearViewLoader<TView, TData>(pool, show, onRemove);
            components.delegates = delegates;
            components.collection = data;
            
            return components;
        }

        public static void FillPoolWithChildrenViews<TView, TData>(IViewPool<TView, TData> pool, Transform parent, PrefabRef<TView> prefabRef, 
            TView prefab, PresentOptions options) where TView : ReusableView
        {
            if (parent == null) return;
            foreach (var obj in parent)
            {
                var view = ((Transform) obj).GetComponent<TView>();
                if (view != null) 
                {
                    if (view.prefabRef != null && view.prefabRef == prefab)
                    {
                        pool.AddViewToUse((TView)view.prefabRef, view);
                    }
                    else if (options.Has(PresentOptions.UseChildWithSameTypeAsView) && view.GetType() == prefabRef.ExtractType())
                    {
                        view.prefabRef = prefab;
                        pool.AddViewToUse((TView)view.prefabRef, view);
                    }
                    else if (options.Has(PresentOptions.UseChildWithSameNameAsView) && view.name == prefabRef.ExtractName())
                    {
                        view.prefabRef = prefab;
                        pool.AddViewToUse((TView)view.prefabRef, view);
                    }
                }
            }
        }
        
        public class GroupItem<TGroup, TData, TView> : TableConnectionsAndComponents<TView, TData>
            where TView : ReusableView
        {
            public TGroup g;
        }

        // data is only for type deduction
        public static IViewPool<TView, TData> SimpleViewPool<TView, TData>(TView prefab, RectTransform parent,
            IReactiveCollection<TData> data = null) where TView : ReusableView
        {
            return new ViewPool<TView, TData>(parent, prefab);
        }

        public static IViewPool<TView, TData> SimpleViewPool<TView, TData>(this RectTransform parent, TView prefab)
            where TView : ReusableView
        {
            return new ViewPool<TView, TData>(parent, prefab);
        }
        
        // Searches prefab in parents children
        public static IViewPool<TView, TData> SimpleViewPool<TView, TData>(this RectTransform parent) where TView : ReusableView
        {
            var views = parent.GetComponentsInChildren<TView>();
            if (views.Length == 0)
            {
                throw new ZergRushException($"cant find {typeof(TView)} in {parent}'s children");
            }

            var prefab = views[0];
            var pool = new ViewPool<TView, TData>(parent, prefab);
            foreach (var view in views)
            {
                pool.AddViewToUse(prefab, view);
            }
            return pool;
        }

        public static TableConnectionsAndComponents<TViewGroup, GroupItem<TGroup, TData, TView>> PresentGrouped<TData,
            TView, TGroup, TViewGroup>(
            IViewPort viewPort,
            IReactiveCollection<TData> collection,
            Func<TData, TGroup> grouper,
            IViewPool<TView, TData> viewPool,
            IViewPool<TViewGroup, TGroup> groupViewPool,
            Action<TData, TView> show,
            Action<TGroup, TViewGroup> showGroup,
            Func<IReactiveCollection<TData>, IScrollViewLayout> layoutFactory,
            Func<TGroup, TGroup, int> groupSort,
            Func<TData, TData, int> itemSort,
            LinearLayoutSettings settings,
            TableDelegates<TView> delegates = null,
            TableDelegates<TViewGroup> groupViewDelegates = null
        ) where TView : ReusableView where TViewGroup : ReusableView
        {
            float LayoutBoundingsFactory(GroupItem<TGroup, TData, TView> item) =>
                item.layout.size.value + settings.mainSize;

            var grouped = new ReactiveCollection<GroupItem<TGroup, TData, TView>>();
            var groupLayout = LinearVariableSizeLayout.Create(grouped, LayoutBoundingsFactory, settings);
            var groupComponents = new TableConnectionsAndComponents<TViewGroup, GroupItem<TGroup, TData, TView>>()
            {
                layout = groupLayout,
                collection = grouped,
                viewPort = viewPort,
                viewLoader = new LinearViewLoader<TViewGroup, GroupItem<TGroup, TData, TView>>(
                    new ViewPoolProxyMap<TViewGroup, GroupItem<TGroup, TData, TView>, TGroup>
                    {
                        viewPoolImplementation = groupViewPool,
                        map = i => i.g
                    }, (i, view) => showGroup(i.g, view)),
                delegates = groupViewDelegates
            };

            List<GroupItem<TGroup, TData, TView>> groupPool = new List<GroupItem<TGroup, TData, TView>>();

            void RecycleGroup(GroupItem<TGroup, TData, TView> group)
            {
                group.DisconnectAll();
                ((ReactiveCollection<TData>)group.collection).Clear();
                groupPool.Add(group);
            }

            void UpdateLayoutsFromPos(int index)
            {
                groupLayout.RefillFromPos(index, grouped, LayoutBoundingsFactory);
                for (var i = index; i < grouped.Count; i++)
                {
                    var groupView = grouped[i];
                    groupView.layout.SetEndShift(groupLayout.EndPointForIndex(i));
                }
            }
            
            GroupItem<TGroup, TData, TView> CreateGroupItem(TGroup g, TData firstItem)
            {
                if (groupPool.Count > 0)
                {
                    var groupItem = groupPool.TakeLast();
                    ((ReactiveCollection<TData>)groupItem.collection).Add(firstItem);
                    groupItem.g = g;
                    return groupItem;
                }
                else
                {
                    var groupColl = new ReactiveCollection<TData>();
                    groupColl.Add(firstItem);
                    var groupItem = new GroupItem<TGroup, TData, TView>();
                    groupItem.viewLoader =
                        new LinearViewLoader<TView, TData>(viewPool, show);
                    groupItem.delegates = delegates;
                    groupItem.collection = groupColl;
                    groupItem.viewPort = viewPort;
                    groupItem.layout = layoutFactory(groupItem.collection);
                    groupItem.layout.UpdatePrefabSizeInfo(viewPool.sampleViewSize(firstItem));
                    groupItem.g = g;
                    return groupItem;
                }
            }

            groupComponents.addConnection = collection.BindCollection(ev =>
            {
                if (ev.type == ReactiveCollectionEventType.Reset)
                {
                    foreach (var groupView in grouped)
                    {
                        RecycleGroup(groupView);
                    }
                    grouped.Clear();

                    foreach (var tData in ev.newData)
                    {
                        var g = grouper(tData);
                        // Lazy read group view size
                        if (settings.forceSize == Vector2.zero)
                        {
                            settings.forceSize = groupViewPool.sampleViewSize(g);
                        }
                        if (grouped.TryFind(elem => EqualityComparer<TGroup>.Default.Equals(g, elem.g), out var coll))
                        {
                            ((ReactiveCollection<TData>) coll.collection).InsertSorted(itemSort, tData);
                        }
                        else
                        {
                            var groupItem = CreateGroupItem(g, tData);
                            groupComponents.addConnection = groupItem;
                            grouped.InsertSorted((g1, g2) => groupSort(g1.g, g2.g), groupItem);
                        }
                    }
                    
                    foreach (var groupView in grouped)
                    {
                        groupView.layout.count = groupView.collection.Count;
                        groupView.layout.RefreshSize();
                    }
                    groupLayout.Refill(grouped, LayoutBoundingsFactory);
                    for (var i = 0; i < grouped.Count; i++)
                    {
                        var groupView = grouped[i];
                        groupView.layout.SetEndShift(groupLayout.EndPointForIndex(i));
                        ControlItemVisibilityAndRecycle(groupView);
                    }
                }
                else if (ev.type == ReactiveCollectionEventType.Remove)
                {
                    var g = grouper(ev.oldItem);
                    var groupView = grouped.Find(v => EqualityComparer<TGroup>.Default.Equals(v.g, g));
                    if (groupView == null) { throw new ZergRushException("wtf"); }
                    var gColl = ((ReactiveCollection<TData>) groupView.collection);

                    var index = grouped.IndexOf(groupView);
                    var initSize = groupView.layout.size.value;
                    gColl.Remove(ev.oldItem);
                    
                    if (gColl.Count == 0)
                    {
                        RecycleGroup(groupView);
                        grouped.RemoveAt(index);
                    }

                    if (gColl.Count == 0 || initSize != groupView.layout.size.value)
                    {
                        UpdateLayoutsFromPos(index);
                    }
                }
                else if (ev.type == ReactiveCollectionEventType.Insert)
                {
                    var g = grouper(ev.newItem);
                    bool needUpdatePos = false;
                    bool groupIsNew = false;
                    var indexOfGroup = grouped.IndexOf(v => EqualityComparer<TGroup>.Default.Equals(v.g, g));
                    var groupView = indexOfGroup != -1 ? grouped[indexOfGroup] : null;
                    if (groupView == null)
                    {
                        groupView = CreateGroupItem(g, ev.newItem);
                        groupComponents.addConnection = groupView;
                        indexOfGroup = grouped.InsertSorted((g1, g2) => groupSort(g1.g, g2.g), groupView);
                        groupView.layout.count = 1;
                        groupView.layout.RefreshSize();
                        needUpdatePos = true;
                        groupIsNew = true;
                    }
                    else
                    {
                        var initSize = groupView.layout.size.value;
                        ((ReactiveCollection<TData>) groupView.collection).InsertSorted(itemSort, ev.newItem);
                        if (initSize != groupView.layout.size.value)
                        {
                            needUpdatePos = true;
                        }
                    }

                    if (needUpdatePos)
                    {
                        UpdateLayoutsFromPos(indexOfGroup);
                    }

                    if (groupIsNew)
                    {
                        ControlItemVisibilityAndRecycle(groupView);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            });

            ControlItemVisibilityAndRecycle(groupComponents);

            if (viewPort is ScrollRectViewPort scroll) groupComponents.addConnection = scroll.BindToLayout(groupLayout);
            
            groupComponents.addConnection = new AnonymousDisposable(() =>
            {
                groupComponents.collection.ForEach(groupView =>
                {
                    groupView.DisconnectAll();
                });
            });

            return groupComponents;
        }

        public static TableConnectionsAndComponents<TView, TData> PresentInScrollWithLayout<TData, TView>(
            this IReactiveCollection<TData> data,
            ReactiveScrollRect scroll,
            PrefabRef<TView> prefab = default,
            Action<TData, TView> show = null,
            Func<TData, PrefabRef<TView>> prefabSelector = null,
            IScrollViewLayout layout = null, // Linear layout is default
            TableDelegates<TView> delegates = null,
            PresentOptions options = PresentOptions.UseChildWithSameTypeAsView
            ) where TView : ReusableView
        {
            var components = CreateBasicTableComponents(
                data,
                scroll.scroll.content,
                show,
                prefab: prefab, 
                prefabSelector:prefabSelector,
                layout: layout, 
                delegates: delegates,
                options: options | PresentOptions.NeedLayout);
            return PresentInScrollWithLayout(components, scroll);
        }

        [MustUseReturnValue]
        public static TableConnectionsAndComponents<TView, TData> PresentWithLayout<TData, TView>(
            this IReactiveCollection<TData> data,
            RectTransform rect,
            PrefabRef<TView> prefab,
            Action<TData, TView> fillFactory,
            IScrollViewLayout layout = null, // Linear layout is default
            TableDelegates<TView> delegates = null,
            PresentOptions options = PresentOptions.UseChildWithSameTypeAsView) where TView : ReusableView
        {
            var components = CreateBasicTableComponents(data, rect, fillFactory, prefab: prefab, 
                layout: layout, delegates: delegates, options:options | PresentOptions.NeedLayout);
            components.viewPort = new AllVisibleViewPort();
            return ControlItemVisibilityAndRecycle(components);
        }

        public static IScrollViewLayout LinearLayout(LayoutDirection direction = LayoutDirection.Vertical,
            float forceMainSize = 0, float margin = 5, bool expandViews = true, float topShift = 0, float bottomShift = 0)
        {
            return new LinearLayout(new LinearLayoutSettings
            {
                direction = direction,
                forceSize = direction == LayoutDirection.Horizontal
                    ? new Vector2(forceMainSize, 0)
                    : new Vector2(0, forceMainSize),
                marginVec = new Vector2(margin, margin),
                expandViews = expandViews,
                topShift = topShift,
                bottomShift = bottomShift
            });
        }
        
        public static IScrollViewLayout GridLayout(int gridSize = 0, LayoutDirection direction = LayoutDirection.Vertical,
            float margin = 5, float subMargin = 5, float topShift = 0, float bottomShift = 0, Vector2 forceSize = default)
        {
            return new GridLayout(new GridLayoutSettings
            {
                direction = direction,
                forceSize = forceSize,
                marginVec = new Vector2(direction == LayoutDirection.Horizontal ? margin : subMargin, direction == LayoutDirection.Horizontal ? subMargin : margin),
                subMargin = subMargin,
                gridSize = gridSize,
                topShift = topShift,
                bottomShift = bottomShift,
            });
        }

        public static IScrollViewLayout LinearLayout(LinearLayoutSettings settings)
        {
            return new LinearLayout(settings);
        }
        
        public static IScrollViewLayout GridLayout(GridLayoutSettings settings)
        {
            return new GridLayout(settings);
        }

        public static IScrollViewLayout VariableViewSizeLayout<TData>(
            IReactiveCollection<TData> data,
            Func<TData, float> viewSizeFactory,
            LinearLayoutSettings settings)
        {
            return LinearVariableSizeLayout.Create(data, viewSizeFactory, settings);
        }

        public static void AdjustScrollRectContentAnchors(ScrollRect scroll, bool horizontal)
        {
            if (horizontal)
            {
                scroll.content.pivot = new Vector2(0, 0.5f);
                scroll.content.anchorMin = new Vector2(0.5f, 0);
                scroll.content.anchorMax = new Vector2(0.5f, 1);
            }
            else
            {
                scroll.content.pivot = new Vector2(0.5f, 1);
                scroll.content.anchorMin = new Vector2(0, 0.5f);
                scroll.content.anchorMax = new Vector2(1, 0.5f);
            }
        }


        public static IDisposable Present<T, TView>(
            this IReactiveCollection<T> coll,
            List<TView> views,
            Action<T, TView> show
            ) where TView : ReusableView
        {
            var connections = new DoubleDisposable();

            void UpdateView(int index)
            {
                var c = coll;
                var view = views[index];
                if (c.Count <= index)
                {
                    view.SetActiveSafe(false);
                    view.DisconnectAll();
                }
                else
                {
                    view.DisconnectAll();
                    view.SetActiveSafe(true);
                    show(c[index], view);
                }
            }

            void UpdateFromIndex(int index)
            {
                for (int i = index; i < views.Count; i++)
                {
                    UpdateView(i);
                }
            }
            connections.First = coll.update.Subscribe(e =>
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        UpdateFromIndex(0);
                        break;
                    case ReactiveCollectionEventType.Insert:
                    case ReactiveCollectionEventType.Remove:
                        UpdateFromIndex(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        UpdateView(e.position);
                        break;
                }
            });
            connections.Second = new AnonymousDisposable(() => views.ForEach(v => v.DisconnectAll()));
            
            UpdateFromIndex(0);
            
            return connections;
        }

        [MustUseReturnValue]
        public static SimplePresentComponents<TView, T> Present<T, TView>(
            this IReactiveCollection<T> coll,
            Transform parent,
            PrefabRef<TView> prefab = default,
            Action<T, TView> show = null,
            Func<T, IEventStream> updater = null,
            Func<T, PrefabRef<TView>> prefabSelector = null,
            IViewPool<TView, T> pool = null,
            PresentOptions options = PresentOptions.UseChildWithSameTypeAsView,
            TableDelegates<TView> delegates = null
            ) where TView : ReusableView
        {
            var components = CreateBasicTableComponents(coll, parent, show, pool, prefab, prefabSelector, null, delegates, updater, options);
            var viewStorage = components.viewLoader;

            components.addConnection = coll.update.Subscribe(e =>
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        viewStorage.ReloadAll(e.newData);
                        break;
                    case ReactiveCollectionEventType.Insert:
                        viewStorage.LoadView(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        viewStorage.UnloadView(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        viewStorage.UnloadView(e.position);
                        viewStorage.LoadView(e.position, e.newItem);
                        break;
                }
            });
            viewStorage.ReloadAll(coll);
            // TODO think about how destroy should work
            components.addConnection = new AnonymousDisposable(() => components.viewLoader.UnloadAll(true));
            components.animationsEnabled = true;
            return components;
        }
    }
}
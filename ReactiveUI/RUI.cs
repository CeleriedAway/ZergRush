#if UNITY_5_3_OR_NEWER

using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UI;
using ZergRush.ReactiveCore;

namespace ZergRush.ReactiveUI
{
    // You need to dispose this when table view is done.
    public class TableConnectionsAndComponents<TView> : Connections
        where TView : ReusableView
    {
        public LinearViewStorage<TView> viewStorage;
        public IViewPort viewPort;
        public ITableViewLayout layout;
        public ITableContentProvider<TView> content; 
        public TableDelegates<TView> delegates;
    }
    
    public static class Rui
    {
        static TableConnectionsAndComponents<TView> ControlItemVisibilityAndRecycle<TView>(TableConnectionsAndComponents<TView> connectionsAndComponents)
            where TView : ReusableView
        {
            var viewStorage = connectionsAndComponents.viewStorage;
            var viewPort = connectionsAndComponents.viewPort;
            var layout = connectionsAndComponents.layout;
            var content = connectionsAndComponents.content;
            var delegates = connectionsAndComponents.delegates;
            
            if (delegates == null) delegates = new TableDelegates<TView>();
            if (delegates.onRecycle != null) viewStorage.pool.AddRecycleAction(delegates.onRecycle);

            Action<TView, int> createView = (view, i) =>
            {
                view.rectTransform.anchoredPosition = layout.AncoredPositionForIndex(i);
                view.indexInModel = i;
                content.FillView(view, i);
            };
            
            Action<TView, int> createViewAnimated = createView;
            if (delegates.onInsert != null) createViewAnimated += (view, i) => delegates.onInsert(view);

            Func<TView, float> destroyAnimated = null;
            if (delegates.onRemove != null) destroyAnimated = view => delegates.onRemove(view);

            //TODO resolve this copypaste to something meaningfull
            Action ensureVisibleViewsAreLoaded = () =>
            {
                if (content.count == 0) return;
                int firstVisible, lastVisible;
                viewPort.CalculateVisibleIndexes(layout, out firstVisible, out lastVisible);
                lastVisible = Mathf.Min(lastVisible, content.count - 1);
                viewStorage.EnsureLoadedInterval(firstVisible, lastVisible, createView, null);
            };
            
            Action ensureVisibleViewsAreLoadedAnimated = () =>
            {
                if (content.count == 0) return;
                int firstVisible, lastVisible;
                viewPort.CalculateVisibleIndexes(layout, out firstVisible, out lastVisible);
                lastVisible = Mathf.Min(lastVisible, content.count - 1);
                viewStorage.EnsureLoadedInterval(firstVisible, lastVisible, createViewAnimated, destroyAnimated);
            };

            Action updateLayout = () => {
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
                         view.rectTransform.anchoredPosition = newPos;
                     }
                 });
            };

            connectionsAndComponents.addConnection = layout.updatePositionsRequest.Listen(updateLayout);
            connectionsAndComponents.addConnection = viewPort.needRecalcVisibility.Listen(ensureVisibleViewsAreLoaded);
            connectionsAndComponents.addConnection = content.updates.Listen(e => {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Insert:
                        viewStorage.InjectAtIndex(e.position, createViewAnimated);
                        ensureVisibleViewsAreLoadedAnimated();
                        // TODO correct reusable view index
                        updateLayout();
                        break;
                    case ReactiveCollectionEventType.Remove:
                        viewStorage.PierceIndex(e.position, destroyAnimated);
                        ensureVisibleViewsAreLoadedAnimated();
                        // TODO correct reusable view index
                        updateLayout();
                        break;
                    case ReactiveCollectionEventType.Set:
                        viewStorage.ReplaceIndex(e.position, createViewAnimated, destroyAnimated);
                        break;
                    case ReactiveCollectionEventType.Reset:
                        viewStorage.UnloadAll(destroyAnimated);
                        ensureVisibleViewsAreLoadedAnimated();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

            ensureVisibleViewsAreLoaded();
            return connectionsAndComponents;
        }
        
        public static TableConnectionsAndComponents<TView> PresentInScrollWithReusableViews<TView>(
            TableConnectionsAndComponents<TView> connectionsAndComponents,
            ReactiveScrollRect scroll)
            where TView : ReusableView
        {
            AdjustScrollRectContentAnchors(scroll.scroll, scroll.scroll.horizontal);
            connectionsAndComponents.viewPort = new ScrollRectViewPort(scroll, connectionsAndComponents.layout, connectionsAndComponents.connectionSink);
            return ControlItemVisibilityAndRecycle(connectionsAndComponents);
        }

        // Creates everithing except viewport.
        public static TableConnectionsAndComponents<TView> CreateBasicTableComponents<TData, TView>(
            RectTransform viewParent,
            IReactiveCollection<TData> data,
            TView prefab,
            TableLayoutSettings settings,
            Action<TData, TView> fillFactory,
            ITableViewLayout layout = null, // Linear layout is default
            TableDelegates<TView> delegates = null) where TView : ReusableView
        {
            var components = new TableConnectionsAndComponents<TView>();
            components.viewStorage = TableViewStorage(viewParent, prefab);
            components.delegates = delegates;
            
            if (settings.viewSize <= 0) settings.ReadSizeFromPrefab(prefab.GetComponent<RectTransform>());
            if (settings.autoAdjustAnchors) components.viewStorage.pool.AddInstantiateAction(v => AdjustAnchors(v, settings.direction));
            
            if (layout == null) layout = LinearTableLayout(data.CountCell(), settings);
            components.layout = layout;
            components.content = ContentProvider(data, fillFactory);
            
            return components;
        }
        
        public static TableConnectionsAndComponents<TView> PresentInScrollWithReusableViews<TData, TView>(
            ReactiveScrollRect scroll,
            IReactiveCollection<TData> data,
            TView prefab,
            TableLayoutSettings settings,
            Action<TData, TView> fillFactory,
            ITableViewLayout layout = null, // Linear layout is default
            TableDelegates<TView> delegates = null) where TView : ReusableView
        {
            var components = CreateBasicTableComponents(scroll.scroll.content, data, prefab, settings, fillFactory,
                layout, delegates);   
            return PresentInScrollWithReusableViews(components, scroll);
        }

        public static TableConnectionsAndComponents<TView> PresentInRect<TData, TView>(
            RectTransform rect,
            IReactiveCollection<TData> data,
            TView prefab,
            TableLayoutSettings settings,
            Action<TData, TView> fillFactory,
            ITableViewLayout layout = null, // Linear layout is default
            TableDelegates<TView> delegates = null) where TView : ReusableView
        {
            var components = CreateBasicTableComponents(rect, data, prefab, settings, fillFactory,
                layout, delegates);
            components.viewPort = new AllVisibleViewPort();
            return ControlItemVisibilityAndRecycle(components);
        }

        public static ITableViewLayout LinearTableLayout(ICell<int> count, TableLayoutSettings settings)
        {
            return new LinearTableLayout(count, settings);
        }

        public static ITableViewLayout GridTableLayout(ICell<int> count, TableLayoutSettings settings, int gridSize)
        {
            return new GridTableLayout(count, settings, gridSize);
        }

        public static ITableViewLayout VariableViewSizeLayout<TData>(
            IReactiveCollection<TData> data, 
            Func<TData, float> viewSizeFactory,
            TableLayoutSettings settings,
            Action<IDisposable> connectionsSink)
        {
            return LinearVariableTableLayout.Create(data, viewSizeFactory, settings, connectionsSink);
        }

        public static ITableContentProvider<TView> ContentProvider<TData, TView> (IReactiveCollection<TData> data, Action<TData, TView> fillFactory)
        {
            return new TableContentProvider<TData, TView>(data, fillFactory);
        }

        public static LinearViewStorage<TView> TableViewStorage<TView>( RectTransform parent, TView prefab)
            where TView : ReusableView
        {
            return new LinearViewStorage<TView>{pool = new ViewPool<TView>(parent, prefab)};  
        }

        public static LinearViewStorage<TView> TableViewStorage<TView>(ViewPool<TView> pool)
            where TView : ReusableView
        {
            return new LinearViewStorage<TView>{pool = pool};  
        }

        public static void AdjustAnchors<TView>(TView view, LayoutDirection direction) where TView : ReusableView
        {
            if (direction == LayoutDirection.Horizontal)
            {
                view.rectTransform.anchorMin = new Vector2(0, 0.5f);
                view.rectTransform.anchorMax = new Vector3(0, 0.5f);
            }
            else
            {
                view.rectTransform.anchorMin = new Vector2(0.5f, 1);
                view.rectTransform.anchorMax = new Vector3(0.5f, 1);
            }
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

    }
}
#endif

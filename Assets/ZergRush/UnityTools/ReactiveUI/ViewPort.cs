using System;
using UnityEngine;
using UnityEngine.UI;
using ZergRush.ReactiveCore;

namespace ZergRush.ReactiveUI
{
    public interface IViewPort
    {
        void CalculateVisibleIndexes(IScrollViewLayout layout, out int first, out int last);
        IEventStream needRecalcVisibility { get; }
    }

    public class AllVisibleViewPort : IViewPort
    {
        public void CalculateVisibleIndexes(IScrollViewLayout layout, out int first, out int last)
        {
            first = 0;
            last = Int32.MaxValue;
        }

        public IEventStream needRecalcVisibility { get { return AbandonedStream.value; } }
    }

    public class ScrollRectViewPort : IViewPort
    {
        ReactiveScrollRect rect;

        public ScrollRectViewPort(ReactiveScrollRect rect)
        {
            this.rect = rect;
        }
        public ScrollRectViewPort(ReactiveScrollRect rect, IScrollViewLayout layout, Action<IDisposable> connectionSink)
        {
            this.rect = rect;
            connectionSink(BindToLayout(layout));
        }

        public IDisposable BindToLayout(IScrollViewLayout layout)
        {
            return layout.size.MergeBind(layout.topShift, (size, pos) =>
            {
                this.rect.scroll.SetRectMainSize(size + pos + layout.settings.bottomShift);
            });
        }

        /// <summary>
        /// Content shift in layout space (viewport top/left = 0). Works with any content pivot/anchors.
        /// </summary>
        public static float GetContentShift(ScrollRect scroll)
        {
            bool horizontal = scroll.horizontal;
            var viewportRect = scroll.viewport != null ? scroll.viewport.rect : scroll.GetComponent<RectTransform>().rect;
            var viewportSize = horizontal ? viewportRect.width : viewportRect.height;
            var viewportHalf = viewportSize / 2f;
            var content = scroll.content;
            var anchored = content.anchoredPosition;
            var pivot = content.pivot;
            var rect = content.rect;
            var contentMainSize = horizontal ? rect.width : rect.height;
            // Pivot position = anchoredPosition; content edge = pivot + (edge offset in local space)
            var contentEdgeFromPivot = horizontal ? -pivot.x * contentMainSize : (1f - pivot.y) * contentMainSize;

            if (horizontal)
            {
                var contentLeft = anchored.x + contentEdgeFromPivot;
                var viewportLeft = -viewportHalf;
                return viewportLeft - contentLeft;
            }
            else
            {
                var contentTop = anchored.y + contentEdgeFromPivot;
                var viewportTop = viewportHalf;
                return contentTop - viewportTop;
            }
        }

        public static float GetUnfuckedContentShift(ScrollRect scroll)
        {
            return GetContentShift(scroll);
        }

        public void CalculateVisibleIndexes(IScrollViewLayout layout, out int first, out int last)
        {
            var pos = GetContentShift(rect.scroll);
            var height = rect.scroll.RectMainSize();

            if (layout.topShift.value - pos > height || pos - layout.size.value - layout.topShift.value > 0)
            {
                first = -1;
                last = -1;
                return;
            }
            
            first = layout.FirstVisibleIndexFromShift(pos);
            last = layout.LastVisibleIndexFromShift(pos + height);
        }

        public IEventStream needRecalcVisibility { get { return rect.scrollPos.updates; } }
    }
}

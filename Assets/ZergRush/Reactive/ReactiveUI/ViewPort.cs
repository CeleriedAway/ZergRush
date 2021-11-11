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

        public static float GetUnfuckedContentShift(ScrollRect scroll)
        {
            bool horizontal = scroll.horizontal;
            var viewPortCorrection = horizontal
                ? scroll.GetComponent<RectTransform>().rect.width
                : scroll.GetComponent<RectTransform>().rect.height;

            viewPortCorrection /= 2;

            var pos = scroll.horizontal
                ? scroll.content.anchoredPosition.x
                : scroll.content.anchoredPosition.y; 
            pos += horizontal ? viewPortCorrection : -viewPortCorrection;
            if (scroll.horizontal) pos = -pos;
            return pos;
        }

        public void CalculateVisibleIndexes(IScrollViewLayout layout, out int first, out int last)
        {
            bool horizontal = rect.scroll.horizontal;
            var viewPortCorrection = horizontal
                ? rect.GetComponent<RectTransform>().rect.width
                : rect.GetComponent<RectTransform>().rect.height;

            viewPortCorrection /= 2;

            var pos = rect.scrollPos.value;
            pos += horizontal ? viewPortCorrection : -viewPortCorrection;
            if (rect.scroll.horizontal) pos = -pos;

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

#if UNITY_5_3_OR_NEWER

using System;
using UnityEngine;
using ZergRush.ReactiveCore;

namespace ZergRush.ReactiveUI
{
    public interface IViewPort
    {
        void CalculateVisibleIndexes(ITableViewLayout layout, out int first, out int last);
        IEventStream needRecalcVisibility { get; }
    }

    public class AllVisibleViewPort : IViewPort
    {
        public void CalculateVisibleIndexes(ITableViewLayout layout, out int first, out int last)
        {
            first = 0;
            last = Int32.MaxValue;
        }

        public IEventStream needRecalcVisibility { get { return AbandonedStream.value; } }
    }

    public class ScrollRectViewPort : IViewPort
    {
        ReactiveScrollRect rect;

        public ScrollRectViewPort(ReactiveScrollRect rect, ITableViewLayout layout, Action<IDisposable> connectionSink)
        {
            this.rect = rect;
            connectionSink(layout.boundingSize.Bind(r =>
            {
                this.rect.scroll.SetRectMainSize(r.size + r.position);
            }));
        }

        public void CalculateVisibleIndexes(ITableViewLayout layout, out int first, out int last)
        {
            bool horizontal = rect.scroll.horizontal;
            var viewPortCorrection = horizontal
                ? rect.GetComponent<RectTransform>().sizeDelta.x
                : rect.GetComponent<RectTransform>().sizeDelta.y;

            viewPortCorrection /= 2;

            var pos = rect.scrollPos.value;
            pos += horizontal ? viewPortCorrection : -viewPortCorrection;
            if (rect.scroll.horizontal) pos = -pos;

            first = layout.FirstVisibleIndexFromShift(pos);
            last = layout.LastVisibleIndexFromShift(pos + rect.scroll.RectMainSize());
        }

        public IEventStream needRecalcVisibility { get { return rect.scrollPos.updates; } }
    }
}
#endif

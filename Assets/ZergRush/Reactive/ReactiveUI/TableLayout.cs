#if UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ZergRush.ReactiveCore;

namespace ZergRush.ReactiveUI
{
    public abstract class IScrollViewLayout : Connections
    {
        public BaseLayoutSettings settings;
        public int count;
        public EventStream needUpdate = new EventStream();

        public IScrollViewLayout(BaseLayoutSettings settings)
        {
            if (settings == null) throw new ArgumentNullException();
            this.settings = settings;
            topShift.value = settings.topShift;
        }

        public abstract int IndexFromContainerPosition(Vector2 pos);
        public abstract Vector2 AncoredPositionForIndex(int index);
        public abstract int FirstVisibleIndexFromShift(float shift);
        public abstract int LastVisibleIndexFromShift(float shift);

        public void SetStartShift(float shift) => topShift.value = shift;
        public void SetEndShift(float shift) => topShift.value = shift - size.value;

        protected abstract bool expandViews { get; }

        public abstract void RefreshSize();

        public void UpdatePrefabSizeInfo(Vector2 size)
        {
            if (settings.forceSize == Vector2.zero)
            {
                settings.forceSize = size;
                // update all inner size when view size began to be known, dont ask
                RefreshSize();
            }
        }
        public virtual void CorrectViewAnchors(ReusableView v)
        {
            if (settings.forceSize == Vector2.zero)
            {
                settings.forceSize = v.rectTransform.sizeDelta;
                // update all inner size when view size began to be known, dont ask
                RefreshSize();
            }
            v.rectTransform.sizeDelta = settings.forceSize;

            bool h = settings.direction == LayoutDirection.Horizontal;
            if (h)
            {
                v.rectTransform.anchorMin = new Vector2(0, expandViews ? 0 : 0.5f);
                v.rectTransform.anchorMax = new Vector3(0, expandViews ? 1 : 0.5f);
            }
            else
            {
                v.rectTransform.anchorMin = new Vector2(expandViews ? 0 : 0.5f, 1);
                v.rectTransform.anchorMax = new Vector3(expandViews ? 1 : 0.5f, 1);
            }

            if (expandViews)
            {
                v.rectTransform.sizeDelta = settings.direction == LayoutDirection.Horizontal
                    ? v.rectTransform.sizeDelta.WithY(0)
                    : v.rectTransform.sizeDelta.WithX(0);
            }
        }

        public Cell<float> size = new Cell<float>();
        public Cell<float> topShift = new Cell<float>();
    }

    [Serializable]
    public class BaseLayoutSettings
    {
        public LayoutDirection direction;
        public Vector2 marginVec; 
        public Vector2 forceSize;
        public Vector2 effectiveSize => forceSize + marginVec;
        public float topShift;
        public float bottomShift;

        public float Main(Vector2 vec) => direction == LayoutDirection.Vertical ? vec.y : vec.x;
        public float Sub(Vector2 vec) => direction == LayoutDirection.Vertical ? vec.x : vec.y;
        public float mainSize => direction == LayoutDirection.Vertical ? effectiveSize.y : effectiveSize.x;
        public float subSize => direction == LayoutDirection.Vertical ? effectiveSize.x : effectiveSize.y;
    }

    [Serializable]
    public class LinearLayoutSettings : BaseLayoutSettings
    {
        public bool expandViews;
    }

    [Serializable]
    public class GridLayoutSettings : BaseLayoutSettings
    {
        public int gridSize;
        public float subMargin;
    }

    class LinearLayout : IScrollViewLayout
    {
        new LinearLayoutSettings settings => (LinearLayoutSettings)base.settings;
        int directionSign;

        public LinearLayout(LinearLayoutSettings settings) : base(settings)
        {
            directionSign = settings.direction == LayoutDirection.Horizontal ? 1 : -1;
        }

        public override int IndexFromContainerPosition(Vector2 pos)
        {
            return Mathf.FloorToInt(Mathf.Abs(settings.Main(pos) / settings.mainSize));
        }

        public override Vector2 AncoredPositionForIndex(int index)
        {
            float viewPos = directionSign * (topShift.value + index * settings.mainSize +
                                             settings.mainSize / 2);
            Vector2 finalPos = settings.direction == LayoutDirection.Horizontal
                ? new Vector2(viewPos, 0)
                : new Vector2(0, viewPos);
            return finalPos;
        }

        public override int FirstVisibleIndexFromShift(float shift)
        {
            float i = (shift - topShift.value) / (settings.mainSize);
            return Mathf.Clamp(Mathf.FloorToInt(i), 0, count - 1);
        }

        public override int LastVisibleIndexFromShift(float shift)
        {
            return FirstVisibleIndexFromShift(shift);
        }
        
        public override void RefreshSize()
        {
            size.value = count * settings.mainSize;
        }

        protected override bool expandViews => settings.expandViews;
    }

    class GridLayout : IScrollViewLayout
    {
        int directionMainSign;
        int directionSubSign;
        int gridSize => ((GridLayoutSettings)settings).gridSize;

        new GridLayoutSettings settings => (GridLayoutSettings)base.settings;

        public GridLayout(GridLayoutSettings settigns) : base(settigns)
        {
            directionMainSign = settigns.direction == LayoutDirection.Horizontal ? 1 : -1;
            directionSubSign = settigns.direction == LayoutDirection.Horizontal ? -1 : 1;
        }

        protected override bool expandViews => false;

        public override void RefreshSize()
        {
            size.value = (count / gridSize + (count % gridSize != 0 ? 1 : 0)) * settings.mainSize;
        }

        public override int IndexFromContainerPosition(Vector2 pos)
        {
            var mainIndex = Mathf.FloorToInt(settings.Main(pos) / settings.mainSize * directionMainSign);
            var subIndex = Mathf.FloorToInt(settings.Sub(pos) / settings.subSize * directionSubSign);
            
            return mainIndex * gridSize + subIndex;
        }

        public override Vector2 AncoredPositionForIndex(int index)
        {
            int mainIndex = index / gridSize;
            float viewPosMain = directionMainSign *
                                (topShift.value + mainIndex * settings.mainSize +
                                 settings.mainSize / 2);
            int subIndex = index % gridSize;
            float viewSubPos = directionSubSign *
                               ((subIndex - (gridSize - 1) / 2f) * settings.subSize);
            Vector2 finalPos = settings.direction == LayoutDirection.Horizontal
                ? new Vector2(viewPosMain, viewSubPos)
                : new Vector2(viewSubPos, viewPosMain);

            return finalPos;
        }

        public override int FirstVisibleIndexFromShift(float shift)
        {
            float i = (shift - topShift.value) / (settings.mainSize);
            return Mathf.Clamp((Mathf.FloorToInt(i)) * gridSize, 0, count - 1);
        }

        public override int LastVisibleIndexFromShift(float shift)
        {
            float i = (shift - topShift.value) / (settings.mainSize);
            return Mathf.Clamp((Mathf.FloorToInt(i) + 1) * gridSize - 1, 0, count - 1);
        }
    }

    class LinearVariableSizeLayout : IScrollViewLayout
    {
        int directionSign;
        List<float> endPoints = new List<float>();

        LinearVariableSizeLayout(LinearLayoutSettings settings) : base(settings)
        {
        }

        public static LinearVariableSizeLayout Create<TData>(IReactiveCollection<TData> data,
            Func<TData, float> viewSizeFactory, LinearLayoutSettings settings)
        {
            var layout = new LinearVariableSizeLayout(settings);
            layout.settings = settings;
            layout.directionSign = settings.direction == LayoutDirection.Horizontal ? 1 : -1;

            layout.addConnection = data.update.Subscribe(e =>
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        layout.RefillFromPos(0, data, viewSizeFactory);
                        break;
                    case ReactiveCollectionEventType.Insert:
                    case ReactiveCollectionEventType.Remove:
                    case ReactiveCollectionEventType.Set:
                        layout.RefillFromPos(e.position, data, viewSizeFactory);
                        break;
                }
            });
            layout.RefillFromPos(0, data, viewSizeFactory);
            return layout;
        }

        public float EndPointForIndex(int index)
        {
            return endPoints[index];
        }

        public void Refill<TData>(IReactiveCollection<TData> data, Func<TData, float> viewSizeFactory)
        {
            RefillFromPos<TData>(0, data, viewSizeFactory);
        }

        public void RefillLast<TData>(IReactiveCollection<TData> data, Func<TData, float> viewSizeFactory)
        {
            RefillFromPos(data.Count - 1, data, viewSizeFactory);
        }

        public void RefillFromPos<TData>(int i, IReactiveCollection<TData> data, Func<TData, float> viewSizeFactory)
        {
            var curr = data;
            float accum = 0;
            if (i == 0)
            {
                endPoints.Clear();
            }
            else
            {
                endPoints.RemoveRange(i, endPoints.Count - i);
                accum = endPoints[i - 1];
            }

            endPoints.Capacity = curr.Count;
            for (int j = i; j < curr.Count; j++)
            {
                accum += viewSizeFactory(curr[j]);
                endPoints.Add(accum);
            }

            // Update bounding size
            size.value = (endPoints.Count > 0 ? endPoints[endPoints.Count - 1] : 0) + settings.bottomShift;
            needUpdate.Send();
        }

        public override int IndexFromContainerPosition(Vector2 pos)
        {
            throw new NotImplementedException();
        }

        public override Vector2 AncoredPositionForIndex(int index)
        {
            var viewPos = directionSign *
                          (topShift.value + settings.mainSize / 2 + (index > 0 ? endPoints[index - 1] : 0));
            Vector2 finalPos = settings.direction == LayoutDirection.Horizontal
                ? new Vector2(viewPos, 0)
                : new Vector2(0, viewPos);
            return finalPos;
        }

        public override int FirstVisibleIndexFromShift(float shift)
        {
            return Mathf.Min(endPoints.UpperBound(shift - topShift.value), endPoints.Count - 1);
        }

        public override int LastVisibleIndexFromShift(float shift)
        {
            return FirstVisibleIndexFromShift(shift);
        }

        public override void RefreshSize()
        {
        }

        protected override bool expandViews => ((LinearLayoutSettings)settings).expandViews;
    }
}
#endif
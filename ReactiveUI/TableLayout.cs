#if UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ZergRush.ReactiveCore;

namespace ZergRush.ReactiveUI
{
    public interface ITableViewLayout
    {
        int FirstVisibleIndexFromShift(float shift);
        int LastVisibleIndexFromShift(float shift);
        Vector2 AncoredPositionForIndex(int index);
        ICell<Rect> boundingSize { get; }
        IEventStream updatePositionsRequest { get; }
    }

    class LinearTableLayout : ITableViewLayout
    {
        TableLayoutSettings settings;
        ICell<int> count;
        int directionSign;

        public LinearTableLayout(ICell<int> itemCount, TableLayoutSettings settigns)
        {
            settings = settigns;
            count = itemCount;
            directionSign = settigns.direction == LayoutDirection.Horizontal ? 1 : -1;
        }


        public Vector2 AncoredPositionForIndex(int index)
        {
            float viewPos = directionSign * (settings.topShift + index * (settings.viewSize + settings.margin) + settings.viewSize / 2);
            Vector2 finalPos = settings.direction == LayoutDirection.Horizontal ?
                new Vector2(viewPos, 0) : new Vector2(0, viewPos);
            return finalPos;
        }

        public ICell<Rect> boundingSize
        {
            get
            {
                return count.Select(count =>
                {
                    var startCoord = settings.topShift;
                    var length = count * settings.effectiveSize + settings.bottomShift;
                    return settings.direction == LayoutDirection.Horizontal
                        ? new Rect(startCoord, 0, length, 0)
                        : new Rect(0, startCoord, 0, length);
                });
            }
        }

        public IEventStream updatePositionsRequest { get { return AbandonedStream.value; } }

        public int FirstVisibleIndexFromShift(float shift)
        {
            float i = (shift - settings.topShift) / (settings.viewSize + settings.margin);
            return Mathf.Clamp(Mathf.FloorToInt(i), 0, count.value - 1);
        }
        public int LastVisibleIndexFromShift(float shift)
        {
            return FirstVisibleIndexFromShift(shift);
        }
    }

    class GridTableLayout : ITableViewLayout
    {
        TableLayoutSettings settings;
        ICell<int> count;
        int directionMainSign;
        int directionSubSign;
        int gridSize;

        public GridTableLayout(ICell<int> itemCount, TableLayoutSettings settigns, int gridCellCount)
        {
            settings = settigns;
            count = itemCount;
            directionMainSign = settigns.direction == LayoutDirection.Horizontal ? 1 : -1;
            directionSubSign = settigns.direction == LayoutDirection.Horizontal ? -1 : 1;
            gridSize = gridCellCount;
        }


        public Vector2 AncoredPositionForIndex(int index)
        {
            int mainIndex = index / gridSize;
            float viewPosMain = directionMainSign * (settings.topShift + mainIndex * (settings.viewSize + settings.margin) + settings.viewSize / 2);
            int subIndex = index % gridSize;
            float viewSubPos = directionSubSign * ((subIndex - (gridSize - 1) / 2f)* (settings.viewSecondSize + settings.margin));
            Vector2 finalPos = settings.direction == LayoutDirection.Horizontal ?
                new Vector2(viewPosMain, viewSubPos) : new Vector2(viewSubPos, viewPosMain);
            return finalPos;
        }

        public ICell<Rect> boundingSize
        {
            get
            {
                return count.Select(count =>
                {
                    var startCoord = settings.topShift;
                    var length = Mathf.CeilToInt(count / (float)gridSize) * settings.effectiveSize + settings.bottomShift;
                    return settings.direction == LayoutDirection.Horizontal
                        ? new Rect(startCoord, 0, length, 0)
                        : new Rect(0, startCoord, 0, length);
                });
            }
        }

        public IEventStream updatePositionsRequest { get { return AbandonedStream.value; } }

        public int FirstVisibleIndexFromShift(float shift)
        {
            float i = (shift - settings.topShift) / (settings.viewSize + settings.margin);
            return Mathf.Clamp((Mathf.FloorToInt(i)) * gridSize, 0, count.value - 1);
        }
        public int LastVisibleIndexFromShift(float shift)
        {
            float i = (shift - settings.topShift) / (settings.viewSize + settings.margin);
            return Mathf.Clamp((Mathf.FloorToInt(i) + 1) * gridSize - 1, 0, count.value - 1);
        }
    }

    class LinearVariableTableLayout : ITableViewLayout
    {
        TableLayoutSettings settings;
        int directionSign;
        List<float> endPoints = new List<float>();
        Cell<Rect> boundingSizeCell = new Cell<Rect>();
        
        LinearVariableTableLayout() {}

        public static LinearVariableTableLayout Create<TData>(IReactiveCollection<TData> data,
            Func<TData, float> viewSizeFactory, TableLayoutSettings settigns, Action<IDisposable> connectionSink)
        {
            var layout = new LinearVariableTableLayout();
            layout.settings = settigns;
            layout.directionSign = settigns.direction == LayoutDirection.Horizontal ? 1 : -1;
            Action<int> refillFromPos = i =>
            {
                var curr = data.current;
                var endPoints = layout.endPoints;
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
                var startCoord = layout.settings.topShift;
                var length = (endPoints.Count > 0 ? endPoints[endPoints.Count - 1] : 0) + layout.settings.bottomShift;
                layout.boundingSizeCell.value = layout.settings.direction == LayoutDirection.Horizontal
                        ? new Rect(startCoord, 0, length, 0)
                        : new Rect(0, startCoord, 0, length);
            };

            connectionSink(data.update.Listen(e => {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        refillFromPos(0);
                        break;
                    case ReactiveCollectionEventType.Insert:
                    case ReactiveCollectionEventType.Remove:
                    case ReactiveCollectionEventType.Set:
                        refillFromPos(e.position);
                        break;
                } 
            }));
            refillFromPos(0);
            return layout;
        }

        public Vector2 AncoredPositionForIndex(int index)
        {
            var viewCenterShift = index > 0 ? (endPoints[index] - endPoints[index - 1]) / 2 : endPoints[0] / 2;
            var viewPos = directionSign * (settings.topShift + viewCenterShift + (index > 0 ? endPoints[index - 1] : 0));
            Vector2 finalPos = settings.direction == LayoutDirection.Horizontal ?
                new Vector2(viewPos, 0) : new Vector2(0, viewPos);
            return finalPos;
        }

        public ICell<Rect> boundingSize
        {
            get { return boundingSizeCell; }
        }

        public IEventStream updatePositionsRequest { get { return AbandonedStream.value; } }

        public int FirstVisibleIndexFromShift(float shift)
        {
            return Mathf.Min(endPoints.UpperBound(shift - settings.topShift), endPoints.Count - 1);
        }

        public int LastVisibleIndexFromShift(float shift)
        {
            return FirstVisibleIndexFromShift(shift);
        }
    }
}
#endif

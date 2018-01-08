#if UNITY_5_3_OR_NEWER

using System;
using UnityEngine;

namespace ZergRush.ReactiveUI
{
    public enum LayoutDirection
    {
        Vertical,
        Horizontal
    }

    [Serializable]
    public class TableLayoutSettings
    {
        public float topShift = 0;
        public float bottomShift = 0;
        public float viewSize = 0;
        // For grids
        public float viewSecondSize = 0;
        public float margin = 10;
        public LayoutDirection direction = LayoutDirection.Vertical;
        public bool autoAdjustAnchors = true;
        public float effectiveSize { get { return viewSize + margin; } }

        public void ReadSizeFromPrefab(RectTransform rt)
        {
            viewSize = direction == LayoutDirection.Vertical ? rt.sizeDelta.y : rt.sizeDelta.x;
            viewSecondSize = direction == LayoutDirection.Vertical ? rt.sizeDelta.x : rt.sizeDelta.y;
        }
    }
}

#endif

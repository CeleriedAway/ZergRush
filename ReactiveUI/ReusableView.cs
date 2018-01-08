#if UNITY_5_3_OR_NEWER

using System;
using UnityEngine;

namespace ZergRush.ReactiveUI
{
    public class ReusableView : ConnectableObject
    {
        private RectTransform rect;
        public RectTransform rectTransform { get { return rect ?? (rect = GetComponent<RectTransform>()); } }
        public IDisposable currentMoveAnimation;
        [NonSerialized] public int indexInModel;
    }
}

#endif

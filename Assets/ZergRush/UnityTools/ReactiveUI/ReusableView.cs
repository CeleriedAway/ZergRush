using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZergRush.ReactiveUI
{
    public class ReusableView : ConnectableMonoBehaviour
    {
        protected RectTransform _rect;
        public RectTransform rectTransform { get { return _rect == null ? (_rect = GetComponent<RectTransform>()) : _rect; } }
        private Transform _tr;
        public Transform tr { get { return _tr == null ? (_tr = transform) : _tr; } }
        public IDisposable currentMoveAnimation;
        [NonSerialized] public int indexInModel;
        // for some inner impl
        [NonSerialized] public ReusableView prefabRef;
        public virtual bool autoDisableOnRecycle => rectTransform == null;
        public virtual bool setImpossiblePositionOnRecycle => rectTransform != null;

        public virtual void OnRecycle()
        {
        }

        public virtual void OnBeforeUsed()
        {
        }
    }
}

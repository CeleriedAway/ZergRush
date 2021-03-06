﻿#if UNITY_5_3_OR_NEWER

using System;
using UnityEngine;

namespace ZergRush.ReactiveUI
{
    public class TableDelegates<TView>
        where TView : ReusableView
    {
        // Used to animate dynamic insertion somehow
        public Action<TView> onInsert;

        // Used to animate dynamic remove. Returns time to delay recycle for proper animation.
        public Func<TView, float> onRemove;

        // Callback for proper view move animation if layout was changed.
        public Func<TView, Vector2, IDisposable> moveAnimation;

        public static TableDelegates<TView> WithRemoveAnimation(Func<TView, float> nRemove) =>
            new TableDelegates<TView> {onRemove = nRemove};

    }
}
#endif
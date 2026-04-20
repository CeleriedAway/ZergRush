
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZergRush.ReactiveUI
{
    public class TableDelegates<TView>
        where TView : ReusableView
    {
        // Called when view is shown after creation or recycle
        public Action<TView> onViewReady;
        
        // Used to animate dynamic insertion somehow
        public Action<TView> onInsertAnimated;

        // Used to animate dynamic remove. Returns time to delay recycle for proper animation.
        public Func<TView, float> onRemoveAnimated;

        // Callback for proper view move animation if layout was changed.
        public Func<TView, Vector2, IDisposable> moveAnimation;

        // Optional: animate ScrollRect content main-size change (instead of instant SetRectMainSize).
        // Called by ScrollRectViewPort with (scroll, targetMainSize). Implementation is free to tween,
        // for example only animating when targetSize > current (size increase) and snapping otherwise.
        public Action<ScrollRect, float> sizeAnimation;

        public static TableDelegates<TView> WithRemoveAnimation(Func<TView, float> nRemove) =>
            new TableDelegates<TView> {onRemoveAnimated = nRemove};

    }
}
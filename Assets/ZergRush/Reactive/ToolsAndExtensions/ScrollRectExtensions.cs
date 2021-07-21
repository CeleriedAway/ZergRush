#if UNITY_5_3_OR_NEWER

using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZergRush
{
    public static class ScrollRectExtensions
    {
        public static float RectMainSize(this ScrollRect scroll)
        {
            return scroll.horizontal
                ? scroll.GetComponent<RectTransform>().rect.width
                : scroll.GetComponent<RectTransform>().rect.height;
        }

        public static void SetRectMainSize(this ScrollRect scroll, float value)
        {
            if (scroll.horizontal)
                scroll.content.sizeDelta = new Vector2(value, scroll.content.sizeDelta.y);
            else
                scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, value);
        }

        public static void SetRectMainSize(this ScrollRect scroll, Vector2 size)
        {
            if (scroll.horizontal)
                scroll.content.sizeDelta = new Vector2(size.x, scroll.content.sizeDelta.y);
            else
                scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, size.y);
        }

        public static float GetRectSizeForScrollAxis(this ScrollRect scroll)
        {
            return (scroll.horizontal)
                ? scroll.GetComponent<RectTransform>().rect.width
                : scroll.GetComponent<RectTransform>().rect.height;
        }

        public static float GetViewportSizeForScrollAxis(this ScrollRect scroll)
        {
            float result;
            if (scroll.viewport == null)
            {
                result = scroll.GetRectSizeForScrollAxis();
            }
            else
            {
                result = (scroll.horizontal)
                    ? scroll.viewport.rect.width
                    : scroll.viewport.rect.height;
            }

            return result;
        }

        public static float GetNormalizedPosition(this ScrollRect scroll)
        {
            return (scroll.horizontal)
                ? scroll.horizontalNormalizedPosition
                : scroll.verticalNormalizedPosition;
        }

        public static void SetNormalizedPosition(this ScrollRect scroll, float normPos)
        {
            try
            {
                if (scroll.horizontal)
                {
                    scroll.horizontalNormalizedPosition = normPos;
                }
                else
                {
                    scroll.verticalNormalizedPosition = normPos;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
#endif
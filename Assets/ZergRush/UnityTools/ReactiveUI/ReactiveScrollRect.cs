using System;
using UnityEngine;
using UnityEngine.UI;
using ZergRush.ReactiveCore;
using ZergRush.ReactiveUI;


[RequireComponent(typeof(ScrollRect))]
public class ReactiveScrollRect : MonoBehaviour
{
	public Cell<float> scrollPos = new Cell<float>();
	public ScrollRect scroll => GetComponent<ScrollRect>();
	void Update()
	{
		scrollPos.value = scroll.horizontal
			? scroll.content.anchoredPosition.x
			: scroll.content.anchoredPosition.y;
	}

	public IViewPort CreateViewPort()
	{
        Rui.AdjustScrollRectContentAnchors(scroll, scroll.horizontal);
		return new ScrollRectViewPort(this);
	}
}
	

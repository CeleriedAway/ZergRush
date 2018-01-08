#if UNITY_5_3_OR_NEWER

using System;
using UnityEngine;
using UnityEngine.UI;
using ZergRush.ReactiveCore;


[RequireComponent(typeof(ScrollRect))]
public class ReactiveScrollRect : MonoBehaviour
{
	public Cell<float> scrollPos = new Cell<float>();
	[NonSerialized] public ScrollRect scroll;
	void Awake()
	{
		scroll = GetComponent<ScrollRect>();
	}
	void Update()
	{
		scrollPos.value = scroll.horizontal
			? scroll.content.anchoredPosition.x
			: scroll.content.anchoredPosition.y;
	}
}
	
#endif

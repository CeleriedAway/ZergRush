#if UNITY_5_3_OR_NEWER

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZergRush;
using ZergRush.ReactiveCore;


public class ConnectableObject : MonoBehaviour
{
	private Connections connections;

	public IDisposable addConnection
	{
		set
		{
			if (connections == null) connections = new Connections();
			connections.Add(value);
		}
	}

    public Action<IDisposable> connectionSink
    {
        get { return disp => addConnection = disp; }
    }

	public void DisconnectAll()
	{
        connections.DisconnectAll();
	}

	protected virtual void OnDestroy()
	{
        DisconnectAll();
	}
}

public static class UnityExtensions 
{
    class UnityActionDisposable : IDisposable
    {
        public UnityEvent e;
        public UnityAction action;

        public void Dispose()
        {
            if (e != null)
            {
                e.RemoveListener(action);
                e = null;
                action = null;
            }
        }
    }

    class UnityActionDisposable<T> : IDisposable
    {
        public UnityEvent<T> e;
        public UnityAction<T> action;

        public void Dispose()
        {
            if (e != null)
            {
                e.RemoveListener(action);
                e = null;
                action = null;
            }
        }
    }

	public static IEventStream ClickStream(this Button button)
	{
		if (button == null)
		{
			Debug.LogError("button is null!!!");
			return new AbandonedStream();
		}

		return new AnonymousEventStream((Action reaction) =>
		{
			var ua = new UnityAction(reaction);
			button.onClick.AddListener(ua);
			return new UnityActionDisposable {action = ua, e = button.onClick};
		});
	}

	public static void SetPointerEventListener(this EventTrigger trigger, EventTriggerType eventType, Action action){
		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = eventType;
		entry.callback.AddListener(eventData => action());
		trigger.triggers.Add(entry);
	}
	
    public static IDisposable OnPointerDown(this EventTrigger self, Action act)
    {
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };

        entry.callback.AddListener(data => {
            act();
        });

        self.triggers.Add(entry);
        return new AnonymousDisposable(() => self.triggers.Remove(entry));
    }


    public static void SetLayerRecursively(this GameObject go, int layerId)
    {
        go.layer = layerId;
        foreach (Transform c in go.transform)
        {
            SetLayerRecursively(c.gameObject, layerId);
        }
    }

    static public T GetOrAddComponent<T>(this GameObject obj) where T : Component
    {
	    var component = obj.GetComponent<T>();
	    if (component == null)
	    {
		    component = obj.AddComponent<T>();
	    }
	    return component;
    }


	public static ICell<bool> IsPressed(this GameObject go)
	{
		var cell = new Cell<bool>();
		var trigger = go.GetOrAddComponent<EventTrigger>();
		trigger.SetPointerEventListener(EventTriggerType.PointerDown, () => cell.value = true);
		trigger.SetPointerEventListener(EventTriggerType.PointerUp, () => cell.value = false);
		trigger.SetPointerEventListener(EventTriggerType.Cancel, () => cell.value = false);
		return cell;
	}

    public static IDisposable RunCoroutineWhile(this MonoBehaviour self, Func<IEnumerator> coro, ICell<bool> value)
    {
        Coroutine currentCoro = null;
        return value.Bind(val =>
        {
            if (val) currentCoro = self.StartCoroutine(coro());
            else if (currentCoro != null)
            {
                self.StopCoroutine(currentCoro);
                currentCoro = null;
            }
        });
    }
	
    public static void MakeUniqueMaterial(this Image img)
    {
        img.material = UnityEngine.Object.Instantiate(img.material) as Material;
    }
    
    public static T MakeInstance<T>(this GameObject obj)
        where T : Component
    {
        return (UnityEngine.Object.Instantiate(obj) as GameObject).GetComponent<T>();
    }

    public static GameObject MakeInstance(this GameObject obj)
    {
        return (UnityEngine.Object.Instantiate(obj) as GameObject);
    }

    public static void ResetTransform(this GameObject obj)
    {
        ResetTransform(obj.transform);
    }

    public static void ResetTransform(this Component c)
    {
        c.transform.localPosition = Vector3.zero;
        c.transform.localRotation = Quaternion.identity;
        c.transform.localScale = Vector3.one;
    }

    public static void SetPositionX(this Transform t, float x)
    {
        var position = t.position;
        position.x = x;
        t.position = position;
    }
    public static void SetLocalPositionX(this Transform t, float x)
    {
        var position = t.localPosition;
        position.x = x;
        t.localPosition = position;
    }

    public static void SetAnchoredPositionX(this RectTransform t, float x)
    {
        var position = t.anchoredPosition;
        position.x = x;
        t.anchoredPosition = position;
    }

    public static void SetAnchoredPositionY(this RectTransform t, float y)
    {
        var position = t.anchoredPosition;
        position.y = y;
        t.anchoredPosition = position;
    }

    public static void SetPositionY(this Transform t, float y)
    {
        var position = t.position;
        position.y = y;
        t.position = position;
    }
    public static void SetLocalPositionY(this Transform t, float y)
    {
        var position = t.localPosition;
        position.y = y;
        t.localPosition = position;
    }


    public static void SetPositionZ(this Transform t, float z)
    {
        var position = t.position;
        position.z = z;
        t.position = position;
    }
    public static void SetLocalPositionZ(this Transform t, float z)
    {
        var position = t.localPosition;
        position.z = z;
        t.localPosition = position;
    }

	public static void SetActiveSafe(this GameObject obj, bool active)
    {
        if (!obj) return;
        if (obj.activeSelf == active) return;
        obj.SetActive(active);
    }

	public static void SetActiveSafe(this Component obj, bool active)
    {
        if (!obj) return;
        if (obj.gameObject.activeSelf == active) return;
        obj.gameObject.SetActive(active);
    }

    public static Sprite LoadSprite(string name)
    {
        Sprite result = null;
        WWW www = new WWW(WWW.EscapeURL("file://" + Application.dataPath + "/" + name));
        while (!www.isDone) { }

        if (www.texture != null)
            result = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
        else
            Debug.Log("#Failed loading sprite " + name);

        return result;
    }
    
	public static void SetTransparency(this Image img, float transparency)
	{
		var color = img.color;
		color.a = transparency;
		img.color = color;
	}

	public static void RestartCurrentStateWIthRandomSpeedAndShift(this Animator animator)
	{
		animator.speed = UnityEngine.Random.Range(0.8f, 1.2f);
		AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
		animator.Play(state.fullPathHash, -1, UnityEngine.Random.Range(0f, 2f));
	}

    public static Coroutine ExecuteWithDelay(this MonoBehaviour self, float delay, Action action)
    {
        if (delay <= 0)
        {
            action();
            return null;
        }
        return self.StartCoroutine(DelayCoro(action, delay));
    }
    
    public static IEnumerable<T> FlagsToList<T>(int mask)
    {
        if (typeof(T).IsSubclassOf(typeof(Enum)) == false)
            throw new ArgumentException();

        return Enum.GetValues(typeof(T))
                             .Cast<int>()
                             .Where(Mathf.IsPowerOfTwo)
                             .Where(m => ((int)mask & (int)m) != 0)
                             .Cast<T>();
    }
    
    static IEnumerator DelayCoro(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action();
    }
    
    static public float EaseOut(float t)
    {
        return -t * (t - 2);
    }

    static public float EaseIn(float t)
    {
        return t * t * t;
    }

    static public float EaseElasticOut(float t)
    {
        var p = 0.3F;
        return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
    }

    static public float EaseOut(float from, float to, float t)
    {
        return Mathf.Lerp(from, to, -t * (t - 2));
    }

    static public float EaseIn(float from, float to, float t)
    {
        return Mathf.Lerp(from, to, t * t * t);
    }

    public static float TenPower(int power)
    {
        return Mathf.Pow(10, power);
    }

    public static IDisposable SetTextContent<T>(this Text text, ICell<T> val)
    {
        return val.Bind(v => text.text = v.ToString());
    }
    public static IDisposable SetVisibility(this MonoBehaviour obj, ICell<bool> val)
    {
        return val.Bind(obj.SetActiveSafe);
    }
    
}

#endif

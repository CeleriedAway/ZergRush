using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZergRush;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;
using ZergRush.ReactiveUI;

namespace ZergRush.ReactiveUI
{
    public static class PresentTools
    {
        [MustUseReturnValue]
        public static IDisposable PresentCell<T>(this ICell<T> val, Action<T, UIChanges> changes)
        {
            var changeCollection = new UIChanges();
            return new DoubleDisposable
            {
                First = val.Bind(v =>
                {
                    changeCollection.Dispose();
                    changes(v, changeCollection);
                }),
                Second = changeCollection
            };
        }
        
        [MustUseReturnValue]
        public static IDisposable PresentEach<TData>(
            this IReactiveCollection<TData> data, Action<TData, UIChanges> present)
        {
            var disp = new DoubleDisposable();
            var dict = new Dictionary<TData, UIChanges>();
            disp.First = data.BindEach(d =>
            {
                if (d == null) return;
                var conns = new UIChanges();
                dict[d] = conns;
                present(d, conns);
            }, d =>
            {
                if (d == null) return;
                dict[d].Dispose();
                dict.Remove(d);
            });
            disp.Second = new AnonymousDisposable(() =>
            {
                foreach (var dictValue in dict.Values)
                {
                    dictValue.Dispose();
                }
            });
            return disp;
        }

    }

    [GenIgnore]
    public class UIChanges : IDisposable, IConnectionSink
    {
        public string debugName;
        public Connections connections = new Connections();

        public UIChanges()
        {
        }

        public UIChanges(Connections conns)
        {
            connections = conns;
        }

        public void Bind<T>(ICell<T> e, Action<T> callback)
        {
            connections += e.Bind(callback);
        }

        public void PresentCell<T>(ICell<T> e, Action<T, UIChanges> callback)
        {
            connections += e.PresentCell(callback);
        }

        public void Subscribe(IEventStream e, Action callback)
        {
            connections += e.Subscribe(callback);
        }

        public void Subscribe<T>(IEventStream<T> e, Action<T> callback)
        {
            connections += e.Subscribe(callback);
        }

        public void StartCoroutine(MonoBehaviour executer, IEnumerator coro)
        {
            var execution = executer.StartCoroutine((IEnumerator) coro);
            if (execution == null)
            {
                return;
            }

            connections += () => executer.StopCoroutine(execution);
        }

        public virtual void Dispose()
        {
            for (var i = connections.Count - 1; i >= 0; i--)
            {
                var connection = connections[i];
                connection.Dispose();
            }

            connections.Clear();
        }

        public void SetAnimationClip(Component anim, AnimationClip clip)
        {
            var prevState = anim.gameObject.GetOrAddComponent<Animation>().clip;
            connections += new AnonymousDisposable(() =>
            {
                if (anim) anim.PlaySimpleAnimation(prevState);
            });
            anim.PlaySimpleAnimation(clip);
        }

        public T Instantiate<T>(T prefab, Transform parent, bool worldPosStay = false) where T : Component
        {
            var obj = GameObject.Instantiate(prefab, parent, worldPosStay);
            connections += new AnonymousDisposable(() =>
            {
                if (obj) GameObject.Destroy(obj.gameObject);
            });
            return obj;
        }
        
        public GameObject Instantiate(GameObject prefab, Transform parent, bool worldPosStay = false)
        {
            var obj = GameObject.Instantiate(prefab, parent, worldPosStay);
            connections += new AnonymousDisposable(() =>
            {
                if (obj) GameObject.Destroy(obj);
            });
            return obj;
        }

        public T Instantiate<T>(T prefab) where T : Component
        {
            var obj = GameObject.Instantiate(prefab);
            connections += new AnonymousDisposable(() =>
            {
                if (obj) GameObject.Destroy(obj.gameObject);
            });
            return obj;
        }

        public void SetBehaviourEnabled(Behaviour behav, bool enabled)
        {
            var prevState = behav.enabled;
            connections += new AnonymousDisposable(() =>
            {
                if (behav) behav.enabled = prevState;
            });
            behav.enabled = enabled;
        }

        public void SetActive(Component behav, bool active)
        {
            var prevState = behav.gameObject.activeInHierarchy;
            connections += new AnonymousDisposable(() =>
            {
                if (behav) behav.gameObject.SetActive(prevState);
            });
            behav.gameObject.SetActiveSafe(active);
        }

        public void SetActive(GameObject obj, bool active)
        {
            var prevState = obj.activeInHierarchy;
            connections += new AnonymousDisposable(() =>
            {
                if (obj) obj.SetActive(prevState);
            });
            obj.SetActiveSafe(active);
        }

        public void SetSprite(Image image, Sprite sprite)
        {
            var prevState = image.sprite;
            connections += new AnonymousDisposable(() =>
            {
                if (image) image.sprite = prevState;
            });
            image.sprite = sprite;
        }

        public void SetColor(Image image, Color color)
        {
            var prevState = image.color;
            connections += new AnonymousDisposable(() =>
            {
                if (image) image.color = prevState;
            });
            image.color = color;
        }
        public void SetColor(SpriteRenderer image, Color color)
        {
            var prevState = image.color;
            connections += new AnonymousDisposable(() =>
            {
                if (image) image.color = prevState;
            });
            image.color = color;
        }

        public void SetText(TextMeshProUGUI textMesh, string text)
        {
            var prevState = textMesh.text;
            connections += new AnonymousDisposable(() => textMesh.text = prevState);
            textMesh.text = text;
        }

        public void SetValue<T>(Cell<T> cell, T value)
        {
            var prevState = cell.value;
            connections += new AnonymousDisposable(() => cell.value = prevState);
            cell.value = value;
        }
        
        //need prevCursor, because Unity doesn't have a way to get current cursor
        public void SetCursor(Texture2D newCursor, Texture2D prevCursor = null, Vector2Int hotspot = default, Vector2Int prevHotspot = default)
        {
            Cursor.SetCursor(newCursor, hotspot, CursorMode.Auto);
            connections += new AnonymousDisposable(() =>
                Cursor.SetCursor(prevCursor, prevHotspot, CursorMode.Auto));
        }

        public void AddConnection(IDisposable connection)
        {
            connections.AddConnection(connection);
        }

        public void SetFill(Image hpSlider, float newFill)
        {
            var prevState = hpSlider.fillAmount;
            connections += new AnonymousDisposable(() => hpSlider.fillAmount = prevState);
            hpSlider.fillAmount = newFill;
        }
    }
}
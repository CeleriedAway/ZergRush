using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;
using Object = UnityEngine.Object;

namespace ZergRush.ReactiveUI
{
    public static class PrefabRef
    {
        public static PrefabRef<TView> ToPrefabRef<TView>(this TView view) where TView : ReusableView
        {
            if (view == null)
            {
                LogSink.errLog($"prefab is null in ToPrefabRef function for type {typeof(TView).Name}");
            }
            return new PrefabRef<TView> {prefab = view};
        }
    }
    
    public struct PrefabRef<TView> : IEquatable<PrefabRef<TView>> where TView : ReusableView
    {
        public bool Equals(PrefabRef<TView> other)
        {
            return Equals(type, other.type) && string.Equals(name, other.name) && EqualityComparer<TView>.Default.Equals(prefab, other.prefab);
        }

        public override bool Equals(object obj)
        {
            return obj is PrefabRef<TView> other && Equals(other);
        }

        public static bool operator ==(PrefabRef<TView> left, PrefabRef<TView> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PrefabRef<TView> left, PrefabRef<TView> right)
        {
            return !left.Equals(right);
        }

        public Type type;
        public string name;
        public TView prefab;

        public static PrefabRef<TView> Auto()
        {
            return new PrefabRef<TView> {type = typeof(TView)};
        }
        public static PrefabRef<TView> ByType(Type type)
        {
            return new PrefabRef<TView> {type = type};
        }

        public static PrefabRef<TView> ByName(string name)
        {
            return new PrefabRef<TView> {name = name};
        }

        public Type ExtractType()
        {
            if (type != null) return type;
            if (prefab != null) return prefab.GetType();
            return null;
        }
        public string ExtractName()
        {
            if (name != null) return name;
            if (type != null) return type.Name;
            if (prefab != null) return prefab.name;
            return null;
        }
        
        public bool Valid() => name != null || type != null || prefab != null;

        public TView ExtractPrefab(Transform parent)
        {
            TView extractPrefab = null;
            if (prefab != null) extractPrefab = prefab as TView;
            else if (type != null)
            {
                var childCount = parent.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var childView = parent.GetChild(i).GetComponent(type) as TView;
                    if (childView != null)
                    {
                        if (childView.prefabRef != null)
                        {
                            extractPrefab = (TView)childView.prefabRef;
                            break;
                        }
                        else if (extractPrefab == null)
                        {
                            extractPrefab = childView;
                        }
                    }
                    if (extractPrefab != null) break;
                }
            }
            else if (name != null) extractPrefab = parent.Find(name)?.GetComponent<TView>();
            else
            {
                // No prefab found
            }

            if (extractPrefab == null)
            {
                var res = name ?? type?.Name;
                if (res != null) extractPrefab = Resources.Load<TView>(res);
            }

            return extractPrefab;
        }

        public static implicit operator PrefabRef<TView>(string name) => ByName(name);
        public static implicit operator PrefabRef<TView>(Type type) => ByType(type);
        public static implicit operator PrefabRef<TView>(TView view) => PrefabRef.ToPrefabRef(view);

        public override int GetHashCode()
        {
            if (type != null) return type.GetHashCode();
            if (name != null) return name.GetHashCode();
            if (prefab != null) return prefab.GetHashCode();
            return 0;
        }

        public override string ToString()
        {
            return $"{nameof(type)}: {type}, {nameof(name)}: {name}, {nameof(prefab)}: {prefab}";
        }
    }
}
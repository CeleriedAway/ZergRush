using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZergRush.ReactiveCore;
using Object = UnityEngine.Object;

namespace ZergRush.ReactiveUI
{
    public static class PrefabRef
    {
        public static PrefabRef<TView> ToPrefabRef<TView>(this TView view) where TView : ReusableView
        {
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

        public TView ExtractPrefab(Transform parent)
        {
            TView view = null;
            if (prefab != null) view = prefab as TView;
            else if (type != null) view = parent.GetComponentInChildren(type, true) as TView;
            else if (name != null) view = parent.Find(name).GetComponent<TView>();
            else view = parent.GetComponentInChildren<TView>();

            if (view == null)
            {
                var res = name ?? type?.Name;
                if (res != null) view = Resources.Load<TView>(res);
            }

            return view;
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
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZergRush.Alive;
using UnityEngine;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.CodeGen
{
	public static partial class CodeGen
	{
        public static bool IsControllable(this Type t)
        {
            return t.ReadGenFlags() != GenTaskFlags.None;
        }
		
        public static bool IsList(this Type t)
        {
	        //return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);
	        return t.IsGenericType && !t.IsGenericOfType(typeof(RefListMk2<>)) && typeof(IList<>).EnrichGeneric(t.FirstGenericArg()).IsAssignableFrom(t);
        }
        public static bool IsReadOnlyList(this Type t)
        {
	        //return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);
	        return t.IsGenericType && typeof(IReadOnlyList<>).EnrichGeneric(t.FirstGenericArg()).IsAssignableFrom(t);
        }
        public static bool IsDictionary(this Type t)
        {
	        return
		        t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

		public static bool IsCollection(this Type t)
		{
			return t.IsList() || t.IsArray || t.IsDictionary();
		}

		public static Type[] CollectionElemTypes(this Type t)
		{            
			if (t.IsList() || t.IsDictionary()) return t.GetGenericArguments();
			if (t.IsArray) return new Type[] { t.GetElementType() };
            throw new NotImplementedException();
		}


        public static bool IsCell(this Type t)
        {
	        return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Cell<>) || (t.BaseType != null && t.BaseType.IsCell());
        }

		public static Type FirstGenericArg(this Type t)
		{
			if (t.IsArray) return t.GetElementType();
			if (t.IsGenericType == false) return t.BaseType.FirstGenericArg();
			return t.GetGenericArguments()[0];
		}
		public static Type SecondGenericArg(this Type t)
		{
			return t.GenericTypeArguments[1];
		}

        public static bool IsString(this Type t)
        {
            return t == typeof(string);
        }

        public static bool IsImmutableValueType(this Type t)
        {
	        return t.IsPrimitive || t.IsEnum || t.IsFix64();
        }
		
        static bool AnyAndOnlyOneChildWithTag<Tag>(this Type t, out DataInfo childInfo) where Tag : Attribute
        {
            bool found = false;
            childInfo = new DataInfo();
            //foreach (var field in t.GetFields().Concat(t.GetProperties().Select(v => (MemberInfo) v)))
            foreach (var field in t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.HasAttribute<Tag>())
                {
                    if (found)
                    {
                        CodeGen.Error($"Multiple attributes of type {typeof(Tag)} detected");
                        continue;
                    }
                    found = true;
                    childInfo = t.GetMembersForCodeGen(inheretedMembers: true, ignoreCheck:false).First(v => v.baseAccess == field.Name);
                }
            }
            return found;
        }

        public static bool HasInHierarchy(this Type t, Func<Type, bool> predicate)
        {
            return t.ParentWithPredicate(predicate) != null;
        }

        static Type ParentWithTag<T>(this Type t) where T : Attribute
        {
            return ParentWithPredicate(t, p => p.HasAttribute<T>());
        }
        public static T FindTagInHierarchy<T>(this Type t) where T : Attribute
        {
            return ParentWithPredicate(t, p => p.HasAttribute<T>())?.GetCustomAttribute<T>(inherit: false);
        }
        static Type ParentWithPredicate(this Type t, Func<Type, bool> predicate)
        {
            while (t != null)
            {
                if (predicate(t)) return t;
                t = t.BaseType;
            }

            return null;
        }
        
        static bool AnyAndOnlyOneChildWithName(this Type t, string name, out DataInfo childInfo)
        {
            bool found = false;
            childInfo = new DataInfo();
            foreach (var valueInfo in t.GetMembersForCodeGen(inheretedMembers: true))
            {
                if (valueInfo.name == name)
                {
                    if (found)
                    {
                        CodeGen.Error($"Type {t.RealName(true)} has several fields with name {name}");
                        continue;
                    }

                    childInfo = valueInfo;
                    found = true;
                }
            }
            return found;
        }

        static bool HasPrivateLivableMembersInHierarchy(this Type t)
        {
            Type parent = t.BaseType;
            while (parent.IsControllable())
            {
                if (parent.GetMembersForCodeGen(GenTaskFlags.LifeSupport).Any(v => v.isPrivate))
                    return true;
            }
            return false;
        }
		
        static bool IsLoadableConfig(this Type t)
        {
            return t.IsConfig();
        }
        
        public static bool IsImmutableData(this Type t)
        {
            return t.IsImmutableType() || t.IsLoadableConfig() || t.HasAttribute<Immutable>();
        }

		public static bool IsGenericOfType(this Type t, Type genericType)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == genericType;
        }


		public static bool IsChildOf<T>(this Type t)
		{
			return typeof(T).IsAssignableFrom(t);
		}
        public static bool IsImmutableType(this Type t)
        {
	        return t.IsPrimitive || t.IsEnum || t == typeof(string)
	               || t == typeof(byte[]) || t.IsFix64() || t.IsNullable();
        }
        public static bool IsStruct(this Type t)
        {
	        return t.IsValueType;
        }

        public static string NewInstance(this Type t)
        {
            if (t == typeof(string)) return "string.Empty";
            return $"new {t.RealName(true)}()";
        }
		
		//static Random rand = new Random();

		public static string AddOrCondition(string prev, string newCondition)
		{
			if (string.IsNullOrEmpty(prev)) return newCondition;
			else return prev + " || " + newCondition;
		}

	    
        public static string FileName(this Type t)
        {
	        var str = t.UniqueName();
	        return str;
//            if (t.IsGenericType)
//            {
//                var name = t.Name;
//                name = name.Substring(0, name.Length - 2);
//                name += $"_{t.GetGenericArguments().Select(a => a.RealName()).PrintCollection("_")}";
//                return name;
//            }
//            return t.Name;
        }

		static void _ReadGenericArguments(List<Type> list, Type t)
		{
			if (t.IsGenericType == false) return;
			foreach (var genericArgument in t.GetGenericArguments())
			{
				if (genericArgument.IsGenericParameter)
				{
					list.Add(genericArgument);
				}
				else
				{
					_ReadGenericArguments(list, genericArgument);
				}
			}
		}
		
		public static List<Type> UnknownGenericArguments(this Type t)
		{
			var unknownParameters = new List<Type>();
			_ReadGenericArguments(unknownParameters, t);
			return unknownParameters;
		}
		
		public static string GenericParametersSuffix(this Type t)
		{
			var unknownParameters = UnknownGenericArguments(t);
            if (unknownParameters.Count <= 0) return "";
			return $"<{unknownParameters.PrintCollection()}>";
		}


		public static Type EnrichGeneric(this Type t, Type genType)
		{
			if (t.IsGenericType == false) return t;
			if (t.IsConstructedGenericType) return t;
			return t.MakeGenericType(genType);
		}
		
		
		
		public static string GenericParametersConstraints(this Type t)
		{
			if (!t.IsGenericType) return ""; 
			return string.Join(Environment.NewLine, t.UnknownGenericArguments()
				.Select(p =>
				{
					var constraints = p.GetGenericParameterConstraints().ToList();
					string printedConstr = constraints.Select(c => c.RealName(true)).Distinct().PrintCollection();
					if ((p.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
					{
						printedConstr += $", new()";
					}

					if (string.IsNullOrEmpty(printedConstr))
						return "";
					return $"where {p.Name} : {printedConstr}";
				}));
		}

        public static string UniqueName(this Type t, bool withNamespace = true)
        {
			if (Nullable.GetUnderlyingType(t) != null)
			{
				return UniqueName(Nullable.GetUnderlyingType(t)) + "?";
			}

			var name = (string)null;
			if (withNamespace && t.Namespace != null)
			{
				name = $"{t.Namespace.Replace('.', '_')}_{t.Name}";
			}
			else
			{
				name = t.Name;
			}
			if (t.IsArray)
			{
				return name.Substring(0, name.Length - 2) + "_Array";
			}

			if (t.IsGenericType)
			{
				name = name.Substring(0, name.Length - 2);
				name += $"_{t.GetGenericArguments().Select(a => a.RealName(!a.IsGenericParameter && withNamespace)).PrintCollection("_")}";
			}
			return name;
        }
		
		public static string ClearName(this Type t)
		{
			if (t.IsGenericType)
			{
				return t.Name.Substring(0, t.Name.Length - 2);
			}
			return t.Name;
		}
		
	}
}

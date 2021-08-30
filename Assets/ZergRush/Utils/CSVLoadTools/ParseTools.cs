using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using ZergRush;

public static class ParseTools
{
    public static bool IsNullOrWhitespace(this string str)
    {
        return String.IsNullOrWhiteSpace(str);
    }

    public static bool IsNullOrEmpty(this string str)
    {
        return String.IsNullOrEmpty(str);
    }

    public static bool HasPrefixAndStrip(this string str, string prefix, out string suff)
    {
        if (str.StartsWith(prefix))
        {
            suff = str.Substring(prefix.Length);
            return true;
        }

        suff = str;
        return false;
    }
    public static bool HasSuffixAndStrip(this string str, string suff, out string pref)
    {
        if (str.EndsWith(suff))
        {
            pref = str.Substring(0, str.Length - suff.Length);
            return true;
        }
        pref = str;
        return false;
    }

    public static string StripSuffix(this string str, string suffix)
    {
        var suff = str.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
        if (suff == -1) return str;
        return str.Substring(0, suff);
    }

    public static float ParseFloatStrict(this string str)
    {
        if (string.IsNullOrEmpty(str) || !float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var f)
        ) throw new ZergRushException($"can't parse float from string\"{str}\"");
        return f;
    }

    public static int ParseIntStrict(this string str)
    {
        str = str.Replace('@', '-');
        if (string.IsNullOrEmpty(str) || !int.TryParse(str, out var i))
            throw new ZergRushException($"can't parse int from string\"{str}\"");
        return i;
    }

    public static int ParsePartToPercent(this string str)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        return Mathf.RoundToInt(float.Parse(str, CultureInfo.InvariantCulture) * 100);
    }

    public static int ParsePartToPercentStrict(this string str)
    {
        if (string.IsNullOrEmpty(str)) throw new ZergRushException($"can't parse part to percent from string\"{str}\"");
        return Mathf.RoundToInt(float.Parse(str, CultureInfo.InvariantCulture) * 100);
    }

    public static int ParseInt(this string str)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        return int.TryParse(str, out var i) ? i : 0;
    }

    public static bool ParseBool(this string str)
    {
        var lower = str?.ToLower();
        return lower == "true" || lower == "yes";
    }


    static int[] ParseIntArray(this string str)
    {
        return str.Split(',').Select(s => ParseInt(s)).ToArray();
    }


    public static IEnumerable<TEnum> EnumValues<TEnum>()
    {
        return Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
    }

    public static string TextToCamelCase(this string str)
    {
        if (str == null) return null;
        var builder = new StringBuilder();
        str = str.Replace(".", "");
        foreach (var s in str.Split(' '))
        {
            builder.Append(s.UpperFirstLetter());
        }
        return builder.ToString();
    }

    public static string UpperFirstLetter(this string str)
    {
        if (str == null)
            return null;

        if (str.Length > 1)
            return char.ToUpper(str[0]) + str.Substring(1);

        return str.ToUpper();
    }

    public static string CamelCaseToReadableText(this string name)
    {
        if (name == null) return null;
        return Regex.Replace(name, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");
    }

    public static string CamelCaseToUnderscored(this string name)
    {
        return Regex.Replace(name, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1_");
    }

    public static TEnum ParseEnumFlagsStrict<TEnum>(this string str) where TEnum : struct
    {
        int val = 0;
        if (str.IsNullOrEmpty() == false)
            foreach (var s in str.Split(' ', ','))
            {
                if (s.IsNullOrWhitespace()) continue;
                val = val | Convert.ToInt32(s.ParseEnumStrict<TEnum>());
            }

        return (TEnum) Enum.ToObject(typeof(TEnum), val);
    }
    
    public static List<TEnum> ParseEnumListStrict<TEnum>(this string str) where TEnum : struct
    {
        var result = new List<TEnum>();
        if (str.IsNullOrEmpty() == false)
        {
            foreach (var s in str.Split(' ', ','))
            {
                if (s.IsNullOrWhitespace()) continue;
                result.Add(s.ParseEnumStrict<TEnum>());
            }
        }

        return result;
    }


    public static TEnum ParseEnumStrict<TEnum>(this string str) where TEnum : struct
    {
        TEnum val;
        if (Enum.TryParse(str.TextToCamelCase(), true, out val) == false)
        {
            throw new ZergRushException($"enum of type {typeof(TEnum).Name} could not be parsed from string \"{str}\"");
        }

        return val;
    }

    public static TEnum ParseEnum<TEnum>(this string str, TEnum def = default) where TEnum : struct
    {
        TEnum val;
        if (Enum.TryParse(str.TextToCamelCase(), true, out val) == false)
        {
            return def;
        }
        return val;
    }

    public struct IgnoreCaseComp
    {
        public bool Equals(IgnoreCaseComp other)
        {
            return string.Equals(str, other.str, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is IgnoreCaseComp && Equals((IgnoreCaseComp) obj);
        }

        public override int GetHashCode()
        {
            return (str != null ? str.GetHashCode() : 0);
        }

        public string str;

        public static bool operator ==(IgnoreCaseComp self, string other)
        {
            return string.Equals(self.str, other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(IgnoreCaseComp self, string other)
        {
            return !(self == other);
        }
    }

    // Make special ignore case comparator in order of sintax sugar
    public static IgnoreCaseComp IC(this string str) => new IgnoreCaseComp {str = str};
}
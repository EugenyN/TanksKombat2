using System;
using System.Text.RegularExpressions;

namespace TanksKombat2.Client
{
    public static class EnumExtensions
    {
        public static string ToDisplayString(this Enum value)
        {
            return Regex.Replace(value.ToString(), "([a-z])([A-Z])", "$1 $2");
        }

        public static TEnum FromDisplayString<TEnum>(this string value) where TEnum : struct, Enum
        {
            return Enum.Parse<TEnum>(value.Replace(" ", ""));
        }
    }
}

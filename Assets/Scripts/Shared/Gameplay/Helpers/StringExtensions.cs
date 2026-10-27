using Unity.Collections;
using UnityEngine;

namespace TanksKombat2.Shared
{
    public static class StringExtensions
    {
        public static string FormatWithColor(this string text, Color color)
        {
            return  $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }
        
        public static string FormatWithColor(this FixedString32Bytes text, Color color)
        {
            return text.ToString().FormatWithColor(color);
        }
    }
}
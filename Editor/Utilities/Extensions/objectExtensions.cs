using System;

namespace UIToolkit.Editor.Utilities
{
    public static class objectExtensions
    {
        public static string ToStaticString(this object @object)
        {
            switch (@object)
            {
                case Enum @enum:
                    return $"{@enum.GetType().GetSafeName()}.{@enum}";
                case float single:
                    return $"{single}f";
                default:
                    return @object.ToString();
            }
        }
    }
}
using System;
using System.Reflection;

namespace UIToolkit.Editor.Utilities
{
    //Based on: http://dotnetfollower.com/wordpress/2012/12/c-how-to-set-or-get-value-of-a-private-or-internal-property-through-the-reflection/
    public static class ReflectionHelper
    {
        private static FieldInfo GetPropertyInfo(Type type, string propertyName)
        {
            FieldInfo fieldInfo;
            do
            {
                fieldInfo = type.GetField(propertyName, 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (fieldInfo == null && type != null);
            return fieldInfo;
        }

        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            
            var objType = obj.GetType();
            var fieldInfo = GetPropertyInfo(objType, propertyName);
            
            if (fieldInfo == null)
                throw new ArgumentOutOfRangeException(nameof(propertyName), $"Couldn't find property {propertyName} in type {objType.FullName}");
            
            return fieldInfo.GetValue(obj);
        }
    }
}
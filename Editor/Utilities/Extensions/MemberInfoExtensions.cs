using System;
using System.Reflection;

namespace UIToolkit.Editor.Utilities
{
    public static class MemberInfoExtensions
    {
        public static object GetValue(this MemberInfo memberInfo, object forObject)
        {
            if (memberInfo == null)
                return default;
            
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(forObject);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(forObject);
                case MemberTypes.Method:
                    return ((MethodInfo)memberInfo).Invoke(forObject, null);
                default:
                    throw new NotImplementedException();
            }
        } 
    }
}
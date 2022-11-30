using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UIToolkit.Editor.Utilities
{
    public static class TypeExtensions
    {
        private static BindingFlags BIND_ATTRIBUTE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static |
                                                    BindingFlags.GetField | BindingFlags.GetProperty |
                                                    BindingFlags.NonPublic | BindingFlags.Default;
        
        private static Dictionary<Type, bool> s_TypeHasCustomInspectorDict;
        private static IEnumerable<Type> s_EditorAssemblyTypes;
        private static IEnumerable<CustomEditor> s_SavedCustomEditors;
        private static IEnumerable<CustomPropertyDrawer> s_SavedPropertyDrawers;
        
        public static bool HasCustomEditor(this Type type)
        {
            if (s_TypeHasCustomInspectorDict == null)
            {
                s_TypeHasCustomInspectorDict = new Dictionary<Type, bool>();
                
                s_EditorAssemblyTypes = from a in AppDomain.CurrentDomain.GetAssemblies()
                    //where a.FullName.Contains("UnityEditor")
                    from t in a.GetTypes()
                    select t;
                
                s_SavedCustomEditors =
                    from t in s_EditorAssemblyTypes
                    let attributes = t.GetCustomAttributes<CustomEditor>()
                    where attributes != null && attributes.Count() != 0
                    from t2 in attributes
                    select t2;
            
                s_SavedPropertyDrawers =
                    from t in s_EditorAssemblyTypes
                    let attributes = t.GetCustomAttributes<CustomPropertyDrawer>()
                    where attributes != null && attributes.Count() != 0
                    from t2 in attributes
                    select t2;
            }

            if (s_TypeHasCustomInspectorDict.TryGetValue(type, out var hasCustomEditor))
                return hasCustomEditor;
           
            
            var hasEditor = s_SavedCustomEditors.Any(x => ((Type)x.GetPropertyValue("m_InspectedType")) == type);
            var hasPropertyDrawer = s_SavedPropertyDrawers.Any(x => ((Type)x.GetPropertyValue("m_Type")) == type);

            hasCustomEditor = hasEditor || hasPropertyDrawer;

            s_TypeHasCustomInspectorDict.Add(type, hasCustomEditor);

            return hasCustomEditor;
        }

        /// <summary>
        /// Gets Type Fullname with the + char replaced with .
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSafeName(this Type type)
        {
            return type.FullName.Replace('+', '.');
        }

        /// <summary>
        /// This function is used by generated code
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public static MemberInfo GetFirstMember(this Type type, in string memberName)
        {
            var outMember = type.GetMember(memberName, BIND_ATTRIBUTE)
                .FirstOrDefault();
            
            if(outMember == null)
                Debug.LogError($"Cannot find member {memberName} within {type.GetSafeName()}");

            return outMember;
        }
        
        public static bool IsField(this Type type, in string memberName)
        {
            var memberInfo = type.GetMember(memberName, BIND_ATTRIBUTE).FirstOrDefault();

            if (memberInfo == null)
                return false;

            return memberInfo.MemberType == MemberTypes.Field;
        }
    }
}
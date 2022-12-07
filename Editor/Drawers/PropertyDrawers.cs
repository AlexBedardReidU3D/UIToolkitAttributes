using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UIToolkit.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using ReadOnlyAttribute = UIToolkit.Experimental.ReadOnlyAttribute;

namespace UIToolkit.Editor.experimental
{
    //https://github.cds.internal.unity3d.com/unity/unity/blob/trunk/Editor/Mono/ScriptAttributeGUI/Implementations/PropertyDrawers.cs
    //https://github.cds.internal.unity3d.com/unity/unity/blob/trunk/Runtime/Export/PropertyDrawer/PropertyAttribute.cs
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    internal sealed class ReadonlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.miniproject.uitoolkitattributes/Editor/Styles/UXMLStyles.uss");
            ReadOnlyAttribute readOnlyAttribute = (ReadOnlyAttribute)attribute;

            var customLabelAttribute = fieldInfo.GetCustomAttribute<CustomLabelAttribute>();
            var label = customLabelAttribute == null ? fieldInfo.Name : customLabelAttribute.GetText();

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                {
                    var intSlider = new IntegerField(label)
                    {
                        bindingPath = property.propertyPath
                    };

                    intSlider.SetEnabled(false);
                    intSlider.AddToClassList("read-only");
                    intSlider.styleSheets.Add(styleSheet);
                    return intSlider;
                }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

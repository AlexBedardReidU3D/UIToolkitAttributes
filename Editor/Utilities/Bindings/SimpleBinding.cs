using System.Reflection;
using UnityEngine.UIElements;

namespace UIToolkit.Editor.Utilities
{
    /// <summary>
    /// Used by Generated code to store references to Visual Elements & the members its meant to bind with.
    /// By default, the CurrentValue is equal to default. This allows an immediate update when requested.
    /// </summary>
    public class SimpleBinding
    {
            public readonly VisualElement SavedElement;
            public readonly MemberInfo MemberInfo;
        
            public object CurrentValue;

            public SimpleBinding(
                in VisualElement savedElement,
                in MemberInfo memberInfo)
            {
                SavedElement = savedElement;
                MemberInfo = memberInfo;

                CurrentValue = default;
            }

            public bool RequiresUpdate(in object newValue)
            {
                if (CurrentValue == null)
                    return true;
            
                return CurrentValue.Equals(newValue) == false;
            }
    }
}
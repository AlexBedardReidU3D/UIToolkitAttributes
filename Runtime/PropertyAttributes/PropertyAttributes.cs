using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UIToolkit.Experimental
{
    [System.AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : PropertyAttribute
    {
        public readonly string name;
        public readonly string function;

        public ButtonAttribute(string name, [CallerMemberName] string propName = null)
        {
            this.name = name;
            function = propName;
        }
    }
    
    

    
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class ReadOnlyAttribute : PropertyAttribute
    {

        // Attribute used to make a float or int variable in a script be restricted to a specific range.
        public ReadOnlyAttribute() { }
    }
}

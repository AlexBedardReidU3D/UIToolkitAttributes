using System;
using System.Diagnostics;
using System.Linq;

namespace UIToolkit.Attributes
{
    //TODO Add HideInInspector
    //TODO Add EnableIf/DisableIf
    //TODO Add EnableInEditor/DisableInEditor
    //TODO Add EnableInPlayMode/DisableInPlaymode
    
    [Conditional("UNITY_EDITOR")]
    public abstract class GroupBaseAttribute : Attribute
    {
        //private readonly string m_Label;  
        public readonly string Path;
        public readonly string Name;
        public readonly string ParentPath;
        public readonly bool HasSubGroups;
        
        public GroupBaseAttribute(string path)
        {
            //m_Label = label;
            Path = path;

            var pathSplit = Path.Split('/');
            Name = pathSplit[pathSplit.Length - 1];
            HasSubGroups = pathSplit.Length > 1;
            ParentPath = HasSubGroups ? string.Join('/', pathSplit.Take(pathSplit.Length - 1)) : null;
        }
    }
}
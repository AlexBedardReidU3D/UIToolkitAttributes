# UIToolkit Attributes
This is a Tool that allows the auto generation of UXML & custom inspector scripts by using attributes

- Unity Version 2021.3.12f1
## Importing Package
* Use the `Package Manager -> Add package from git URL...`
* Use link `https://github.com/AlexBedardReidU3D/UIToolkitAttributes.git`

## Getting Started
1. Add `[GenerateUXML]` to any Monobehaviour or Value type/ Reference Type
2. Allow the scripts to recompile, and 2 new auto-generated scripts will have been added to `Assets/Editor/Custom Inspector/TYPE_NAME/`

## Todo List
- [ ] Stacked Groups utilizing Dynamic Labels
- [ ] Enum Flags Support
- [ ] Scriptable Objects Support
- [ ] Custom Label for structs utilizing `[BoxGroup]`
- [ ] Implement `[HideLabel]` to hide label
- [ ] Implement `[LeftToggle]`
- [ ] Implement `[Required]` to show warning when missing reference
- [ ] Implement Enum Toggle Buttons
- [ ] Implement `[Tooltip]`
- [ ] Implement `[HideIf]` / `[ShowIf]`

## How to use Attribute

### `[GenerateUXML]`
Add this attribute to any serializable `Class` or `Struct`. Adding this to a Monobehaviour would
generate a `CustomInspector`, otherwise a `PropertyDrawer` will be created.
```cs
//Generate UXML Custom Inspector for Components
[GenerateUXML]
public class MyClass : MonoBehaviour
{
    //Generate UXML Property Drawers for non-Components
    [Serializable, GenerateUXML]
    public struct MyStruct
    {
    
    }
}
```
---
### `[Button]` | `[Button(string)]`
This will generate a button element, that will `Invoke` the function it sits above. 
This will also work with the conditional attributes.
```cs
[Button]
private void MyMethod()
{
  /*My Method Code*/
}
```
```cs
[Button("My Custom Button Label")]
private void MyMethod()
{
  /*My Method Code*/
}
```
* Parameter: text `string`
  * **If this parameter is not included**, then the name of the method will be used , then the name of the method will be used
  * This optional parameter will set the text of the button to the string passed.
  * If a string starts with the `$` character (`$m_myField`) then it will attempt to get the string of the field name provided
 ---
### `[CustomLabel(string)]`
## Attribute Information
| Attribute             | AttributeTargets | Allow Multiple |
|-----------------------|------------------|----------------|
| `[GenerateUXML]`      | `Class, Struct`  | `false`        |
| `[Button()]`          | `Method`         | `false`        |
| `[CustomLabel()]`     | `Field`          | `false`        |
| `[DisplayAsString()]` | `Field`          | `false`        |
| `[InfoBox()]`         | `Field, Method`  | `false`        |
| `[ReadOnly]`          | `Field`          | `false`        |
### Groups
| Attribute                   | AttributeTargets | Allow Multiple |
|-----------------------------|------------------|----------------|
| `[BoxGroup()]`              | `Field, Method`  | `true`         |
| `[FoldoutGroup()]`          | `Field, Method`  | `true`         |
| `[HorizontalLayoutGroup()]` | `Field, Method`  | `true`         |
| `[TitleGroup()]`            | `Field, Method`  | `true`         |
| `[VerticalLayoutGroup()]`   | `Field, Method`  | `true`         |

### Conditionals
| Attribute                 | AttributeTargets | Allow Multiple |
|---------------------------|------------------|----------------|
| `[DisableIf()]`           | `Field, Method`  | `false`        |
| `[EnableIf()]`            | `Field, Method`  | `false`        |
| `[DisableInEditorMode()]` | `Field, Method`  | `false`        |
| `[DisableInPlayMode()]`   | `Field, Method`  | `false`        |

## Change Log
- 0.0.1 package created

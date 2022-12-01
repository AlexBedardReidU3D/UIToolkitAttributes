# UIToolkit Attributes
This is a Tool that allows the auto generation of UXML & custom inspector scripts by using attributes
> **_NOTE:_**  Other versions of Unity need to be tested to expand this list.
- Unity Version 2021.3.12f1

## Importing Package
* Use the `Package Manager -> ➕ -> Add package from git URL...`
* Use link `https://github.com/AlexBedardReidU3D/UIToolkitAttributes.git`

## Getting Started
1. Add `[GenerateUXML]` to any `Monobehaviour` or Value type/ Reference Type
2. Allow the scripts to recompile, and 2 new auto-generated scripts will have been added to `Assets/Editor/Custom Inspector/TYPE_NAME/`

## Todo List
- [ ] ⚠ Implement auto-cleanup of deleted Types
- [ ] Stacked Groups utilizing Dynamic Labels
- [ ] Enum Flags Support
- [ ] Scriptable Objects Support
- [ ] Custom Label for `struct` utilizing `[BoxGroup]`
- [ ] Implement `[HideLabel]` to hide label
- [ ] Implement `[LeftToggle]`
- [ ] Implement `[Required]` to show warning when missing reference
- [ ] Implement `[EnumToggleButtons]`
- [ ] Implement `[Tooltip]`
- [ ] Implement `[HideIf]` / `[ShowIf]`
---
**Table of Contents**
- [The Attributes](#the-attributes)
  * [Generic Attributes](#generic-attributes)
    + [`[GenerateUXML]`](#--generateuxml--)
    + [`[Button]`](#--button-------button-string---)
    + [`[CustomLabel(string)]`](#--customlabel-string---)
    + [`[DisplayAsString]`](#--displayasstring--)
    + [`[InfoBox(string)]`](#--infobox-string---)
    + [`[ReadOnly]`](#--readonly--)
  * [Group Attributes](#group-attributes)
    + [Group Path Examples](#group-path-examples)
    + [`[BoxGroup(string)]`](#--boxgroup-string--------boxgroup-string--string---)
    + [`[FoldoutGroup(string)]`](#--foldoutgroup-string--------foldoutgroup-string--string---)
    + [`[HorizontalLayoutGroup(string)]`](#--horizontallayoutgroup-string---)
    + [`[TitleGroup(string)]`](#--titlegroup-string--------titlegroup-string--string---)
    + [`[VerticalLayoutGroup(string)]`](#--verticallayoutgroup-string---)
  * [Conditional Attributes](#conditional-attributes)
    + [`[DisableIf(string)]`](#--disableif-string--------disableif-string--object---)
    + [`[EnableIf(string)]`](#--enableif-string--------enableif-string--object---)
    + [`[DisableInEditorMode]`](#--disableineditormode--)
    + [`[DisableInPlayMode]`](#--disableinplaymode--)
  * [Attribute Information](#attribute-information)
    + [Groups](#groups)
    + [Conditionals](#conditionals)
- [Known Issues](#known-issues)
- [Change Log](#change-log)

<small><i><a href='http://ecotrust-canada.github.io/markdown-toc/'>Table of contents generated with markdown-toc</a></i></small>

---

# The Attributes
## Generic Attributes
**Links**
- [`[GenerateUXML]`](#--generateuxml--)
- [`[Button]`](#--button-------button-string---)
- [`[CustomLabel(string)]`](#--customlabel-string---)
- [`[DisplayAsString]`](#--displayasstring--)
- [`[InfoBox(string)]`](#--infobox-string---)
- [`[ReadOnly]`](#--readonly--)
### `[GenerateUXML]`
Add this attribute to any serializable `Class` or `Struct`. Adding this to a `Monobehaviour` would
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
Custom Labels allow for overrides on the field name used in the script. This name can be a constant string or
a dynamic member reference.
```csharp
[CustomLabel("My Custom Label")]
public int myValue;

[CustomLabel("$myDynamicLabel")]
public int myNewValue;

public string myDynamicLabel;
```
* Parameter: text `string`
  * Text to display as the fields label 
  * If a string starts with the `$` character (`$m_myField`) then it will attempt to get the string of the field name provided
---
### `[DisplayAsString]`
This displays the value & field name as an editable string (No Field Entry). This can be used
in conjunction with `[CustomLabel()]`.
```csharp
[DisplayAsString]
public int myValue;
```
---
### `[InfoBox(string)]`
This will display a text box with a warning/info icon and whatever text is passed to the attribute.
This will appear immediately over the field it is with. This will also appear within groups as
expected.
```csharp
[InfoBox("This is a warning")]
public int myValue;
```
* Parameter: infoText `string`
  * Text to display for the info box body
---
### `[ReadOnly]`
Unlike `[DisplayAsString]`, `[ReadOnly]` only prevents the field from being edited in the
inspector, leaving the field visible.
```csharp
[ReadOnly]
public int myValue;
```
---
## Group Attributes
- [`[BoxGroup(string)]`](#--boxgroup-string--------boxgroup-string--string---)
- [`[FoldoutGroup(string)]`](#--foldoutgroup-string--------foldoutgroup-string--string---)
- [`[HorizontalLayoutGroup(string)]`](#--horizontallayoutgroup-string---)
- [`[TitleGroup(string)]`](#--titlegroup-string--------titlegroup-string--string---)
- [`[VerticalLayoutGroup(string)]`](#--verticallayoutgroup-string---)
* Groups can contain multiple elements. 
* A group can exist as a sub group to another group. 
* Some groups use a label which can either be specified
  * The group name in the path will be used as the label if no label was specified.
* A group can be stacked on a single field, but each group must be declared before a sub group can be used.
* It does not have to be declared on the same value, but it must be declared above the field.

All groups have:
* Parameter: **path** `string`
  * The group hierarchy, with each group separated by `/`

Some Groups have:
* Optional Parameter: **label** `string`
  * Text to display for the groups label
  * If a string starts with the `$` character (`$m_myField`) then it will attempt to get the string of the field name provided

### Group Path Examples
```csharp
//This will throw an exception
//-----------------------------------//
[BoxGroup("MyGroup/MySubGroup/MySubSubGroup")]
public int myValue;
```
```csharp
//This will not throw an exception
//-----------------------------------//
[VerticalLayoutGroup("MyGroup")]                // <-- Declare MyGroup 
[HorizontalLayoutGroup("MyGroup/MySubGroup")]   // <-- Declare MySubGroup, as a child of MyGroup
[BoxGroup("MyGroup/MySubGroup/MySubSubGroup")]  // <-- Declare MySubSubGroup, as a child of MySubGroup
public int myValue;
```
```csharp
//This will not throw an exception
//-----------------------------------//
[VerticalLayoutGroup("MyGroup")]                // <-- Declare MyGroup 
public int myValue;

[HorizontalLayoutGroup("MyGroup/MySubGroup")]   // <-- Declare MySubGroup, as a child of MyGroup
public int myValue2;

[BoxGroup("MyGroup/MySubGroup/MySubSubGroup")]  // <-- Declare MySubSubGroup, as a child of MySubGroup
public int myValue3;
```
A declared group type must match all other references to it:
```csharp
//This will throw an exception
//-----------------------------------//
[BoxGroup("MyGroup")]
public int myValue;

[FoldoutGroup("MyGroup")] // <-- This does not match BoxGroup with the same name specified above
public int myOtherValue;
```
```csharp
//This will not throw an exception
//-----------------------------------//
[BoxGroup("MyGroup")]
public int myValue;

[BoxGroup("MyGroup")]
public int myOtherValue;
```

---
### `[BoxGroup(string)]` | `[BoxGroup(string, string)]`
A static box with a label to contain multiple elements.
```csharp
[BoxGroup("MyBoxGroup")]
public int myValue;

[BoxGroup("MyOtherBoxGroup", "My Custom Label")]
public int myOtherValue;
```
A struct can also be display as a box group instead of the normal foldout by specifying the use of `[BoxGroup]`. 
> **_NOTE:_**  The struct must utilize `[GenerateUXML]` for this to work.
```csharp
[Serializable, GenerateUXML]
public struct MyStruct
{
    public int myValue;
}

public MyStruct myDefaultDisplay;

[BoxGroup("myBoxDisplay")]
public MyStruct myBoxDisplay;
```

---
### `[FoldoutGroup(string)]` | `[FoldoutGroup(string, string)]`
Similar to `[BoxGroup]` this group uses a header to contain multiple elements, but with the addition of a toggle to fold
the group in or out.
```csharp
[FoldoutGroup("MyGroup")]
public int myValue;

[FoldoutGroup("MyOtherGroup", "My Custom Label")]
public int myOtherValue;

[FoldoutGroup("MyDynamicGroup", "$dynamicLabel")]
public int myValue2;
public string dynamicLabel;
```
---
### `[HorizontalLayoutGroup(string)]`
Horizontal groups do not utilize a label, so act as almost an invisible group/container. Horizontal groups use the flex layout
setting `row` to stack the elements horizontally.
```csharp
[HorizontalLayoutGroup("MyGroup")]
public int myValue;

[HorizontalLayoutGroup("MyGroup")]
public int myOtherValue;
```
---
### `[TitleGroup(string)]` | `[TitleGroup(string, string)]` 
Title Groups are similar to the BoxGroup because they use a header, though instead of a container a bolded title with an underline
exists instead. 
```csharp
[TitleGroup("MyGroup")]
public int myValue;

[TitleGroup("MyOtherGroup", "My Custom Label")]
public int myOtherValue;

[TitleGroup("MyDynamicGroup", "$dynamicLabel")]
public int myValue2;
public string dynamicLabel;
```
---
### `[VerticalLayoutGroup(string)]`
Similar to the `[HorizontalLayoutGroup]` this is an invisible container with no title. However, this uses the default `column`
setting for flex layout.
```csharp
[VerticalLayoutGroup("MyGroup")]
public int myValue;

[VerticalLayoutGroup("MyGroup")]
public int myOtherValue;
```

---
## Conditional Attributes
- [`[DisableIf(string)]`](#--disableif-string--------disableif-string--object---)
- [`[EnableIf(string)]`](#--enableif-string--------enableif-string--object---)
- [`[DisableInEditorMode]`](#--disableineditormode--)
- [`[DisableInPlayMode]`](#--disableinplaymode--)

Conditionals allow the locking of elements by enabling or disabling them depending on the conditions specified. When an element
is disabled, input will no longer be possible. Conditionals can also be applied to `[Button]` attributes.
---
### `[DisableIf(string)]` | `[DisableIf(string, object)]`
This will disable the member when either the `condition` member returns `true` OR when the `condition` member matches the
included optional `expectedValue`.

```csharp
//This uses the value returned by shouldToggle
//-----------------------------------//
public bool shouldToggle;

[DisableIf("shouldToggle")]
public int myValue;

//This will compare threshold to 10
//-----------------------------------//
public int threshold;

[DisableIf("threshold", 10)]
public int myOtherValue;
```
```csharp
public enum MyEnum
{
    One,
    Two,
    Three
}

public MyEnum myEnum;

//This will compare myEnum to MyEnum.Two
//-----------------------------------//
[Button, DisableIf("myEnum", MyEnum.Two)]
public void MyButton()
{
    //Method Body
}
```
* Parameter: **condition** `string`
  * The Member name to use when attempting to determine if this element should be enabled
  * The Member should return a bool value _(If not using the optional parameter)_
  * **If using the optional parameter**, the member can be anything that returns an `object`
* Optional Parameter: **expectedValue** `object`
  * This is a constant value in which to compare the specified member
---
### `[EnableIf(string)]` | `[EnableIf(string, object)]`
This will enable the member when either the `condition` member returns `true` OR when the `condition` member matches the
included optional `expectedValue`.

```csharp
//This uses the value returned by shouldToggle
//-----------------------------------//
public bool shouldToggle;

[EnableIf("shouldToggle")]
public int myValue;

//This will compare threshold to 10
//-----------------------------------//
public int threshold;

[EnableIf("threshold", 10)]
public int myOtherValue;
```
```csharp
public enum MyEnum
{
    One,
    Two,
    Three
}

public MyEnum myEnum;

//This will compare myEnum to MyEnum.Two
//-----------------------------------//
[Button, EnableIf("myEnum", MyEnum.Two)]
public void MyButton()
{
    //Method Body
}
```
* Parameter: **condition** `string`
  * The Member name to use when attempting to determine if this element should be enabled
  * The Member should return a bool value _(If not using the optional parameter)_
  * **If using the optional parameter**, the member can be anything that returns an `object`
* Optional Parameter: **expectedValue** `object`
  * This is a constant value in which to compare the specified member
---
### `[DisableInEditorMode]`
This will apply the disabled functionality when out of play mode, and will switch when pressing play
```csharp
[DisableInEditorMode]
public int myValue;
```
---
### `[DisableInPlayMode]`
This will apply the disabled functionality when in play mode, and will switch when entering back into editor
```csharp
[DisableInPlayMode]
public int myValue;
```
---
## Attribute Information
| Attribute           | AttributeTargets | Allow Multiple |
|---------------------|------------------|----------------|
| `[GenerateUXML]`    | `Class, Struct`  | `false`        |
| `[Button()]`        | `Method`         | `false`        |
| `[CustomLabel()]`   | `Field`          | `false`        |
| `[DisplayAsString]` | `Field`          | `false`        |
| `[InfoBox()]`       | `Field, Method`  | `false`        |
| `[ReadOnly]`        | `Field`          | `false`        |
### Groups
| Attribute                   | AttributeTargets | Allow Multiple |
|-----------------------------|------------------|----------------|
| `[BoxGroup()]`              | `Field, Method`  | `true`         |
| `[FoldoutGroup()]`          | `Field, Method`  | `true`         |
| `[HorizontalLayoutGroup()]` | `Field, Method`  | `true`         |
| `[TitleGroup()]`            | `Field, Method`  | `true`         |
| `[VerticalLayoutGroup()]`   | `Field, Method`  | `true`         |

### Conditionals
| Attribute               | AttributeTargets | Allow Multiple |
|-------------------------|------------------|----------------|
| `[DisableIf()]`         | `Field, Method`  | `false`        |
| `[EnableIf()]`          | `Field, Method`  | `false`        |
| `[DisableInEditorMode]` | `Field, Method`  | `false`        |
| `[DisableInPlayMode]`   | `Field, Method`  | `false`        |

# Known Issues
* When using `[HorizontalLayoutGroup]` children that use a field, the sizing is off as the width must be fixed, but causes the field to be a strange size
# Change Log
- 0.0.1 package created

using UnityEditor;
using UnityEngine;

public struct BlittableBool
{
    private readonly byte _value;
    public BlittableBool(bool value) { _value = (byte)(value ? 1 : 0); }
    public static implicit operator BlittableBool(bool value) { return new BlittableBool(value); }
    public static implicit operator bool(BlittableBool value) { return value._value != 0; }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(BlittableBool))]
class TBoolDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var field = property.FindPropertyRelative("_value");
        field.intValue = EditorGUI.Toggle(position, label, field.intValue != 0) ? 1 : 0;
    }
}
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

/// <summary>
/// Custom inspector for Object including derived classes.
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(UnityEngine.Object), true)]
public class ObjectEditor : Editor
{
    private Dictionary<Button, MethodInfo> buttons = new Dictionary<Button, MethodInfo>();

    public override VisualElement CreateInspectorGUI()
    {
        buttons.Clear();

        var container = new VisualElement();
        var type = target.GetType();

        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.GetParameters().Length == 0);
        foreach (var method in methods)
        {
            var ba = (ContextMenu)Attribute.GetCustomAttribute(method, typeof(ContextMenu));

            if (ba != null)
            {
                Button button = new Button();
                button.name = ba.menuItem;
                button.text = ba.menuItem;
                button.clicked += () => { ButtonClicked(button); };
                button.style.height = 30;
                container.Add(button);

                buttons[button] = method;
            }
        }

        InspectorElement.FillDefaultInspector(container, this.serializedObject, this);

        return container;
    }

    private void ButtonClicked(Button button)
    {
        MethodInfo method = buttons[button];
        if (method != null)
        {
            foreach (var t in targets)
            {
                method.Invoke(t, null);
            }
        }
    }
}

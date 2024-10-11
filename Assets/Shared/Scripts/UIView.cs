using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIView : MonoBehaviour
{
    protected ViewController viewController;
    protected UIDocument document;
    protected VisualElement root;
    public VisualElement Root => root;

    public virtual void Init(ViewController viewController)
    {
        this.viewController = viewController;
        document = this.GetComponent<UIDocument>();
        root = document.rootVisualElement;
    }

    public virtual void Show()
    {
        root.style.display = DisplayStyle.Flex;
    }

    public virtual void Hide()
    {      
        root.style.display = DisplayStyle.None;
        //this.root.style.display = DisplayStyle.None;
    }

    public virtual void Unload()
    {

    }
}

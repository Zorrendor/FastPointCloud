using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewController : MonoBehaviour
{
    public UIView firstViewToDisplay;
    
    public bool NeedsReset { get; set; }
    
    public List<UIView> AllViews { get; private set; }

    private Dictionary<Type, UIView> views = new Dictionary<Type, UIView>();

    private Stack<UIView> viewStack = new Stack<UIView>();
    
    public event System.Action<UIView> OnInit;
    public event System.Action<UIView> OnShow;
    public event System.Action<UIView> OnHide;

    private void Start()
    {
        UIView []viewsArray = this.GetComponentsInChildren<UIView>(true);
        foreach (UIView view in viewsArray)
        {
            views[view.GetType()] = view;
            view.gameObject.SetActive(true);
            view.Init(this);
            
            OnInit?.Invoke(view);

            view.Hide();
        }

        viewStack.Push(firstViewToDisplay);
        firstViewToDisplay.Show();
        OnShow?.Invoke(firstViewToDisplay);

        AllViews = new List<UIView>();
        AllViews.AddRange(viewsArray);
    }

    public T ShowView<T>() where T : UIView
    {
        UIView view;
        if (views.TryGetValue(typeof(T), out view))
        {
            while (viewStack.Count != 0)
            {
                viewStack.Pop().Hide();
            }
            viewStack.Clear();
            view.Show();
            OnShow?.Invoke(view);
            viewStack.Push(view);
        }
        Debug.Assert(view != null, "There is no view typeof " + typeof(T).Name);
        return view as T;
    }

    public T PushView<T>() where T : UIView
    {
        UIView view;
        if (views.TryGetValue(typeof(T), out view))
        {
            //UIView prevView;
            if (viewStack.TryPeek(out UIView prevView))
            {
                prevView.Hide();
                OnHide?.Invoke(prevView);
            }

            view.Show();
            viewStack.Push(view);
            OnShow?.Invoke(view);
        }
        Debug.Assert(view != null, "There is no view typeof " + typeof(T).Name);
        return view as T;
    }

    public void ReplaceView<T>() where T : UIView
    {
        //if 
    }

    public void Hide(UIView view)
    {
        if (viewStack.Count > 0)
        {
            viewStack.Pop();
        }        
        view.Hide();
        OnHide?.Invoke(view);

        if (viewStack.TryPeek(out UIView currentView))
        {
            currentView.Show();
            OnShow?.Invoke(currentView);
        }
    }

#if UNITY_EDITOR
    
    private void LateUpdate()
    {
        if (NeedsReset)
        {
            this.Reset();
            NeedsReset = false;
        }
    }

    [ContextMenu("Reset")]
    public void Reset()
    {
        UIView []viewsArray = this.GetComponentsInChildren<UIView>(true);
        foreach (UIView view in viewsArray)
        {
            views[view.GetType()] = view;
            view.gameObject.SetActive(true);
            view.Init(this);

            view.Hide();
        }

        if (viewStack.Count > 0)
        {
            viewStack.Peek().Show();
        }
    }
    
#endif
}

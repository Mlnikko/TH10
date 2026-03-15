using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonGroupController : MonoBehaviour
{
    [Header("按钮导航")]
    [SerializeField] protected bool isEnabled = true;
    [SerializeField] protected bool loopSelection = true;
    [SerializeField] protected bool enableMouseHover = true;
    [SerializeField] int firstSelectBtnIndex = 0;
    protected List<CustomButton> buttons = new();
    protected int groupSize => buttons.Count;
    protected int currentIndex = -1;

    void Awake()
    {
        buttons.Clear();
        GetComponentsInChildren(buttons);
        foreach (var button in buttons)
        {
            button.InitButton(this);
        }
    }

    protected virtual void OnEnable()
    {
        AddInputListener();
        SelectButton(firstSelectBtnIndex);   
    }
    void AddInputListener()
    {
        //InputManager.Instance.OnKeyInput_Z += HandleBtnGroupComfirm;

        //InputManager.Instance.OnKeyInput_Esc += HandleGroupCancel;
        //InputManager.Instance.OnKeyInput_X += HandleGroupCancel;
    }

    public void EnableNavigation(bool enable)
    {
        isEnabled = enable;
    }

    void HandleUpNavigation(bool upInput)
    {
        if (!isEnabled || buttons.Count == 0) return;
        if (upInput)
        {
            Navigate(-1);
        }
    }

    void HandleDownNavigation(bool downInput) 
    {
        if (!isEnabled || buttons.Count == 0) return;
        if (downInput)
        {
            Navigate(1);
        }
    }

    void Navigate(int direction)
    {
        if (buttons.Count == 0) return;

        int newIndex = currentIndex + direction;
        if (loopSelection)
        {
            if (newIndex < 0) newIndex = buttons.Count - 1;
            if (newIndex >= buttons.Count) newIndex = 0;
        }
        else
        {
            newIndex = Mathf.Clamp(newIndex, 0, buttons.Count - 1);
        }
        SelectButton(newIndex);
    }

    IEnumerator AutoNavigation(int direction, bool isAuto)
    {
        while (isAuto)
        {
            yield return new WaitForSeconds(0.1f);
            Navigate(direction);
        }
    }

    void HandleBtnGroupComfirm(bool value)
    {
        if (!isEnabled || buttons.Count == 0) return;
        OnBtnGroupConfim(value);
    }

    protected virtual void OnBtnGroupConfim(bool value)
    {
        // 至少需要点击当前选择的按钮
        if (currentIndex >= 0 && value == true)
        {
            buttons[currentIndex].ClickButton();
        }
    }

    void HandleGroupCancel()
    {
        if (!isEnabled || buttons.Count == 0) return;
        OnGroupCancel();
    }

    protected virtual void OnGroupCancel()
    {
        // 处理按钮组取消操作
    }

    public void SelectButton(CustomButton button)
    {
        // 仅当启用鼠标悬停或来自键盘操作时才执行
        int index = buttons.IndexOf(button);
        if (index >= 0)
        {
            SelectButton(index);
        }
    }

    public void SelectButton(int index)
    {
        if (index < 0 || index >= buttons.Count) return;

        if(currentIndex != -1) // 首次选择按钮时不需要取消选择当前按钮
        {
            // 取消当前按钮选择
            buttons[currentIndex].UnSelectButton();
        }
        
        // 选择指定按钮
        currentIndex = index;
        buttons[currentIndex].SelectButton();

        // 处理滚动容器
        ScrollToSelected();
    }

    public void ClickCurSelectButton()
    {
        buttons[currentIndex].ClickButton();
    }

    void ScrollToSelected()
    {
        // 如果按钮在ScrollRect中，自动滚动到可见位置
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect == null) return;

        RectTransform selectedRT = buttons[currentIndex].GetComponent<RectTransform>();
        Canvas.ForceUpdateCanvases();
        scrollRect.content.anchoredPosition =
            (Vector2)scrollRect.transform.InverseTransformPoint(scrollRect.content.position)
            - (Vector2)scrollRect.transform.InverseTransformPoint(selectedRT.position);
    }

    void OnDisable()
    {    
        RemoveInputListener();     
    }

    void RemoveInputListener()
    {
        if (InputManager.Instance != null)
        {
            //InputManager.Instance.OnKeyInput_Z -= HandleBtnGroupComfirm;
            //InputManager.Instance.OnKeyInput_Esc -= HandleGroupCancel;
            //InputManager.Instance.OnKeyInput_X -= HandleGroupCancel;
        }
    }
}

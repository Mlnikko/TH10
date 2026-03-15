using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable]
public class CustomButtonEvent
{
    public E_Event eventName;
    public E_EventValue valueType;
    public string value;
    public UnityEvent unityEvent;
}

public class CustomButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    [Header("按钮事件")]
    public CustomButtonEvent[] buttonEvents;

    [Header("缩放响应")]
    [SerializeField] bool isScaleEnable = true;
    [SerializeField] float hoverScale = 1.05f;
    [SerializeField] float selectScale = 1.05f;
    [SerializeField] float scaleAnimDuration = 0.2f;

    [Header("按钮音效")]
    [SerializeField] AudioName hoverSound;
    [SerializeField] AudioName unHoverSound;
    [SerializeField] AudioName selectSound;
    [SerializeField] AudioName unSelectSound;
    [SerializeField] AudioName confirmSound;

    public Button button
    {
        get
        {
            if(btn == null)
            {
                btn = GetComponent<Button>();
            }
            return btn;
        }
    }
    Button btn;

    protected ButtonGroupController btnGroup;

    public RectTransform BtnRectTransform
    {
        get
        {
            if (rectTransform == null) 
            {
                rectTransform = GetComponent<RectTransform>();
            }
            return rectTransform;
        }
    }
    RectTransform rectTransform;

    protected Vector3 baseScale;
    protected bool isHover;
    protected bool isSelected;

    protected virtual void Awake()
    { 
        button.onClick.AddListener(OnButtonClicked);
    }

    public void InitButton(ButtonGroupController btnGroup)
    {
        this.btnGroup = btnGroup;
        OnButtonInit();
    }

    protected virtual void OnButtonInit()
    {
        baseScale = BtnRectTransform.localScale;
        isSelected = false;
        isHover = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoverButton();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        SelectButton();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        UnHoverButton();
    }
    public void HoverButton()
    {
        OnButtonHover();
        isHover = true;
    }
    public void UnHoverButton()
    {
        OnButtonUnHover();
        isHover = false;
    }
    public void SelectButton()
    {
        OnButtonSelected();
        isSelected = true;
    }
    public void UnSelectButton()
    {
        OnButtonUnSelected();
        isSelected = false;
    }
    public void ClickButton()
    {
        OnButtonClicked();
    }

    protected virtual void OnButtonSelected()
    {
        AudioManager.Instance.PlayAudio(selectSound);
        if (isScaleEnable)
        {
            BtnRectTransform.Scale(selectScale, scaleAnimDuration);
        }
    }
    protected virtual void OnButtonUnSelected()
    {
        AudioManager.Instance.PlayAudio(unSelectSound);
        if (isScaleEnable)
        {
            BtnRectTransform.Scale(baseScale, scaleAnimDuration);
        }
    }
    protected virtual void OnButtonClicked()
    {
        foreach (var btnEvent in buttonEvents)
        {
            switch (btnEvent.valueType)
            {
                case E_EventValue.NULL:
                    EventManager.Instance.TriggerEvent(btnEvent.eventName);
                    break;
                case E_EventValue.Int:
                    EventManager.Instance.TriggerEvent(btnEvent.eventName, int.Parse(btnEvent.value));
                    break;
                case E_EventValue.Float:
                    EventManager.Instance.TriggerEvent(btnEvent.eventName, float.Parse(btnEvent.value));
                    break;
                case E_EventValue.String:
                    EventManager.Instance.TriggerEvent(btnEvent.eventName, btnEvent.value);
                    break;
            }
            btnEvent.unityEvent?.Invoke();
            AudioManager.Instance.PlayAudio(confirmSound);
        }
    }
    protected virtual void OnButtonHover()
    {
        AudioManager.Instance.PlayAudio(hoverSound);
        if (isScaleEnable)
        {
            BtnRectTransform.Scale(hoverScale, scaleAnimDuration);
        }

    }
    protected virtual void OnButtonUnHover()
    {
        AudioManager.Instance.PlayAudio(unHoverSound);
        if (isScaleEnable)
        {
            BtnRectTransform.Scale(baseScale, scaleAnimDuration);
        }
    }
}

using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class DoTweenExTools_UI
{
    #region 渐变动画
    // 渐变透明度（支持Graphic和CanvasGroup）
    public static Tween Fade(this Component target, float endValue, float duration,Ease ease = Ease.OutSine)
    {
        target.KillTweens();

        if (target is Graphic graphic)
        {
            return graphic.DOFade(endValue, duration).SetEase(ease);
        }
        else if (target is CanvasGroup group)
        {
            return group.DOFade(endValue, duration).SetEase(ease);
        }

        Debug.LogError($"Fade animation not supported on {target.GetType().Name}");
        return null;
    }

    public static Tween Fade(this Component target, float startValue, float endValue, float duration, int loops = 0, LoopType loopType = LoopType.Yoyo, Ease ease = Ease.OutSine)
    {
        target.KillTweens();

        if (target is Graphic graphic)
        {
            var color = graphic.color;
            color.a = startValue;
            graphic.color = color;

            return graphic.DOFade(endValue, duration).SetLoops(loops, loopType).SetEase(ease);
        }
        else if (target is CanvasGroup group)
        {
            group.alpha = startValue;
            return group.DOFade(endValue, duration).SetLoops(loops, loopType).SetEase(ease);
        }

        Debug.LogError($"Fade animation not supported on {target.GetType().Name}");
        return null;
    }
    #endregion

    #region 缩放动画
    // 缩放动画
    public static Tween Scale(this Component target, Vector3 endScale, float duration, Ease ease = Ease.OutBack)
    {
        target.KillTweens();
        return target.transform.DOScale(endScale, duration).SetEase(ease);
    }

    public static Tween Scale(this Component target, float endScale, float duration, Ease ease = Ease.OutBack)
    {
        target.KillTweens();
        return target.transform.DOScale(endScale, duration).SetEase(ease);
    }

    #endregion

    #region 颜色动画
    // 颜色变化（仅限Graphic）
    public static Tween Color(this Graphic target, Color endColor, float duration, Ease ease = Ease.InOutSine)
    {
        target.KillTweens();
        return target.DOColor(endColor, duration).SetEase(ease);
    }

    public static Tween Color(this Graphic target, Color startColor, Color endColor, float duration, int loops = 0, LoopType loopType = LoopType.Yoyo, Ease ease = Ease.InOutSine)
    {
        target.KillTweens();
        target.color = startColor;
        return target.DOColor(endColor, duration).SetLoops(loops, loopType).SetEase(ease);
    }
    #endregion

    #region 特殊效果
    public static Tween Shake(this RectTransform target, float duration)
    {
        return target.DOShakeAnchorPos(duration);
    }
    #endregion

    // 平移动画
    public static Tween Move(this RectTransform target, Vector2 endPos, float duration, bool useLocal = false, Ease ease = Ease.OutSine)
    {
        target.KillTweens();
        if (useLocal)
        {
            return target.DOLocalMove(endPos, duration).SetEase(ease);
        }
        else
        {
            return target.DOAnchorPos(endPos, duration).SetEase(ease);
        }     
    }

    public static Tween Move(this RectTransform target, Vector2 startPos ,Vector2 endPos, float duration, bool useLocal = false, Ease ease = Ease.OutSine)
    {
        target.KillTweens();
        if (useLocal)
        {
            target.localPosition = startPos;
            return target.DOLocalMove(endPos, duration).SetEase(ease);
        }
        else
        {
            target.anchoredPosition = startPos;
            return target.DOAnchorPos(endPos, duration).SetEase(ease);
        }
    }

    // 动画队列组合
    public static Sequence Sequence(this Component target)
    {
        target.KillTweens();
        return DOTween.Sequence().SetTarget(target);
    }

    // 清理对象相关动画
    public static void KillTweens(this Component target)
    {
        DOTween.Kill(target);
    }

    // 暂停所有动画
    public static void PauseTweens(this Component target)
    {
        DOTween.Pause(target);
    }

    // 恢复动画
    public static void ResumeTweens(this Component target)
    {
        DOTween.Play(target);
    }
}


public class TitleBtnGroup : ButtonGroupController
{
    protected override void OnGroupCancel()
    {
        base.OnGroupCancel();
        // 选择最后一个按钮
        if(currentIndex != groupSize - 1)
        {
            SelectButton(groupSize - 1);
        }
        else
        {
            ClickCurSelectButton();
        }
    }
}

using QFramework;

public class DecreaseCountCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var counterModel = this.GetModel<CounterModel>();
        counterModel.Decrease();
        this.SendEvent<CountChangedEvent>();
    }
}
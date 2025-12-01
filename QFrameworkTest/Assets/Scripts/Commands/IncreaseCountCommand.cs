using QFramework;

// 这个命令负责处理"增加计数"的操作
public class IncreaseCountCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // 1. 获取计数器模型
        var counterModel = this.GetModel<CounterModel>();

        // 2. 调用模型的增加方法
        counterModel.Increase();

        // 3. 发送事件通知UI更新
        this.SendEvent<CountChangedEvent>();
    }
}

// 定义一个事件，当计数改变时发送
public struct CountChangedEvent { }
using QFramework;

// 这个类专门负责存储计数器的数据
public class CounterModel : AbstractModel
{
    // 当前计数值
    public int Count { get; private set; }

    // 增加计数
    public void Increase()
    {
        Count++;
    }

    // 减少计数
    public void Decrease()
    {
        Count--;
    }

    // 重置计数
    public void Reset()
    {
        Count = 0;
    }

    protected override void OnInit()
    {
        // 模型初始化时的代码可以写在这里
        Count = 0;
    }
}
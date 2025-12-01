using UnityEngine;
using UnityEngine.UI;
using QFramework;

// 这个类负责管理计数器的UI界面
public class CounterPanel : MonoBehaviour, IController
{
    [SerializeField] private Text countText;      // 显示计数的文本
    [SerializeField] private Button increaseBtn;  // 增加按钮
    [SerializeField] private Button decreaseBtn;  // 减少按钮

    private CounterModel mCounterModel;

    private void Start()
    {
        // 获取计数器模型
        mCounterModel = this.GetModel<CounterModel>();

        // 设置按钮点击事件
        increaseBtn.onClick.AddListener(OnIncreaseClick);
        decreaseBtn.onClick.AddListener(OnDecreaseClick);

        // 注册计数改变事件
        this.RegisterEvent<CountChangedEvent>(e => UpdateView())
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 初始化界面显示
        UpdateView();
    }

    private void OnIncreaseClick()
    {
        // 发送增加命令
        this.SendCommand<IncreaseCountCommand>();
    }

    private void OnDecreaseClick()
    {
        // 发送减少命令
        this.SendCommand<DecreaseCountCommand>();
    }

    // 更新界面显示
    private void UpdateView()
    {
        countText.text = mCounterModel.Count.ToString();
    }

    // 必须实现的方法，返回架构实例
    public IArchitecture GetArchitecture()
    {
        return GameApp.Interface;
    }
}
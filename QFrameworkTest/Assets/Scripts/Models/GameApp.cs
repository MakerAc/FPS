using UnityEngine;
using QFramework;

// 这是游戏的"大脑"，管理整个游戏的架构
public class GameApp : Architecture<GameApp>
{
    protected override void Init()
    {
        // 注册模型（数据）
        this.RegisterModel(new CounterModel());
    }

    // 游戏启动时调用这个方法来初始化
    [RuntimeInitializeOnLoadMethod]
    public static void StartGame()
    {
        // 确保架构被创建
        var app = Interface;
    }
}
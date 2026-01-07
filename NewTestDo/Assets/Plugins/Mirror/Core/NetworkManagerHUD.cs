using UnityEngine;

namespace Mirror
{
    /// <summary>Shows NetworkManager controls in a GUI at runtime.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/Network Manager HUD")]
    [RequireComponent(typeof(NetworkManager))]
    [HelpURL("https://mirror-networking.gitbook.io/docs/components/network-manager-hud")]
    public class NetworkManagerHUD : MonoBehaviour
    {
        NetworkManager manager;

        public int offsetX;
        public int offsetY;

        // 添加缩放控制
        public float guiScale = 3.0f;
        private bool isSkinCreated = false;

        void Awake()
        {
            manager = GetComponent<NetworkManager>();
        }

        void OnGUI()
        {
            // 确保只创建一次皮肤
            if (!isSkinCreated)
            {
                CreateScaledGUISkin();
                isSkinCreated = true;
            }

            // 保存原始GUI矩阵
            Matrix4x4 originalMatrix = GUI.matrix;

            // 计算缩放矩阵
            Vector3 scale = new Vector3(guiScale, guiScale, 1.0f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX * guiScale, offsetY * guiScale, 0),
                                      Quaternion.identity, scale);

            // 如果这个宽度被修改，也修改 GUIConsole::OnGUI 中的 offsetX
            int width = 300;

            GUILayout.BeginArea(new Rect(10, 40, width, 9999));

            // 应用自定义皮肤
            GUISkin originalSkin = GUI.skin;

            if (!NetworkClient.isConnected && !NetworkServer.active)
                StartButtons();
            else
                StatusLabels();

            if (NetworkClient.isConnected && !NetworkClient.ready)
            {
                if (GUILayout.Button("Client Ready"))
                {
                    // 客户端准备就绪
                    NetworkClient.Ready();
                    if (NetworkClient.localPlayer == null)
                        NetworkClient.AddPlayer();
                }
            }

            StopButtons();

            GUILayout.EndArea();

            // 恢复原始GUI皮肤
            GUI.skin = originalSkin;

            // 恢复原始矩阵
            GUI.matrix = originalMatrix;
        }

        void CreateScaledGUISkin()
        {
            // 创建自定义GUI皮肤
            GUISkin skin = ScriptableObject.CreateInstance<GUISkin>();

            // 复制默认皮肤的样式
            skin.label = new GUIStyle(GUI.skin.label);
            skin.label.fontSize = (int)(GUI.skin.label.fontSize * guiScale);

            skin.button = new GUIStyle(GUI.skin.button);
            skin.button.fontSize = (int)(GUI.skin.button.fontSize * guiScale);
            skin.button.fixedHeight = GUI.skin.button.fixedHeight * guiScale;

            skin.textField = new GUIStyle(GUI.skin.textField);
            skin.textField.fontSize = (int)(GUI.skin.textField.fontSize * guiScale);
            skin.textField.fixedHeight = GUI.skin.textField.fixedHeight * guiScale;

            skin.box = new GUIStyle(GUI.skin.box);
            skin.box.fontSize = (int)(GUI.skin.box.fontSize * guiScale);
            skin.box.fixedHeight = GUI.skin.box.fixedHeight * guiScale;

            // 设置为当前GUI皮肤
            GUI.skin = skin;
        }

        void StartButtons()
        {
            if (!NetworkClient.active)
            {
#if UNITY_WEBGL
                // WebGL构建中不能作为服务器
                if (GUILayout.Button("Single Player"))
                {
                    NetworkServer.dontListen = true;
                    manager.StartHost();
                }
#else
                // 服务器 + 客户端
                if (GUILayout.Button("Host (Server + Client)"))
                    manager.StartHost();
#endif

                // 客户端 + IP (+ 端口)
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Client"))
                    manager.StartClient();

                manager.networkAddress = GUILayout.TextField(manager.networkAddress);

                // 只有在有端口传输时才显示端口字段
                if (Transport.active is PortTransport portTransport)
                {
                    if (ushort.TryParse(GUILayout.TextField(portTransport.Port.ToString()), out ushort port))
                        portTransport.Port = port;
                }

                GUILayout.EndHorizontal();

                // 仅服务器
#if UNITY_WEBGL
                // WebGL构建中不能作为服务器
                GUILayout.Box("( WebGL cannot be server )");
#else
                if (GUILayout.Button("Server Only"))
                    manager.StartServer();
#endif
            }
            else
            {
                // 连接中
                GUILayout.Label($"Connecting to {manager.networkAddress}..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                    manager.StopClient();
            }
        }

        void StatusLabels()
        {
            // 主机模式
            // 单独显示，因为这总是让人困惑：
            //   服务器: ...
            //   客户端: ...
            if (NetworkServer.active && NetworkClient.active)
            {
                // 主机模式
                GUILayout.Label($"<b>Host</b>: running via {Transport.active}");
            }
            else if (NetworkServer.active)
            {
                // 仅服务器
                GUILayout.Label($"<b>Server</b>: running via {Transport.active}");
            }
            else if (NetworkClient.isConnected)
            {
                // 仅客户端
                GUILayout.Label($"<b>Client</b>: connected to {manager.networkAddress} via {Transport.active}");
            }
        }

        void StopButtons()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                GUILayout.BeginHorizontal();
#if UNITY_WEBGL
                if (GUILayout.Button("Stop Single Player"))
                    manager.StopHost();
#else
                // 如果是主机模式，停止主机
                if (GUILayout.Button("Stop Host"))
                    manager.StopHost();

                // 如果是主机模式，停止客户端，保持服务器运行
                if (GUILayout.Button("Stop Client"))
                    manager.StopClient();
#endif
                GUILayout.EndHorizontal();
            }
            else if (NetworkClient.isConnected)
            {
                // 如果仅是客户端，停止客户端
                if (GUILayout.Button("Stop Client"))
                    manager.StopClient();
            }
            else if (NetworkServer.active)
            {
                // 如果仅是服务器，停止服务器
                if (GUILayout.Button("Stop Server"))
                    manager.StopServer();
            }
        }
    }
}
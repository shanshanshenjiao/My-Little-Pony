using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;

namespace Multiplayer
{
    public class NetworkGameManager : MonoBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        // ⭐ 是否初始化完成（关键）
        public bool IsInitialized { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await InitializeUnityServices();

            // ⭐ 监听断线（关键）
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
            }
        }

        // ================= 初始化 =================
        private async Task InitializeUnityServices()
        {
            try
            {
                Debug.Log("初始化Unity服务...");

                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                IsInitialized = true;

                Debug.Log("Unity服务初始化完成！");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Unity服务初始化失败: " + e.Message);
            }
        }

        // ================= HOST =================
        public async Task<string> StartHostWithRelay(int maxPlayers = 4)
        {
            try
            {
                // ⭐ 等初始化完成（关键！！！）
                while (!IsInitialized)
                {
                    Debug.Log("等待服务初始化...");
                    await Task.Delay(500);
                }

                Debug.Log("开始创建Relay...");

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                transport.SetRelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    new byte[0],
                    true
                );

                NetworkManager.Singleton.StartHost();

                Debug.Log("Host启动成功，JoinCode: " + joinCode);

                return joinCode;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Host启动失败: " + e.Message);
                return null;
            }
        }

        // ================= CLIENT =================
        public async Task StartClientWithRelay(string joinCode)
        {
            try
            {
                // ⭐ 等初始化完成（关键！！！）
                while (!IsInitialized)
                {
                    Debug.Log("等待服务初始化...");
                    await Task.Delay(500);
                }

                Debug.Log("开始连接Relay...");

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                transport.SetRelayServerData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData,
                    false
                );

                NetworkManager.Singleton.StartClient();

                Debug.Log("Client连接成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Client连接失败: " + e.Message);
            }
        }

        // ================= ⭐ 断线处理 =================
        private void OnTransportFailure()
        {
            Debug.LogError("Relay连接断开！");

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // 👉 这里你可以回主界面
            if (UIManager.Instance != null)
            {
                UIManager.Instance.roomPanel.SetActive(false);
                UIManager.Instance.mainPanel.SetActive(true);
            }
        }
    }
}
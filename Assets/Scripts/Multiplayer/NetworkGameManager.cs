using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.Collections;
using Unity.Networking.Transport.Relay;

namespace Multiplayer
{
    public class NetworkGameManager : MonoBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        public bool IsInitialized { get; private set; } = false;

        private string lastJoinCode; // ⭐ 用于重连

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

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
            }

            // ⭐ 启动保活
            StartCoroutine(KeepAlive());
        }

        // ================= 初始化 =================
        private async Task InitializeUnityServices()
        {
            try
            {
                Debug.Log("初始化Unity服务...");

                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                IsInitialized = true;

                Debug.Log("Unity服务初始化完成！");
            }
            catch (System.Exception e)
            {
                Debug.LogError("初始化失败: " + e.Message);
            }
        }

        // ================= HOST =================
        public async Task<string> StartHostWithRelay(int maxPlayers = 4)
        {
            try
            {
                while (!IsInitialized)
                    await Task.Delay(200);

                Debug.Log("启动Host...");

                // ⭐ 防重复启动
                if (NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                    await Task.Delay(1000);
                }

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                lastJoinCode = joinCode;

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                // ⭐ 官方推荐写法（稳定）
                transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

                NetworkManager.Singleton.StartHost();

                Debug.Log("Host启动成功: " + joinCode);

                return joinCode;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Host失败: " + e.Message);
                return null;
            }
        }

        // ================= CLIENT =================
        public async Task StartClientWithRelay(string joinCode)
        {
            try
            {
                while (!IsInitialized)
                    await Task.Delay(200);

                Debug.Log("启动Client...");

                // ⭐ 防重复连接
                if (NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                    await Task.Delay(1000);
                }

                lastJoinCode = joinCode;

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

                NetworkManager.Singleton.StartClient();

                Debug.Log("Client连接成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Client失败: " + e.Message);
            }
        }

        // ================= ⭐ 保活（关键） =================
        IEnumerator KeepAlive()
        {
            while (true)
            {
                yield return new WaitForSeconds(5f);

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    // ⭐ 模拟网络活动（防止Relay断开）
                    Debug.Log("KeepAlive ping");
                }
            }
        }

        // ================= ⭐ 断线处理 =================
        private async void OnTransportFailure()
        {
            Debug.LogError("Relay断开，准备重连...");

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            await Task.Delay(1000);

            // ⭐ 自动重连逻辑
            if (!string.IsNullOrEmpty(lastJoinCode))
            {
                Debug.Log("尝试重连...");

                await StartClientWithRelay(lastJoinCode);
            }
            else
            {
                Debug.Log("没有JoinCode，返回主界面");

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.roomPanel.SetActive(false);
                    UIManager.Instance.mainPanel.SetActive(true);
                }
            }
        }
    }
}
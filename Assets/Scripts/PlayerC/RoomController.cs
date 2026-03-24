using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.Collections.Generic;
using Multiplayer;

public class RoomController : MonoBehaviour
{
    private Lobby currentLobby;

    private bool isCountingDown = false;
    private float timer = 0f;

    private bool isCreatingRoom = false;   // 防止重复点击
    private bool isJoiningRoom = false;

    void Start()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager没有初始化！");
            return;
        }

        UIManager.Instance.createRoomButton.onClick.AddListener(OnClickCreateRoom);

        UIManager.Instance.joinRoomButton.onClick.AddListener(() =>
        {
            string id = UIManager.Instance.roomIdInput.text;
            OnClickJoinRoom(id);
        });

        UIManager.Instance.readyButton.onClick.AddListener(SetReady);
    }

    // ================= 创建房间 =================
    public async void OnClickCreateRoom()
    {
        if (isCreatingRoom) return; // ?? 防连点
        isCreatingRoom = true;

        try
        {
            Debug.Log("点击创建房间");

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("MyRoom", 3);

            Debug.Log("房间创建成功：" + currentLobby.Id);

            UIManager.Instance.mainPanel.SetActive(false);
            UIManager.Instance.roomPanel.SetActive(true);
            UIManager.Instance.roomIdText.text = "房间号：" + currentLobby.Id;

            // ?? 启动Relay
            string relayCode = await NetworkGameManager.Instance.StartHostWithRelay();

            if (string.IsNullOrEmpty(relayCode))
            {
                Debug.LogError("Relay创建失败");
                return;
            }

            // ?? 写入Lobby
            await LobbyService.Instance.UpdateLobbyAsync(
                currentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {
                            "relayCode",
                            new DataObject(DataObject.VisibilityOptions.Public, relayCode)
                        }
                    }
                }
            );

            Debug.Log("RelayCode写入Lobby成功：" + relayCode);

            UpdatePlayerList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("创建房间失败：" + e);
        }
        finally
        {
            isCreatingRoom = false;
        }
    }

    // ================= 加入房间 =================
    public async void OnClickJoinRoom(string lobbyId)
    {
        if (isJoiningRoom) return; // ?? 防连点
        isJoiningRoom = true;

        try
        {
            Debug.Log("加入房间：" + lobbyId);

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            UIManager.Instance.mainPanel.SetActive(false);
            UIManager.Instance.roomPanel.SetActive(true);
            UIManager.Instance.roomIdText.text = "房间号：" + currentLobby.Id;

            // ? 等待RelayCode（降低频率！！）
            int retry = 0;
            while ((currentLobby.Data == null || !currentLobby.Data.ContainsKey("relayCode")) && retry < 10)
            {
                Debug.Log("等待Host创建Relay...");
                await Task.Delay(2000); // ?? 从1秒改成2秒（避免429）

                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                retry++;
            }

            if (!currentLobby.Data.ContainsKey("relayCode"))
            {
                Debug.LogError("获取RelayCode失败");
                return;
            }

            string relayCode = currentLobby.Data["relayCode"].Value;

            Debug.Log("获取到RelayCode：" + relayCode);

            await NetworkGameManager.Instance.StartClientWithRelay(relayCode);

            UpdatePlayerList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("加入房间失败：" + e);
        }
        finally
        {
            isJoiningRoom = false;
        }
    }

    // ================= 更新玩家列表 =================
    void UpdatePlayerList()
    {
        if (currentLobby == null || currentLobby.Players == null) return;

        string text = "";

        foreach (var player in currentLobby.Players)
        {
            text += player.Id + "\n";
        }

        if (UIManager.Instance != null && UIManager.Instance.playerListText != null)
        {
            UIManager.Instance.playerListText.text = text;
        }

        CheckAllReady();
    }

    // ================= 点击准备 =================
    public async void SetReady()
    {
        if (currentLobby == null) return;

        try
        {
            Debug.Log("点击准备");

            await LobbyService.Instance.UpdatePlayerAsync(
                currentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {
                            "ready",
                            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true")
                        }
                    }
                }
            );

            UIManager.Instance.statusText.text = "已准备";
        }
        catch (System.Exception e)
        {
            Debug.LogError("准备失败：" + e);
        }
    }

    // ================= 检查准备 =================
    void CheckAllReady()
    {
        if (currentLobby == null || currentLobby.Players == null) return;

        foreach (var player in currentLobby.Players)
        {
            if (player.Data == null ||
                !player.Data.ContainsKey("ready") ||
                player.Data["ready"].Value != "true")
            {
                UIManager.Instance.statusText.text = "等待玩家准备...";
                return;
            }
        }

        if (!isCountingDown)
        {
            isCountingDown = true;
            StartCoroutine(StartGameCountdown());
        }
    }

    // ================= 倒计时 =================
    System.Collections.IEnumerator StartGameCountdown()
    {
        UIManager.Instance.statusText.text = "3秒后开始游戏";

        yield return new WaitForSeconds(3);

        UIManager.Instance.statusText.text = "游戏开始！";
    }

    // ================= Lobby刷新（降频版） =================
    void Update()
    {
        if (currentLobby == null) return;

        timer += Time.deltaTime;

        if (timer >= 5f) // ?? 从2秒改成5秒（关键！）
        {
            timer = 0f;
            RefreshLobby();
        }
    }

    async void RefreshLobby()
    {
        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            UpdatePlayerList();
        }
        catch
        {
            Debug.Log("Lobby刷新失败（限流或网络问题）");
        }
    }
}
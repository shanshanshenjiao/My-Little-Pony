using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.Collections.Generic;
using Multiplayer; // ? 必须加这个

public class RoomController : MonoBehaviour
{
    private Lobby currentLobby;
    private bool isCountingDown = false;
    private float timer = 0f; // ? 控制刷新频率

    void Start()
    {
        // ?? 按钮绑定
        UIManager.Instance.createRoomButton.onClick.AddListener(OnClickCreateRoom);

        UIManager.Instance.joinRoomButton.onClick.AddListener(() =>
        {
            string id = UIManager.Instance.roomIdInput.text;
            OnClickJoinRoom(id);
        });

        UIManager.Instance.readyButton.onClick.AddListener(SetReady);
    }

    // ================= 创建房间（Host） =================
    public async void OnClickCreateRoom()
    {
        try
        {
            Debug.Log("点击创建房间");

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("MyRoom", 3);

            Debug.Log("房间创建成功：" + currentLobby.Id);

            // ?? 切UI
            UIManager.Instance.mainPanel.SetActive(false);
            UIManager.Instance.roomPanel.SetActive(true);

            UIManager.Instance.roomIdText.text = "房间号：" + currentLobby.Id;

            // ? 启动Relay（Host）
            string relayCode = await NetworkGameManager.Instance.StartHostWithRelay();

            if (string.IsNullOrEmpty(relayCode))
            {
                Debug.LogError("Relay创建失败");
                return;
            }

            // ? 把relayCode写进Lobby
            await LobbyService.Instance.UpdateLobbyAsync(
                currentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "relayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                    }
                }
            );

            Debug.Log("RelayCode写入Lobby成功：" + relayCode);

            UpdatePlayerList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("创建房间失败：" + e.Message);
        }
    }

    // ================= 加入房间（Client） =================
    public async void OnClickJoinRoom(string lobbyId)
    {
        try
        {
            Debug.Log("加入房间：" + lobbyId);

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            UIManager.Instance.mainPanel.SetActive(false);
            UIManager.Instance.roomPanel.SetActive(true);

            UIManager.Instance.roomIdText.text = "房间号：" + currentLobby.Id;

            // ? 等待Host写入relayCode（防止Key报错）
            while (!currentLobby.Data.ContainsKey("relayCode"))
            {
                Debug.Log("等待Host创建Relay...");
                await Task.Delay(1000);

                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            }

            string relayCode = currentLobby.Data["relayCode"].Value;

            Debug.Log("获取到RelayCode：" + relayCode);

            // ? 启动Client
            await NetworkGameManager.Instance.StartClientWithRelay(relayCode);

            UpdatePlayerList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("加入房间失败：" + e.Message);
        }
    }

    // ================= 更新玩家列表 =================
    void UpdatePlayerList()
    {
        if (currentLobby == null) return;

        string text = "";

        foreach (var player in currentLobby.Players)
        {
            text += player.Id + "\n";
        }

        UIManager.Instance.playerListText.text = text;

        CheckAllReady();
    }

    // ================= 点击准备 =================
    public async void SetReady()
    {
        try
        {
            Debug.Log("点击准备");

            var data = new Dictionary<string, PlayerDataObject>()
            {
                { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true") }
            };

            await LobbyService.Instance.UpdatePlayerAsync(
                currentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions { Data = data }
            );

            UIManager.Instance.statusText.text = "已准备";
        }
        catch (System.Exception e)
        {
            Debug.LogError("准备失败：" + e.Message);
        }
    }

    // ================= 检查是否全部准备 =================
    void CheckAllReady()
    {
        if (currentLobby == null) return;

        foreach (var player in currentLobby.Players)
        {
            if (!player.Data.ContainsKey("ready") || player.Data["ready"].Value != "true")
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

        // ?? 下一步：切场景（你后面做）
        // SceneManager.LoadScene("Game");
    }

    // ================= 定时刷新Lobby（替代async Update） =================
    void Update()
    {
        if (currentLobby == null) return;

        timer += Time.deltaTime;

        if (timer >= 2f)
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
            Debug.Log("Lobby刷新失败");
        }
    }
}
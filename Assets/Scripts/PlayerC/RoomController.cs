using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.Collections.Generic;
using Multiplayer;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RoomController : MonoBehaviour
{
    private Lobby currentLobby;

    private bool isCountingDown = false;
    private float timer = 0f;

    private bool isCreatingRoom = false;
    private bool isJoiningRoom = false;

    private int lastPlayerCount = 0;
    private bool isRefreshing = false;

    private Dictionary<string, GameObject> playerItemDict = new Dictionary<string, GameObject>();

    // ================= 初始化 =================
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
        UIManager.Instance.backButton.onClick.AddListener(OnClickBack);
    }

    // ================= 心跳（防止Lobby消失） =================
    private async void HeartbeatLoop()
    {
        while (currentLobby != null)
        {
            await Task.Delay(15000);

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                Debug.Log("发送心跳");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError("心跳失败: " + e.Reason);
            }
        }
    }

    // ================= 返回 =================
    public async void OnClickBack()
    {
        if (currentLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    currentLobby.Id,
                    AuthenticationService.Instance.PlayerId
                );
            }
            catch { }
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        UIManager.Instance.roomPanel.SetActive(false);
        UIManager.Instance.mainPanel.SetActive(true);

        currentLobby = null;
        isCountingDown = false;
    }

    // ================= 创建房间 =================
    public async void OnClickCreateRoom()
    {
        if (isCreatingRoom) return;
        isCreatingRoom = true;

        try
        {
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("MyRoom", 3);

            // ⭐ 启动心跳
            HeartbeatLoop();

            UIManager.Instance.mainPanel.SetActive(false);
            UIManager.Instance.roomPanel.SetActive(true);
            UIManager.Instance.roomIdText.text = "房间号：" + currentLobby.Id;

            string relayCode = await NetworkGameManager.Instance.StartHostWithRelay();

            if (string.IsNullOrEmpty(relayCode))
            {
                Debug.LogError("Relay创建失败");
                return;
            }

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
        if (isJoiningRoom) return;
        isJoiningRoom = true;

        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            UIManager.Instance.mainPanel.SetActive(false);
            UIManager.Instance.roomPanel.SetActive(true);
            UIManager.Instance.roomIdText.text = "房间号：" + currentLobby.Id;

            int retry = 0;

            while ((currentLobby.Data == null || !currentLobby.Data.ContainsKey("relayCode")) && retry < 10)
            {
                await Task.Delay(2000);
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                retry++;
            }

            if (currentLobby.Data == null || !currentLobby.Data.ContainsKey("relayCode"))
            {
                Debug.LogError("获取RelayCode失败");
                return;
            }

            string relayCode = currentLobby.Data["relayCode"].Value;

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

    // ================= 玩家列表 =================
    void UpdatePlayerList()
    {
        if (currentLobby == null || currentLobby.Players == null) return;

        Transform parent = UIManager.Instance.playerListParent;
        GameObject prefab = UIManager.Instance.playerItemPrefab;

        HashSet<string> currentIds = new HashSet<string>();

        foreach (var player in currentLobby.Players)
        {
            string playerId = player.Id;
            currentIds.Add(playerId);

            GameObject item;

            // ===== 已存在 → 更新 =====
            if (playerItemDict.ContainsKey(playerId))
            {
                item = playerItemDict[playerId];
            }
            // ===== 新玩家 → 创建 =====
            else
            {
                item = Instantiate(prefab, parent);
                playerItemDict[playerId] = item;
            }

            // ===== 更新UI =====
            Text nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            Text readyText = item.transform.Find("ReadyText")?.GetComponent<Text>();
            Text youText = item.transform.Find("YouText")?.GetComponent<Text>();
            Image avatarImg = item.transform.Find("AvatarImage")?.GetComponent<Image>();

            if (nameText == null || readyText == null) continue;

            string shortId = playerId.Substring(0, 4);
            nameText.text = "玩家 " + shortId;

            if (playerId == currentLobby.HostId)
                nameText.text += " (房主)";

            if (player.Data != null &&
                player.Data.ContainsKey("ready") &&
                player.Data["ready"].Value == "true")
                readyText.text = "已准备";
            else
                readyText.text = "未准备";

            if (youText != null)
                youText.gameObject.SetActive(playerId == AuthenticationService.Instance.PlayerId);

            if (avatarImg != null && AvatarManager.Instance != null)
                avatarImg.sprite = AvatarManager.Instance.GetAvatar(playerId);
        }

        // ===== ⭐ 删除已退出玩家 =====
        List<string> toRemove = new List<string>();

        foreach (var kvp in playerItemDict)
        {
            if (!currentIds.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var id in toRemove)
        {
            playerItemDict.Remove(id);
        }

        CheckAllReady();
    }

    // ================= 准备 =================
    public async void SetReady()
    {
        if (currentLobby == null) return;

        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(
                currentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true") }
                    }
                }
            );

            UIManager.Instance.statusText.text = "已准备";
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("准备失败: " + e.Reason);
        }
    }

    // ================= 检查准备 =================
    void CheckAllReady()
    {
        if (currentLobby == null) return;

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

    // ================= 开始游戏 =================
    System.Collections.IEnumerator StartGameCountdown()
    {
        UIManager.Instance.statusText.text = "3秒后开始游戏";

        yield return new WaitForSeconds(3);

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    // ================= Lobby刷新 =================
    void Update()
    {
        if (currentLobby == null) return;

        timer += Time.deltaTime;

        if (timer >= 2f) // ⭐ 降低请求频率
        {
            timer = 0f;
            RefreshLobby();
        }
    }

    async void RefreshLobby()
    {
        if (isRefreshing) return;
        isRefreshing = true;

        try
        {
            Lobby newLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

            currentLobby = newLobby;
            UpdatePlayerList();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Lobby刷新失败: " + e.Reason);
        }

        isRefreshing = false;
    }
}
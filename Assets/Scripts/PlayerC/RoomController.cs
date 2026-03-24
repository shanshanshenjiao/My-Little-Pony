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

    private bool isRefreshing = false;
    private bool isCreatingRoom = false;
    private bool isJoiningRoom = false;

    private float timer = 0f;
    private bool isCountingDown = false;

    // ⭐ 玩家UI缓存
    private Dictionary<string, GameObject> playerItemDict = new Dictionary<string, GameObject>();

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
            OnClickJoinRoom(UIManager.Instance.roomIdInput.text);
        });
        UIManager.Instance.readyButton.onClick.AddListener(SetReady);
        UIManager.Instance.backButton.onClick.AddListener(OnClickBack);
    }

    // ================= 清理UI =================
    void ClearPlayerUI()
    {
        foreach (var item in playerItemDict.Values)
        {
            if (item != null)
                Destroy(item);
        }
        playerItemDict.Clear();
    }

    // ================= 心跳 =================
    async void HeartbeatLoop()
    {
        while (currentLobby != null)
        {
            await Task.Delay(15000);
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
            catch { }
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
                    AuthenticationService.Instance.PlayerId);
            }
            catch { }
        }

        ClearPlayerUI();

        NetworkManager.Singleton?.Shutdown();

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

        ClearPlayerUI();

        try
        {
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("MyRoom", 3);
            HeartbeatLoop();

            UIManager.Instance.mainPanel.SetActive(false);
            UIManager.Instance.roomPanel.SetActive(true);
            UIManager.Instance.roomIdText.text = "房间号：" + currentLobby.Id;

            string relayCode = await NetworkGameManager.Instance.StartHostWithRelay();

            if (!string.IsNullOrEmpty(relayCode))
            {
                await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id,
                    new UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, DataObject>
                        {
                            { "relayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                        }
                    });
            }

            UpdatePlayerList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("创建房间失败：" + e);
        }

        isCreatingRoom = false;
    }

    // ================= 加入房间 =================
    public async void OnClickJoinRoom(string lobbyId)
    {
        if (isJoiningRoom) return;
        isJoiningRoom = true;

        ClearPlayerUI();

        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            UIManager.Instance.mainPanel.SetActive(false);
            UIManager.Instance.roomPanel.SetActive(true);
            UIManager.Instance.roomIdText.text = "房间号：" + currentLobby.Id;

            // 等待 relayCode
            for (int i = 0; i < 10; i++)
            {
                if (currentLobby.Data != null && currentLobby.Data.ContainsKey("relayCode"))
                    break;

                await Task.Delay(2000);
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            }

            if (currentLobby.Data != null && currentLobby.Data.ContainsKey("relayCode"))
            {
                string relayCode = currentLobby.Data["relayCode"].Value;
                await NetworkGameManager.Instance.StartClientWithRelay(relayCode);
            }

            UpdatePlayerList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("加入房间失败：" + e);
        }

        isJoiningRoom = false;
    }

    // ================= 玩家列表（核心稳定版） =================
    void UpdatePlayerList()
    {
        if (currentLobby == null || currentLobby.Players == null) return;

        Transform parent = UIManager.Instance.playerListParent;
        GameObject prefab = UIManager.Instance.playerItemPrefab;

        HashSet<string> currentIds = new HashSet<string>();

        foreach (var player in currentLobby.Players)
        {
            string id = player.Id;
            currentIds.Add(id);

            // ⭐ 核心：安全获取
            if (!playerItemDict.TryGetValue(id, out GameObject item) || item == null)
            {
                item = Instantiate(prefab, parent);
                playerItemDict[id] = item;
            }

            // 更新UI
            Text name = item.transform.Find("NameText")?.GetComponent<Text>();
            Text ready = item.transform.Find("ReadyText")?.GetComponent<Text>();
            Text you = item.transform.Find("YouText")?.GetComponent<Text>();
            Image avatar = item.transform.Find("AvatarImage")?.GetComponent<Image>();

            if (name == null || ready == null) continue;

            string shortId = id.Length > 4 ? id.Substring(0, 4) : id;
            name.text = "玩家 " + shortId + (id == currentLobby.HostId ? " (房主)" : "");

            bool isReady = player.Data != null &&
                           player.Data.ContainsKey("ready") &&
                           player.Data["ready"].Value == "true";

            ready.text = isReady ? "已准备" : "未准备";

            if (you != null)
                you.gameObject.SetActive(id == AuthenticationService.Instance.PlayerId);

            if (avatar != null && AvatarManager.Instance != null)
                avatar.sprite = AvatarManager.Instance.GetAvatar(id);
        }

        // 删除退出玩家
        List<string> removeList = new List<string>();

        foreach (var kvp in playerItemDict)
        {
            if (!currentIds.Contains(kvp.Key))
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);

                removeList.Add(kvp.Key);
            }
        }

        foreach (var id in removeList)
            playerItemDict.Remove(id);

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
                });

            UIManager.Instance.statusText.text = "已准备";
        }
        catch { }
    }

    // ================= 检查准备 =================
    void CheckAllReady()
    {
        if (currentLobby == null) return;

        foreach (var p in currentLobby.Players)
        {
            if (p.Data == null ||
                !p.Data.ContainsKey("ready") ||
                p.Data["ready"].Value != "true")
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

    System.Collections.IEnumerator StartGameCountdown()
    {
        UIManager.Instance.statusText.text = "3秒后开始游戏";
        yield return new WaitForSeconds(3);

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    // ================= 刷新 =================
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
        if (isRefreshing) return;
        isRefreshing = true;

        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            UpdatePlayerList();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("刷新失败: " + e.Reason);
        }

        isRefreshing = false;
    }
}
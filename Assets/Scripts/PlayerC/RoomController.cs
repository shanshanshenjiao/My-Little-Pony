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

    // ================= 返回 =================
    public void OnClickBack()
    {
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

    // ================= ⭐ 玩家列表 =================
    void UpdatePlayerList()
    {
        if (currentLobby == null || currentLobby.Players == null) return;

        Transform parent = UIManager.Instance.playerListParent;
        GameObject prefab = UIManager.Instance.playerItemPrefab;

        // 清空
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }

        foreach (var player in currentLobby.Players)
        {
            GameObject item = Instantiate(prefab, parent);

            Transform nameTf = item.transform.Find("NameText");
            Transform readyTf = item.transform.Find("ReadyText");
            Transform youTf = item.transform.Find("YouText");
            Transform avatarTf = item.transform.Find("AvatarImage");

            if (nameTf == null || readyTf == null) continue;

            Text nameText = nameTf.GetComponent<Text>();
            Text readyText = readyTf.GetComponent<Text>();
            Text youText = youTf ? youTf.GetComponent<Text>() : null;
            Image avatarImg = avatarTf ? avatarTf.GetComponent<Image>() : null;

            // ===== 名字 =====
            string shortId = player.Id.Substring(0, 4);
            nameText.text = "玩家 " + shortId;

            if (player.Id == currentLobby.HostId)
                nameText.text += " (房主)";

            // ===== 准备状态 =====
            if (player.Data != null &&
                player.Data.ContainsKey("ready") &&
                player.Data["ready"].Value == "true")
                readyText.text = "已准备";
            else
                readyText.text = "未准备";

            // ===== 自己 =====
            if (youText != null)
                youText.gameObject.SetActive(player.Id == AuthenticationService.Instance.PlayerId);

            // ===== ⭐ 头像（调用独立脚本）=====
            if (avatarImg != null && AvatarManager.Instance != null)
            {
                avatarImg.sprite = AvatarManager.Instance.GetAvatar(player.Id);
            }
        }

        CheckAllReady();
    }

    // ================= 准备 =================
    public async void SetReady()
    {
        if (currentLobby == null) return;

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

    // ================= ⭐ 开始游戏 =================
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

        if (timer >= 1f)
        {
            timer = 0f;
            RefreshLobby();
        }
    }

    async void RefreshLobby()
    {
        try
        {
            Lobby newLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

            // ⭐ 人数变化才重点刷新
            if (newLobby.Players.Count != lastPlayerCount)
            {
                lastPlayerCount = newLobby.Players.Count;
                currentLobby = newLobby;
                UpdatePlayerList();
                return;
            }

            currentLobby = newLobby;
            UpdatePlayerList();
        }
        catch
        {
            Debug.Log("Lobby刷新失败");
        }
    }
}
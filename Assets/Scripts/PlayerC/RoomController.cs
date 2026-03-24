using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Multiplayer;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class RoomController : MonoBehaviour
{
    private Lobby currentLobby;

    private bool isRefreshing = false;
    private bool isCreatingRoom = false;
    private bool isJoiningRoom = false;

    private float timer = 0f;

    // ⭐ 拆出去的模块
    public PlayerListUI playerListUI;
    public RoomStateChecker stateChecker;

    void Start()
    {
        UIManager.Instance.createRoomButton.onClick.AddListener(OnClickCreateRoom);
        UIManager.Instance.joinRoomButton.onClick.AddListener(() =>
        {
            OnClickJoinRoom(UIManager.Instance.roomIdInput.text);
        });
        UIManager.Instance.readyButton.onClick.AddListener(SetReady);
        UIManager.Instance.backButton.onClick.AddListener(OnClickBack);

        UIManager.Instance.startGameButton.onClick.AddListener(OnClickStartGame);
        UIManager.Instance.startGameButton.interactable = false;
    }

    void ClearPlayerUI()
    {
        playerListUI?.Clear();
    }

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
    }

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
                await LobbyService.Instance.UpdateLobbyAsync(
                    currentLobby.Id,
                    new UpdateLobbyOptions
                    {
                        Data = new System.Collections.Generic.Dictionary<string, DataObject>
                        {
                            { "relayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                        }
                    });
            }

            UpdateAll();
        }
        catch (System.Exception e)
        {
            Debug.LogError("创建房间失败：" + e);
        }

        isCreatingRoom = false;
    }

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

            UpdateAll();
        }
        catch (System.Exception e)
        {
            Debug.LogError("加入房间失败：" + e);
        }

        isJoiningRoom = false;
    }

    void UpdateAll()
    {
        if (currentLobby == null) return;

        playerListUI?.UpdateList(currentLobby);
        stateChecker?.UpdateState(currentLobby);
    }

    public async void SetReady()
    {
        if (currentLobby == null) return;

        await LobbyService.Instance.UpdatePlayerAsync(
            currentLobby.Id,
            AuthenticationService.Instance.PlayerId,
            new UpdatePlayerOptions
            {
                Data = new System.Collections.Generic.Dictionary<string, PlayerDataObject>
                {
                    { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "true") }
                }
            });
    }

    public void OnClickStartGame()
    {
        if (currentLobby == null) return;

        int playerCount = currentLobby.Players.Count;

        bool allReady = true;

        foreach (var p in currentLobby.Players)
        {
            if (p.Data == null ||
                !p.Data.ContainsKey("ready") ||
                p.Data["ready"].Value != "true")
            {
                allReady = false;
                break;
            }
        }

        bool isHost = AuthenticationService.Instance.PlayerId == currentLobby.HostId;

        // ⭐ 条件满足才开始
        if (playerCount == 3 && allReady && isHost)
        {
            // ================= ⭐ 核心修复在这里 =================
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    client.PlayerObject.Despawn(true);
                }
            }
            // =================================================

            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

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
            UpdateAll();
        }
        catch (LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                OnClickBack();
                return;
            }
        }

        isRefreshing = false;
    }
}
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // 只有服务器执行游戏开始逻辑
        if (IsServer)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        InitPlayers();

        Debug.Log("玩家初始化完成");
    }

    void InitPlayers()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            PlayerRepository.Instance.AddPlayer(clientId);

            Debug.Log("添加玩家: " + clientId);
        }
    }
}
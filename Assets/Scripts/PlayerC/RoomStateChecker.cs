using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

public class RoomStateChecker : MonoBehaviour
{
    public void UpdateState(Lobby lobby)
    {
        if (lobby == null) return;

        bool canStart = CanStartGame(lobby);

        UIManager.Instance.startGameButton.interactable = canStart;

        int count = lobby.Players.Count;

        if (count < 3)
            UIManager.Instance.statusText.text = "等待玩家加入...";
        else if (!AllReady(lobby))
            UIManager.Instance.statusText.text = "等待玩家准备...";
        else if (!IsHost(lobby))
            UIManager.Instance.statusText.text = "等待房主开始游戏...";
        else
            UIManager.Instance.statusText.text = "可以开始游戏！";
    }

    public bool CanStartGame(Lobby lobby)
    {
        return lobby != null &&
               lobby.Players.Count == 3 &&
               AllReady(lobby) &&
               IsHost(lobby);
    }

    bool AllReady(Lobby lobby)
    {
        foreach (var p in lobby.Players)
        {
            if (p.Data == null ||
                !p.Data.ContainsKey("ready") ||
                p.Data["ready"].Value != "true")
                return false;
        }
        return true;
    }

    bool IsHost(Lobby lobby)
    {
        return AuthenticationService.Instance.PlayerId == lobby.HostId;
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

public class PlayerListUI : MonoBehaviour
{
    public Transform parent;
    public GameObject prefab;

    private Dictionary<string, GameObject> playerItemDict = new Dictionary<string, GameObject>();

    // ================= 更新玩家列表 =================
    public void UpdateList(Lobby lobby)
    {
        if (lobby == null || lobby.Players == null) return;

        HashSet<string> currentIds = new HashSet<string>();

        foreach (var player in lobby.Players)
        {
            string id = player.Id;
            currentIds.Add(id);

            if (!playerItemDict.TryGetValue(id, out GameObject item) || item == null)
            {
                item = Instantiate(prefab, parent);
                playerItemDict[id] = item;
            }

            // ===== UI获取 =====
            Text name = item.transform.Find("NameText")?.GetComponent<Text>();
            Text ready = item.transform.Find("ReadyText")?.GetComponent<Text>();
            Text you = item.transform.Find("YouText")?.GetComponent<Text>();
            Image avatar = item.transform.Find("AvatarImage")?.GetComponent<Image>(); // ? 补回来！

            if (name == null || ready == null) continue;

            string shortId = id.Length > 4 ? id.Substring(0, 4) : id;

            name.text = "玩家 " + shortId +
                        (id == lobby.HostId ? " (房主)" : "");

            bool isReady = player.Data != null &&
                           player.Data.ContainsKey("ready") &&
                           player.Data["ready"].Value == "true";

            ready.text = isReady ? "已准备" : "未准备";

            if (you != null)
                you.gameObject.SetActive(id == AuthenticationService.Instance.PlayerId);

            // ===== ? 头像恢复 =====
            if (avatar != null && AvatarManager.Instance != null)
            {
                avatar.sprite = AvatarManager.Instance.GetAvatar(id);
            }
        }

        // ===== 删除退出玩家 =====
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
    }

    // ================= 清空 =================
    public void Clear()
    {
        foreach (var item in playerItemDict.Values)
        {
            if (item != null)
                Destroy(item);
        }

        playerItemDict.Clear();
    }
}
using UnityEngine;

public class AvatarManager : MonoBehaviour
{
    public static AvatarManager Instance;

    private Sprite[] avatars;

    void Awake()
    {
        Instance = this;

        // ⭐ 加载头像资源
        avatars = Resources.LoadAll<Sprite>("Avatars");

        if (avatars == null || avatars.Length == 0)
        {
            Debug.LogError("头像加载失败！请检查 Resources/Avatars");
        }
    }

    // ⭐ 对外提供接口
    public Sprite GetAvatar(string playerId)
    {
        if (avatars == null || avatars.Length == 0)
            return null;

        int hash = Mathf.Abs(playerId.GetHashCode());
        int index = hash % avatars.Length;

        return avatars[index];
    }
}
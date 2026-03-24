using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // 面板
    public GameObject loginPanel;
    public GameObject mainPanel;
    public GameObject roomPanel;

    // 登录
    public Button startButton;

    // 主界面
    public Button createRoomButton;
    public Button joinRoomButton;
    public InputField roomIdInput;

    // 房间
    public Text roomIdText;
    public Button readyButton;
    public Button startGameButton;
    public Text statusText;
    public Button backButton;

    // ⭐ 新增（关键）
    public Transform playerListParent;
    public GameObject playerItemPrefab;
}
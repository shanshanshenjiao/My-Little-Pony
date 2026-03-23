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
    public Text playerListText;
    public Button readyButton;
    public Text statusText;
}
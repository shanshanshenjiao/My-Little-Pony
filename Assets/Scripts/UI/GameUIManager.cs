using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Sanguosha.UI
{
    /// <summary>
    /// 游戏 UI 管理器
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }
        
        [Header("界面面板")]
        public GameObject loginPanel;
        public GameObject lobbyPanel;
        public GameObject gamePanel;
        public GameObject gameOverPanel;
        
        [Header("登录界面")]
        public InputField usernameInput;
        public InputField serverAddressInput;
        public Button connectButton;
        
        [Header("游戏界面")]
        public Text turnIndicatorText;
        public Text gameMessageText;
        public Button playCardButton;
        public Button endTurnButton;
        public Transform playedCardsContainer;
        
        [Header("游戏结束")]
        public Text winnerTitleText;
        public Text winnerMessageText;
        
        [Header("设置")]
        public string defaultServerAddress = "127.0.0.1:8888";
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            // 初始化 UI 状态
            ShowPanel("login");
            
            // 绑定按钮事件
            if (connectButton != null)
            {
                connectButton.onClick.AddListener(OnConnectButtonClick);
            }
            
            if (playCardButton != null)
            {
                playCardButton.onClick.AddListener(OnPlayCardClick);
            }
            
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClick);
            }
            
            // 设置默认服务器地址
            if (serverAddressInput != null)
            {
                serverAddressInput.text = defaultServerAddress;
            }
        }
        
        /// <summary>
        /// 显示指定面板
        /// </summary>
        public void ShowPanel(string panelName)
        {
            loginPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            gamePanel.SetActive(false);
            gameOverPanel.SetActive(false);
            
            switch (panelName)
            {
                case "login":
                    loginPanel.SetActive(true);
                    break;
                case "lobby":
                    lobbyPanel.SetActive(true);
                    break;
                case "game":
                    gamePanel.SetActive(true);
                    break;
                case "gameOver":
                    gameOverPanel.SetActive(true);
                    break;
            }
        }
        
        /// <summary>
        /// 连接按钮点击
        /// </summary>
        private void OnConnectButtonClick()
        {
            string username = usernameInput?.text.Trim();
            string serverAddress = serverAddressInput?.text.Trim();
            
            if (string.IsNullOrEmpty(username))
            {
                ShowMessage("请输入昵称");
                return;
            }
            
            if (string.IsNullOrEmpty(serverAddress))
            {
                ShowMessage("请输入服务器地址");
                return;
            }
            
            // 保存用户名
            PlayerPrefs.SetString("Username", username);
            
            // 连接到服务器
            Network.NetworkManager.Instance?.ConnectToServer(
                serverAddress.Split(':')[0],
                int.Parse(serverAddress.Split(':')[1])
            );
            
            // 切换到大厅
            ShowPanel("lobby");
            ShowMessage($"欢迎，{username}！");
        }
        
        /// <summary>
        /// 出牌按钮点击
        /// </summary>
        private void OnPlayCardClick()
        {
            GameCore.Card selectedCard = CardManager.Instance?.GetSelectedCard();
            
            if (selectedCard == null)
            {
                ShowMessage("请先选择一张牌");
                return;
            }
            
            // 创建游戏动作
            GameCore.GameAction action = new GameCore.GameAction
            {
                actionType = GameCore.ActionType.PlayCard,
                playerId = Network.NetworkManager.Instance?.localPlayerId,
                cardId = selectedCard.cardId,
                targetId = SelectTarget() // TODO: 实现目标选择
            };
            
            // 发送到服务器
            GameCore.GameManager.Instance?.ProcessAction(action);
            
            // 清除选中
            CardManager.Instance?.ClearSelection();
        }
        
        /// <summary>
        /// 结束回合按钮点击
        /// </summary>
        private void OnEndTurnClick()
        {
            GameCore.GameAction action = new GameCore.GameAction
            {
                actionType = GameCore.ActionType.EndTurn,
                playerId = Network.NetworkManager.Instance?.localPlayerId
            };
            
            GameCore.GameManager.Instance?.ProcessAction(action);
        }
        
        /// <summary>
        /// 选择目标（简化版）
        /// </summary>
        private string SelectTarget()
        {
            // TODO: 实现目标选择 UI
            return "player_1";
        }
        
        /// <summary>
        /// 显示消息
        /// </summary>
        public void ShowMessage(string message, float duration = 2f)
        {
            if (gameMessageText != null)
            {
                gameMessageText.text = message;
                
                if (duration > 0)
                {
                    CancelInvoke(nameof(ClearMessage));
                    Invoke(nameof(ClearMessage), duration);
                }
            }
        }
        
        private void ClearMessage()
        {
            if (gameMessageText != null)
            {
                gameMessageText.text = "";
            }
        }
        
        /// <summary>
        /// 更新回合指示器
        /// </summary>
        public void UpdateTurnIndicator(string playerName, bool isMyTurn)
        {
            if (turnIndicatorText != null)
            {
                turnIndicatorText.text = $"当前回合：{playerName}";
            }
            
            // 更新按钮状态
            if (playCardButton != null && endTurnButton != null)
            {
                playCardButton.interactable = isMyTurn;
                endTurnButton.interactable = isMyTurn;
            }
        }
        
        /// <summary>
        /// 显示游戏结束
        /// </summary>
        public void ShowGameOver(bool isRebelWin)
        {
            ShowPanel("gameOver");
            
            if (winnerTitleText != null)
            {
                winnerTitleText.text = isRebelWin ? "反贼胜利！" : "主公胜利！";
            }
            
            if (winnerMessageText != null)
            {
                winnerMessageText.text = isRebelWin ? "主公已被击败" : "所有反贼已被消灭";
            }
        }
        
        /// <summary>
        /// 返回登录界面
        /// </summary>
        public void BackToLogin()
        {
            Network.NetworkManager.Instance?.Disconnect();
            ShowPanel("login");
        }
        
        ///<summary>
        /// 重新开始
        /// </summary>
        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

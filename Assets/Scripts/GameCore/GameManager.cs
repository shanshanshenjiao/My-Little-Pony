using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Sanguosha.GameCore
{
    /// <summary>
    /// 游戏管理器 - 核心单例
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("游戏状态")]
        public bool gameStarted;
        public GamePhase currentPhase;
        public Player currentPlayer;
        public int turnCount;
        
        [Header("游戏数据")]
        public CardDeck cardDeck;
        public Dictionary<string, Player> players;
        public List<Card> playedCards;
        
        [Header("事件")]
        public UnityEngine.Events.UnityEvent onGameStart;
        public PlayerEvent onTurnStart;
        public CardPlayerEvent onCardPlayed;
        public UnityEngine.Events.UnityEvent onGameOver;
        
        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            Initialize();
        }
        
        /// <summary>
        /// 初始化游戏
        /// </summary>
        private void Initialize()
        {
            players = new Dictionary<string, Player>();
            playedCards = new List<Card>();
            cardDeck = new CardDeck();
            currentPhase = GamePhase.Waiting;
            turnCount = 0;
        }
        
        /// <summary>
        /// 添加玩家
        /// </summary>
        public bool AddPlayer(string playerId, string username, bool isLocal = false)
        {
            if (players.Count >= 4)
            {
                Debug.LogWarning("玩家数量已达上限");
                return false;
            }
            
            Player player = new Player(playerId, username, isLocal);
            players.Add(playerId, player);
            Debug.Log($"玩家加入：{username} ({playerId})");
            
            return true;
        }
        
        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (players.Count != 4)
            {
                Debug.LogError("需要 4 名玩家才能开始游戏");
                return;
            }
            
            // 分配身份
            AssignIdentities();
            
            // 分发初始手牌
            DistributeInitialCards();
            
            // 选择起始玩家
            ChooseFirstPlayer();
            
            gameStarted = true;
            currentPhase = GamePhase.Playing;
            
            onGameStart?.Invoke();
            Debug.Log("游戏开始！");
        }
        
        /// <summary>
        /// 分配身份
        /// </summary>
        private void AssignIdentities()
        {
            List<Identity> identities = new List<Identity>
            {
                Identity.Lord,
                Identity.Loyalist,
                Identity.Rebel,
                Identity.Rebel
            };
            
            // 洗牌
            for (int i = 0; i < identities.Count; i++)
            {
                int randomIndex = Random.Range(i, identities.Count);
                Identity temp = identities[i];
                identities[i] = identities[randomIndex];
                identities[randomIndex] = temp;
            }
            
            // 分配
            int index = 0;
            foreach (var player in players.Values)
            {
                player.identity = identities[index++];
                
                // 主公体力 +1
                if (player.identity == Identity.Lord)
                {
                    player.maxHp = 5;
                    player.currentHp = 5;
                }
            }
        }
        
        /// <summary>
        /// 分发初始手牌（每人 4 张）
        /// </summary>
        private void DistributeInitialCards()
        {
            foreach (var player in players.Values)
            {
                List<Card> initialCards = cardDeck.DrawCards(4);
                player.DrawCards(initialCards);
            }
        }
        
        /// <summary>
        /// 选择起始玩家
        /// </summary>
        private void ChooseFirstPlayer()
        {
            List<string> playerIds = new List<string>(players.Keys);
            int randomIndex = Random.Range(0, playerIds.Count);
            currentPlayer = players[playerIds[randomIndex]];
        }
        
        /// <summary>
        /// 处理玩家动作
        /// </summary>
        public void ProcessAction(GameAction action)
        {
            if (!gameStarted)
            {
                Debug.LogWarning("游戏尚未开始");
                return;
            }
            
            if (currentPlayer.playerId != action.playerId)
            {
                Debug.LogWarning("不是你的回合");
                return;
            }
            
            switch (action.actionType)
            {
                case ActionType.PlayCard:
                    HandlePlayCard(action);
                    break;
                case ActionType.EndTurn:
                    HandleEndTurn();
                    break;
            }
        }
        
        /// <summary>
        /// 处理出牌
        /// </summary>
        private void HandlePlayCard(GameAction action)
        {
            Player player = currentPlayer;
            Card card = player.handCards.Find(c => c.cardId == action.cardId);
            
            if (card == null)
            {
                Debug.LogWarning("无效的卡牌");
                return;
            }
            
            // 移除手牌
            player.DiscardCard(card);
            playedCards.Add(card);
            
            // 触发事件
            onCardPlayed?.Invoke(card, player);
            
            // 根据卡牌类型执行效果
            ExecuteCardEffect(card, action);
        }
        
        /// <summary>
        /// 执行卡牌效果
        /// </summary>
        private void ExecuteCardEffect(Card card, GameAction action)
        {
            switch (card.cardName)
            {
                case "杀":
                    // 检查是否超过出杀次数
                    // TODO: 实现攻击次数限制
                    
                    // 对目标造成伤害
                    if (players.ContainsKey(action.targetId))
                    {
                        Player target = players[action.targetId];
                        target.TakeDamage(1);
                    }
                    break;
                    
                case "闪":
                    // 响应【杀】
                    break;
                    
                case "桃":
                    // 恢复体力
                    currentPlayer.RecoverHp(1);
                    break;
                    
                case "无中生有":
                    // 摸两张牌
                    List<Card> newCards = cardDeck.DrawCards(2);
                    currentPlayer.DrawCards(newCards);
                    break;
            }
        }
        
        /// <summary>
        /// 处理结束回合
        /// </summary>
        private void HandleEndTurn()
        {
            Player previousPlayer = currentPlayer;
            
            // 弃牌阶段
            while (previousPlayer.handCards.Count > previousPlayer.currentHp)
            {
                Card discarded = previousPlayer.handCards[previousPlayer.handCards.Count - 1];
                previousPlayer.DiscardCard(discarded);
                cardDeck.Discard(discarded);
            }
            
            // 切换到下一个玩家
            NextTurn();
        }
        
        /// <summary>
        /// 下一回合
        /// </summary>
        private void NextTurn()
        {
            List<string> alivePlayers = new List<string>();
            foreach (var player in players.Values)
            {
                if (player.isAlive)
                {
                    alivePlayers.Add(player.playerId);
                }
            }
            
            int currentIndex = alivePlayers.IndexOf(currentPlayer.playerId);
            int nextIndex = (currentIndex + 1) % alivePlayers.Count;
            
            currentPlayer = players[alivePlayers[nextIndex]];
            turnCount++;
            
            // 摸牌阶段
            List<Card> drawCards = cardDeck.DrawCards(2);
            currentPlayer.DrawCards(drawCards);
            
            // 触发回合开始事件
            onTurnStart?.Invoke(currentPlayer);
            
            Debug.Log($"回合 {turnCount}: {currentPlayer.username} 的回合");
        }
        
        /// <summary>
        /// 检查胜利条件
        /// </summary>
        public void CheckVictory()
        {
            bool lordAlive = false;
            List<Player> rebels = new List<Player>();
            
            foreach (var player in players.Values)
            {
                if (!player.isAlive) continue;
                
                if (player.identity == Identity.Lord)
                {
                    lordAlive = true;
                }
                else if (player.identity == Identity.Rebel)
                {
                    rebels.Add(player);
                }
            }
            
            if (!lordAlive)
            {
                EndGame(Identity.Rebel);
            }
            else if (rebels.Count == 0)
            {
                EndGame(Identity.Lord);
            }
        }
        
        /// <summary>
        /// 结束游戏
        /// </summary>
        private void EndGame(Identity winner)
        {
            gameStarted = false;
            currentPhase = GamePhase.Ended;
            
            Debug.Log($"游戏结束！{(winner == Identity.Rebel ? "反贼" : "主公")} 胜利！");
            onGameOver?.Invoke();
        }
    }
    
    /// <summary>
    /// 游戏阶段枚举
    /// </summary>
    public enum GamePhase
    {
        Waiting,
        Playing,
        Ended
    }
    
    /// <summary>
    /// 游戏动作数据结构
    /// </summary>
    [System.Serializable]
    public class GameAction
    {
        public ActionType actionType;
        public string playerId;
        public string cardId;
        public string targetId;
    }
    
    /// <summary>
    /// 动作类型枚举
    /// </summary>
    public enum ActionType
    {
        None,
        PlayCard,
        EndTurn,
        UseSkill
    }
    
    /// <summary>
    /// 自定义事件类（支持 Player 参数）
    /// </summary>
    [System.Serializable]
    public class PlayerEvent : UnityEngine.Events.UnityEvent<Player> { }
    
    /// <summary>
    /// 自定义事件类（支持 Card 和 Player 参数）
    /// </summary>
    [System.Serializable]
    public class CardPlayerEvent : UnityEngine.Events.UnityEvent<Card, Player> { }
}

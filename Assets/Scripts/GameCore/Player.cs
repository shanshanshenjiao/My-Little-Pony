using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sanguosha.GameCore
{
    /// <summary>
    /// 玩家数据类型
    /// </summary>
    [Serializable]
    public class Player
    {
        public string playerId;
        public string username;
        public string generalName;      // 武将名
        public Identity identity;       // 身份
        public int currentHp;
        public int maxHp;
        public List<Card> handCards;
        public Equipment equipment;
        public bool isAlive;
        public bool isLocalPlayer;    // 是否为本地玩家
        
        public Player(string id, string name, bool isLocal = false)
        {
            this.playerId = id;
            this.username = name;
            this.isLocalPlayer = isLocal;
            this.currentHp = 4;
            this.maxHp = 4;
            this.handCards = new List<Card>();
            this.equipment = new Equipment();
            this.isAlive = true;
        }
        
        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(int amount)
        {
            currentHp = Mathf.Max(0, currentHp - amount);
            if (currentHp <= 0)
            {
                isAlive = false;
                OnDie();
            }
        }
        
        /// <summary>
        /// 恢复体力
        /// </summary>
        public void RecoverHp(int amount)
        {
            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }
        
        /// <summary>
        /// 摸牌
        /// </summary>
        public void DrawCards(List<Card> cards)
        {
            handCards.AddRange(cards);
        }
        
        /// <summary>
        /// 弃牌
        /// </summary>
        public void DiscardCard(Card card)
        {
            handCards.Remove(card);
        }
        
        /// <summary>
        /// 死亡事件
        /// </summary>
        private void OnDie()
        {
            Debug.Log($"[{username}] 阵亡！身份：{identity}");
            // 触发死亡事件，显示身份等
        }
    }
    
    /// <summary>
    /// 身份枚举
    /// </summary>
    public enum Identity
    {
        None,
        Lord,       // 主公
        Loyalist,   // 忠臣
        Rebel       // 反贼
    }
    
    /// <summary>
    /// 装备数据结构
    /// </summary>
    [Serializable]
    public class Equipment
    {
        public Card weapon;
        public Card armor;
        public Card horsePlus;
        public Card horseMinus;
    }
}

using System;
using UnityEngine;

namespace Sanguosha.GameCore
{
    /// <summary>
    /// 卡牌数据类型
    /// </summary>
    [Serializable]
    public class Card
    {
        public string cardId;
        public string cardName;
        public CardType cardType;
        public Suit suit;
        public int number;
        public string description;
        public Sprite cardSprite;
        
        public Card(string id, string name, CardType type, Suit suit = Suit.None, int number = 0, string desc = "")
        {
            this.cardId = id;
            this.cardName = name;
            this.cardType = type;
            this.suit = suit;
            this.number = number;
            this.description = desc;
        }
    }
    
    /// <summary>
    /// 卡牌类型枚举
    /// </summary>
    public enum CardType
    {
        None,
        Basic,      // 基本牌
        Scroll,     // 锦囊牌
        Equipment   // 装备牌
    }
    
    /// <summary>
    /// 花色枚举
    /// </summary>
    public enum Suit
    {
        None,
        Hearts,     // 红桃
        Diamonds,   // 方块
        Clubs,      // 梅花
        Spades      // 黑桃
    }
}

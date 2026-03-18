using System.Collections.Generic;
using UnityEngine;

namespace Sanguosha.GameCore
{
    /// <summary>
    /// 牌堆管理类
    /// </summary>
    public class CardDeck
    {
        private List<Card> deck;          // 牌堆
        private List<Card> discardPile;   // 弃牌堆
        
        public CardDeck()
        {
            deck = new List<Card>();
            discardPile = new List<Card>();
            InitializeDeck();
        }
        
        /// <summary>
        /// 初始化标准牌堆（108 张）
        /// </summary>
        private void InitializeDeck()
        {
            deck.Clear();
            int cardId = 0;
            
            // 基本牌
            // 杀 (30 张)
            for (int i = 0; i < 30; i++)
            {
                deck.Add(new Card($"card_{cardId++}", "杀", CardType.Basic));
            }
            
            // 闪 (15 张)
            for (int i = 0; i < 15; i++)
            {
                deck.Add(new Card($"card_{cardId++}", "闪", CardType.Basic));
            }
            
            // 桃 (8 张)
            for (int i = 0; i < 8; i++)
            {
                deck.Add(new Card($"card_{cardId++}", "桃", CardType.Basic));
            }
            
            // 锦囊牌
            AddScrolls("过河拆桥", 6, ref cardId);
            AddScrolls("顺手牵羊", 5, ref cardId);
            AddScrolls("无中生有", 5, ref cardId);
            AddScrolls("决斗", 5, ref cardId);
            AddScrolls("南蛮入侵", 3, ref cardId);
            AddScrolls("万箭齐发", 3, ref cardId);
            AddScrolls("桃园结义", 1, ref cardId);
            AddScrolls("五谷丰登", 2, ref cardId);
            
            // 装备牌
            AddEquipments("青釭剑", 1, ref cardId);
            AddEquipments("雌雄双股剑", 1, ref cardId);
            AddEquipments("寒冰剑", 1, ref cardId);
            AddEquipments("八卦阵", 2, ref cardId);
            AddEquipments("仁王盾", 1, ref cardId);
            AddEquipments("+1 马", 4, ref cardId);
            AddEquipments("-1 马", 3, ref cardId);
            
            Shuffle();
        }
        
        /// <summary>
        /// 添加锦囊牌
        /// </summary>
        private void AddScrolls(string name, int count, ref int cardId)
        {
            for (int i = 0; i < count; i++)
            {
                deck.Add(new Card($"card_{cardId++}", name, CardType.Scroll));
            }
        }
        
        /// <summary>
        /// 添加装备牌
        /// </summary>
        private void AddEquipments(string name, int count, ref int cardId)
        {
            for (int i = 0; i < count; i++)
            {
                deck.Add(new Card($"card_{cardId++}", name, CardType.Equipment));
            }
        }
        
        /// <summary>
        /// 洗牌
        /// </summary>
        public void Shuffle()
        {
            int count = deck.Count;
            for (int i = 0; i< count; i++)
            {
                int randomIndex = Random.Range(i, count);
                Card temp = deck[i];
                deck[i] = deck[randomIndex];
                deck[randomIndex] = temp;
            }
        }
        
        /// <summary>
        /// 抽牌
        /// </summary>
        public List<Card> DrawCards(int count)
        {
            List<Card> drawnCards = new List<Card>();
            
            for (int i = 0; i < count; i++)
            {
                if (deck.Count == 0)
                {
                    // 牌堆为空，将弃牌堆洗入牌堆
                    if (discardPile.Count > 0)
                    {
                        deck.AddRange(discardPile);
                        discardPile.Clear();
                        Shuffle();
                    }
                    else
                    {
                        break; // 没有牌了
                    }
                }
                
                if (deck.Count > 0)
                {
                    drawnCards.Add(deck[0]);
                    deck.RemoveAt(0);
                }
            }
            
            return drawnCards;
        }
        
        /// <summary>
        /// 弃牌
        /// </summary>
        public void Discard(Card card)
        {
            discardPile.Add(card);
        }
        
        /// <summary>
        /// 获取剩余牌数
        /// </summary>
        public int RemainingCards()
        {
            return deck.Count;
        }
    }
}

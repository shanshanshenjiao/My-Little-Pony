using System.Collections.Generic;
using UnityEngine;

namespace Sanguosha.UI
{
    /// <summary>
    /// 卡牌管理器 - 处理手牌 UI
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        public static CardManager Instance { get; private set; }
        
        [Header("手牌容器")]
        public Transform handCardsContainer;
        public GameObject cardPrefab;
        
        [Header("设置")]
        public float cardSpacing = 10f;
        public float cardWidth = 80f;
        
        private List<CardUI> handCards;
        private CardUI selectedCard;
        
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
            
            handCards = new List<CardUI>();
        }
        
        /// <summary>
        /// 渲染手牌
        /// </summary>
        public void RenderHandCards(List<GameCore.Card> cards)
        {
            // 清空现有手牌
            ClearHandCards();
            
            // 创建新卡牌 UI
            foreach (var card in cards)
            {
                AddCardToHand(card);
            }
            
            UpdateCardPositions();
        }
        
        /// <summary>
        /// 添加卡牌到手牌
        /// </summary>
        private void AddCardToHand(GameCore.Card card)
        {
            if (cardPrefab == null || handCardsContainer == null)
            {
                Debug.LogError("卡牌预制体或容器未设置");
                return;
            }
            
            GameObject cardObj = Instantiate(cardPrefab, handCardsContainer);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.Initialize(card);
                handCards.Add(cardUI);
            }
        }
        
        /// <summary>
        /// 清空手牌
        /// </summary>
        private void ClearHandCards()
        {
            foreach (var card in handCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            handCards.Clear();
            selectedCard = null;
        }
        
        /// <summary>
        /// 更新卡牌位置
        /// </summary>
        private void UpdateCardPositions()
        {
            if (handCardsContainer == null) return;
            
            RectTransform containerRect = handCardsContainer as RectTransform;
            if (containerRect == null) return;
            
            float totalWidth = handCards.Count * cardWidth + (handCards.Count - 1) * cardSpacing;
            float startX = -totalWidth / 2 + cardWidth / 2;
            
            for (int i = 0; i < handCards.Count; i++)
            {
                if (handCards[i] == null) continue;
                
                RectTransform cardRect = handCards[i].GetComponent<RectTransform>();
                float x = startX + i * (cardWidth + cardSpacing);
                cardRect.anchoredPosition = new Vector2(x, 0);
            }
        }
        
        /// <summary>
        /// 卡牌被点击
        /// </summary>
        public void OnCardClicked(CardUI clickedCard)
        {
            // 如果点击的是已选中的卡牌，取消选中
            if (selectedCard == clickedCard)
            {
                selectedCard.SetSelected(false);
                selectedCard = null;
            }
            else
            {
                // 取消之前的选中
                if (selectedCard != null)
                {
                    selectedCard.SetSelected(false);
                }
                
                // 选中新卡牌
                selectedCard = clickedCard;
                selectedCard.SetSelected(true);
            }
            
            // 通知游戏管理器
            if (GameCore.GameManager.Instance != null && selectedCard != null)
            {
                Debug.Log($"选中卡牌：{selectedCard.cardData.cardName}");
            }
        }
        
        /// <summary>
        /// 获取选中的卡牌
        /// </summary>
        public GameCore.Card GetSelectedCard()
        {
            return selectedCard?.cardData;
        }
        
        /// <summary>
        /// 清除选中状态
        /// </summary>
        public void ClearSelection()
        {
            if (selectedCard != null)
            {
                selectedCard.SetSelected(false);
                selectedCard = null;
            }
        }
        
        /// <summary>
        /// 移除一张卡牌（出牌后）
        /// </summary>
        public void RemoveCard(GameCore.Card card)
        {
            for (int i = 0; i < handCards.Count; i++)
            {
                if (handCards[i] != null && handCards[i].cardData.cardId == card.cardId)
                {
                    handCards[i].PlayDiscardAnimation();
                    handCards.RemoveAt(i);
                    UpdateCardPositions();
                    break;
                }
            }
        }
    }
}

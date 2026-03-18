using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Sanguosha.UI
{
    /// <summary>
    /// 卡牌 UI 控制器
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI 组件")]
        public Image cardImage;
        public Text cardNameText;
        public Text cardTypeText;
        public Image suitIcon;
        
        [Header("卡牌数据")]
        public GameCore.Card cardData;
        
        [Header("样式")]
        public Color basicCardColor = new Color(0.2f, 0.6f, 1f);
        public Color scrollCardColor = new Color(0.6f, 0.3f, 0.9f);
        public Color equipmentCardColor = new Color(0.9f, 0.6f, 0.2f);
        
        private bool isSelected;
        private RectTransform rectTransform;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        /// <summary>
        /// 初始化卡牌 UI
        /// </summary>
        public void Initialize(GameCore.Card card)
        {
            cardData = card;
            
            if (cardNameText != null)
            {
                cardNameText.text = card.cardName;
            }
            
            if (cardTypeText != null)
            {
                cardTypeText.text = GetCardTypeText(card.cardType);
            }
            
            // 设置边框颜色
            if (cardImage != null)
            {
                switch (card.cardType)
                {
                    case GameCore.CardType.Basic:
                        cardImage.color = basicCardColor;
                        break;
                    case GameCore.CardType.Scroll:
                        cardImage.color = scrollCardColor;
                        break;
                    case GameCore.CardType.Equipment:
                        cardImage.color = equipmentCardColor;
                        break;
                }
            }
        }
        
        /// <summary>
        /// 获取卡牌类型文本
        /// </summary>
        private string GetCardTypeText(GameCore.CardType type)
        {
            switch (type)
            {
                case GameCore.CardType.Basic:
                    return "基本牌";
                case GameCore.CardType.Scroll:
                    return "锦囊";
                case GameCore.CardType.Equipment:
                    return "装备";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (isSelected)
            {
                // 高亮效果
                rectTransform.anchoredPosition += Vector2.up * 20;
            }
            else
            {
                // 恢复原位
                rectTransform.anchoredPosition -= Vector2.up * 20;
            }
        }
        
        /// <summary>
        /// 点击事件
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnCardClicked(this);
            }
        }
        
        /// <summary>
        /// 播放出牌动画
        /// </summary>
        public void PlayDiscardAnimation()
        {
#if LEANTWEEN_INSTALLED
            LeanTween.cancel(gameObject);
            
            // 缩放并淡出
            LeanTween.scale(gameObject, Vector3.zero, 0.3f)
                .setEaseOutBack()
                .setOnComplete(() => {
                    gameObject.SetActive(false);
                });
#else
            // 没有 LeanTween 时使用简单方式
            gameObject.SetActive(false);
#endif
        }
    }
}

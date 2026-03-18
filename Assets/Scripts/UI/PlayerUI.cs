using UnityEngine;
using UnityEngine.UI;

namespace Sanguosha.UI
{
    /// <summary>
    /// 玩家 UI 控制器
    /// </summary>
    public class PlayerUI : MonoBehaviour
    {
        [Header("UI 组件")]
        public Text playerNameText;
        public Text playerIdentityText;
        public Transform hpContainer;
        public GameObject hpBeadPrefab;
        public Text handCardsCountText;
        public Image avatarImage;
        
        [Header("体力珠")]
        public Color fullHpColor = new Color(0.9f, 0.2f, 0.2f);
        public Color emptyHpColor = new Color(0.6f, 0.6f, 0.6f);
        
        private GameCore.Player playerData;
        
        /// <summary>
        /// 初始化玩家 UI
        /// </summary>
        public void Initialize(GameCore.Player player)
        {
            playerData = player;
            
            if (playerNameText != null)
            {
                playerNameText.text = player.username;
            }
            
            if (playerIdentityText != null && player.isLocalPlayer)
            {
                playerIdentityText.text = $"身份：{GetIdentityText(player.identity)}";
            }
            
            UpdateHpDisplay();
            UpdateHandCardsCount();
        }
        
        /// <summary>
        /// 更新体力显示
        /// </summary>
        public void UpdateHpDisplay()
        {
            if (hpContainer == null || hpBeadPrefab == null) return;
            
            // 清空现有体力珠
            foreach (Transform child in hpContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 创建新的体力珠
            for (int i = 0; i < playerData.maxHp; i++)
            {
                GameObject bead = Instantiate(hpBeadPrefab, hpContainer);
                Image beadImage = bead.GetComponent<Image>();
                
                if (beadImage != null)
                {
                    if (i < playerData.currentHp)
                    {
                        beadImage.color = fullHpColor;
                    }
                    else
                    {
                        beadImage.color = emptyHpColor;
                    }
                }
            }
        }
        
        /// <summary>
        /// 更新手牌数显示
        ///</summary>
        public void UpdateHandCardsCount()
        {
            if (handCardsCountText != null)
            {
                handCardsCountText.text = $"手牌 ({playerData.handCards.Count})";
            }
        }
        
        /// <summary>
        /// 获取身份文本
        /// </summary>
        private string GetIdentityText(GameCore.Identity identity)
        {
            switch (identity)
            {
                case GameCore.Identity.Lord:
                    return "主公";
                case GameCore.Identity.Loyalist:
                    return "忠臣";
                case GameCore.Identity.Rebel:
                    return "反贼";
                default:
                    return "未知";
            }
        }
        
        /// <summary>
        /// 设置当前回合高亮
        /// </summary>
        public void SetActiveTurn(bool isActive)
        {
            // TODO: 添加高亮边框或背景效果
            Debug.Log($"玩家 {playerData.username} 的回合：{isActive}");
        }
        
        /// <summary>
        /// 播放受伤动画
        /// </summary>
        public void PlayDamageAnimation()
        {
            // TODO: 实现受伤动画（闪烁、震动等）
            Debug.Log("播放受伤动画");
        }
        
        /// <summary>
        /// 播放死亡动画
        /// </summary>
        public void PlayDeathAnimation()
        {
            // TODO: 实现死亡动画（变灰、倒下等）
            Debug.Log("播放死亡动画");
        }
    }
}

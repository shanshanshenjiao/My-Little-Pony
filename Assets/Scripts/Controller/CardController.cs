
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌控制器（负责“卡牌效果执行”）
/// 属于 Controller 层（逻辑层）
/// 不负责UI，只负责规则
/// </summary>
public class CardController
{
    /// <summary>
    /// 使用一张卡牌
    /// </summary>
    /// <param name="card">要使用的卡牌</param>
    /// <param name="source">出牌者（谁用的）</param>
    /// <param name="target">目标（打谁）</param>
    public void PlayCard(CardModel card, PlayerModel source, PlayerModel target)
    {
        // 根据卡牌类型执行不同逻辑
        switch (card.type)
        {
            case CardType.Attack: // 如果是攻击牌（类似“杀”）

                // 扣目标血量
                target.hp -= card.damage;

                // 打印日志（原代码有乱码，我帮你修正）
                Debug.Log(source.playerName + " 对 " + target.playerName + " 造成 " + card.damage + " 点伤害");

                break;

                // 后续可以扩展更多类型（非常重要）
                // case CardType.Defense:
                // case CardType.Heal:
        }
    }
}
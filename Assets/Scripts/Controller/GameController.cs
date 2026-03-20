using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏主控制器（负责整体流程）
/// 相当于“导演”
/// 控制回合、玩家、流程
/// </summary>
public class GameController : MonoBehaviour
{
    // 游戏数据（所有玩家、回合信息都在这里）
    public GameModel gameModel;

    /// <summary>
    /// 游戏开始
    /// </summary>
    public void StartGame()
    {
        // 初始化玩家
        InitPlayers();

        // 开始第一个回合
        StartTurn();
    }

    /// <summary>
    /// 初始化玩家数据
    /// </summary>
    void InitPlayers()
    {
        // TODO：这里你需要创建玩家（现在是空的）
        // 示例：
        // gameModel.players.Add(new PlayerModel { playerName = "玩家" });
        // gameModel.players.Add(new PlayerModel { playerName = "AI" });
    }

    /// <summary>
    /// 开始当前回合
    /// </summary>
    public void StartTurn()
    {
        // 获取当前回合玩家
        var player = gameModel.CurrentPlayer;

        // 输出当前是谁的回合
        Debug.Log(player.playerName + " 的回合");

        // 这里未来要加：
        // 摸牌阶段
        // 出牌阶段
        // 弃牌阶段
    }

    /// <summary>
    /// 结束回合（切换到下一个玩家）
    /// </summary>
    public void EndTurn()
    {
        // 当前玩家索引 +1，并循环（关键逻辑）
        gameModel.currentPlayerIndex =
            (gameModel.currentPlayerIndex + 1) % gameModel.players.Count;

        // 开始下一回合
        StartTurn();
    }
}
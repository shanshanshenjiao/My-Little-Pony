using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModel
{
    public List<PlayerModel> players = new List<PlayerModel>();
    public int currentPlayerIndex;

    public PlayerModel CurrentPlayer => players[currentPlayerIndex];
}

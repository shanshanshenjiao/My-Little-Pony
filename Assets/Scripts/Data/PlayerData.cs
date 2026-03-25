using System.Collections.Generic;

public class PlayerData
{
    public ulong clientId;
    public List<CardData> cards = new List<CardData>();

    public bool canGuess = true;
}
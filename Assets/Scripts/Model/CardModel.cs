[System.Serializable]
public class CardModel
{
    public int cardId;
    public string cardName;
    public string description;
    public CardType type;

    public int damage;
    public int cost;
}

public enum CardType
{
    Attack,
    Defense,
    Skill
}
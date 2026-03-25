using UnityEngine;
using System.Collections.Generic;

public class DealSystem : MonoBehaviour
{
    public static DealSystem Instance;

    public CardData hiddenCard;

    private void Awake()
    {
        Instance = this;
    }

    public void Deal()
    {
        List<CardData> deck = CreateDeck();

        // 移除1张（犯人）
        hiddenCard = deck[Random.Range(0, deck.Count)];
        deck.Remove(hiddenCard);

        int index = 0;

        foreach (var clientId in Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds)
        {
            PlayerData player = PlayerRepository.Instance.GetPlayer(clientId);

            for (int i = 0; i < 4; i++)
            {
                player.cards.Add(deck[index++]);
            }
        }
    }

    List<CardData> CreateDeck()
    {
        // TODO: 创建13张牌
        return new List<CardData>();
    }
}
using Unity.Netcode;

public class QuerySystem : NetworkBehaviour
{
    public static QuerySystem Instance;

    private void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void QuerySingleServerRpc(ulong targetId, CardFeature feature, ServerRpcParams rpcParams = default)
    {
        ulong asker = rpcParams.Receive.SenderClientId;

        int result = CountFeature(targetId, feature);

        SendQueryResultClientRpc(asker, targetId, feature, result);
    }

    int CountFeature(ulong playerId, CardFeature feature)
    {
        var player = PlayerRepository.Instance.GetPlayer(playerId);

        int count = 0;

        foreach (var card in player.cards)
        {
            if (card.features.Contains(feature))
                count++;
        }

        return count;
    }

    [ClientRpc]
    void SendQueryResultClientRpc(ulong asker, ulong target, CardFeature feature, int result)
    {
        // ?? 后面你接UI和记录板
    }
}
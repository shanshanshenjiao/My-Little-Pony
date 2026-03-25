using Unity.Netcode;

public class GuessSystem : NetworkBehaviour
{
    public static GuessSystem Instance;

    private void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void GuessServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        ulong player = rpcParams.Receive.SenderClientId;

        bool correct = (cardId == DealSystem.Instance.hiddenCard.id);

        SendGuessResultClientRpc(player, correct);
    }

    [ClientRpc]
    void SendGuessResultClientRpc(ulong player, bool correct)
    {
        // UI¥¶¿Ì
    }
}
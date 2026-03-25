using Unity.Netcode;
using System.Collections.Generic;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    private List<ulong> players = new List<ulong>();
    private int currentIndex = 0;

    public ulong CurrentPlayer => players[currentIndex];

    private void Awake()
    {
        Instance = this;
    }

    public void InitTurn()
    {
        players = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        currentIndex = 0;
    }

    public void NextTurn()
    {
        currentIndex = (currentIndex + 1) % players.Count;
    }
}
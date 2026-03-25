using System.Collections.Generic;

public class PlayerRepository
{
    public static PlayerRepository Instance = new PlayerRepository();

    private Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();

    public PlayerData GetPlayer(ulong id)
    {
        return players[id];
    }

    public void AddPlayer(ulong id)
    {
        // 如果已经存在就不重复添加
        if (!players.ContainsKey(id))
        {
            PlayerData player = new PlayerData();
            player.clientId = id;

            players.Add(id, player);
        }
    }

    public IEnumerable<PlayerData> GetAll()
    {
        return players.Values;
    }
}
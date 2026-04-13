using Fusion;
using UnityEngine;
public struct PlayerMetaData : INetworkStruct
{
    public NetworkString<_16> playerName; 
    public PlayerClass playerClass;
    public int level;
    public int Exp;
    public int health;
    public int heath;
    public int MaxHealth => 100 + (level * 20);
    public int mana;
    public int MaxMana => 100 + (level * 50);
}

public class PlayerDataManager : NetworkBehaviour
{
    [Networked, Capacity(16)]
    public NetworkDictionary<PlayerRef, PlayerMetaData> Player => default;
    [Rpc(sources: RpcSources.All , targets: RpcTargets.StateAuthority)]
    public void RPC_UpdatePlayerMetaData(PlayerRef player, PlayerMetaData metaData)
    {
       Player.Set(player, metaData);
    }

    public bool TryGetPlayerMetaData(PlayerRef player, out PlayerMetaData metaData)
    {
        return Player.TryGet(player, out metaData);
    }
}

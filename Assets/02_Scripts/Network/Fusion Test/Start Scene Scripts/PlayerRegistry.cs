using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRegistry : NetworkBehaviour
{
    [Networked, Capacity(2)]
    public NetworkDictionary<PlayerRef, PlayerPosition> RefToPosition { get; } // PlayerRef로 역할군 찾기

    public bool IsPlayerBuilder(PlayerRef playerRef) => RefToPosition[playerRef] == PlayerPosition.Builder;
    public bool IsPlayerRunneer(PlayerRef playerRef) => RefToPosition[playerRef] == PlayerPosition.Runner;

    // 플레이어 딕셔너리에 추가
    public void AddPlayer(PlayerRef player, PlayerPosition position)
    {
        RefToPosition.Add(player, position);
    }

    // 플레이어 딕셔너리에서 제거
    public void RemovePlayer(PlayerRef player)
    {
        if (RefToPosition.ContainsKey(player))
        {
            RefToPosition.Remove(player);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ChangeRole(PlayerRef player)
    {
        if (HasStateAuthority && RefToPosition.ContainsKey(player))
        {
            var currentRole = RefToPosition.Get(player);
            var newRole = currentRole == PlayerPosition.Builder ? PlayerPosition.Runner : PlayerPosition.Builder;
            RefToPosition.Set(player, newRole);
        }
    }

    public PlayerRef GetPlayerRefFromPosition(PlayerPosition position)
    {
        foreach(var player in RefToPosition)
        {
            var playerPosition = player.Value;
            if(position == playerPosition)
            {
                return player.Key;
            }
        }

        return PlayerRef.None;
    }
}

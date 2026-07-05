using System.Collections.Generic;
using Fusion;
using UnityEngine;

public sealed class PlayerRunnerSlashContactDamageHandler
{
    private TickTimer _damageTimer;

    public void Tick(
        PlayerRunner runner,
        SlashContactDetector slashContactDetector,
        TerritorySystem territorySystem,
        float damageInterval,
        float playerContactDamage,
        float fallbackRadius,
        LayerMask monsterLayerMask,
        bool canDamageRunner)
    {
        if (!canDamageRunner || runner == null || !runner.HasStateAuthority || slashContactDetector == null)
            return;

        if (!_damageTimer.ExpiredOrNotRunning(runner.Runner))
            return;

        IReadOnlyCollection<WorldMonster> monsters = slashContactDetector.DetectWorldMonsters(
            territorySystem,
            fallbackRadius,
            monsterLayerMask);

        foreach (WorldMonster monster in monsters)
        {
            runner.TakeSlashDamage(playerContactDamage);
            Debug.Log(
                $"{nameof(PlayerRunnerSlashContactDamageHandler)} detected slash contact with {monster.name}. " +
                $"Player took {playerContactDamage:F2} damage. HP: {runner.Health:F2}/{runner.MaxHealth:F2}",
                monster);
        }

        _damageTimer = TickTimer.CreateFromSeconds(runner.Runner, Mathf.Max(0.01f, damageInterval));
    }
}

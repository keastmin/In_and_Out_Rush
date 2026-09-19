using System;
using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public enum RunnerLaboratoryUpgradeType
    {
        Health,
        MoveSpeed,
        Stamina,
        StaminaRecovery,
        Weapon
    }

    public readonly struct RunnerLaboratoryUpgradeRequest
    {
        public RunnerLaboratoryUpgradeRequest(
            RunnerLaboratoryUpgradeType type,
            int nextLevel,
            float amount,
            Cost cost)
        {
            Type = type;
            NextLevel = nextLevel;
            Amount = amount;
            Cost = cost;
        }

        public RunnerLaboratoryUpgradeType Type { get; }
        public int NextLevel { get; }
        public float Amount { get; }
        public Cost Cost { get; }
    }

    public interface IRunnerLaboratoryUpgradeReceiver
    {
        bool TryRequestLaboratoryUpgrade(RunnerLaboratoryUpgradeRequest request);
    }

    public class LaboratoryUI : MonoBehaviour
    {
        [SerializeField] private TowerUpgradeUI _towerUpgradeUI;
        [SerializeField] private RunnerUpgradeUI _runnerUpgradeUI;
        [SerializeField] private RunnerSupplyUI _runnerSupplyUI;

        public void InitializeLaboratoryUI(TowerUpgradeManager towerUpgradeManager)
        {
            _towerUpgradeUI?.InitializeTowerUpgradeUI(towerUpgradeManager);
            _runnerUpgradeUI?.InitializeRunnerUpgradeUI();
            _runnerSupplyUI?.InitializeRunnerSupplyUI();
        }

        public void InjectionRunnerReference(PlayerRunner runner) => _runnerUpgradeUI?.InjectionRunnerReference(runner);
        public void OnClickRunnerHPUpButton() => _runnerUpgradeUI?.OnClickRunnerHPUpButton();
        public void OnClickRunnerSpeedUpButton() => _runnerUpgradeUI?.OnClickRunnerSpeedUpButton();
        public void OnClickRunnerStaminaUpButton() => _runnerUpgradeUI?.OnClickRunnerStaminaUpButton();
        public void OnClickRunnerStaminaRecoveryUpButton() => _runnerUpgradeUI?.OnClickRunnerStaminaRecoveryUpButton();
        public void OnClickRunnerWeaponUpButton() => _runnerUpgradeUI?.OnClickRunnerWeaponUpButton();
        public void OnClickSkillSupplyButton() => _runnerSupplyUI?.OnClickSkillSupplyButton();
        public void OnClickWeaponSupplyButton() => _runnerSupplyUI?.OnClickWeaponSupplyButton();
        public void OnClickItemSupplyButton() => _runnerSupplyUI?.OnClickItemSupplyButton();
    }
}

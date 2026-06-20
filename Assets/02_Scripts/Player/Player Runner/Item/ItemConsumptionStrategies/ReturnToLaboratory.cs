using UnityEngine;

public class ReturnToLaboratory : IItemConsumptionStrategy
{
    public RunnerItemType ItemType => RunnerItemType.Lifeline;

    public bool TryUse(RunnerItemUseContext context)
    {
        PlayerRunner playerRunner = context.User;
        if (playerRunner == null)
        {
            Debug.LogError("ReturnToLaboratory strategy requires a Transform parameter.");
            return false;
        }
        var playerRunnerTransform = playerRunner.transform;

        var targetPosition = Vector3.zero; // 연구소 위치
        var angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        var x = Mathf.Cos(angle) * 2f;
        var z = Mathf.Sin(angle) * 2f;
        var randomOffset = new Vector3(x, playerRunnerTransform.position.y, z);
        playerRunnerTransform.position = targetPosition + randomOffset;
        return true;
    }
}

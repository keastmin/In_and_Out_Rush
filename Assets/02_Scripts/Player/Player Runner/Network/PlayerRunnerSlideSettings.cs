using UnityEngine;

[CreateAssetMenu(fileName = "PlayerRunnerSlideSettings", menuName = "Scriptable Objects/Player Runner Slide Settings")]
public class PlayerRunnerSlideSettings : ScriptableObject
{
    [SerializeField, Min(0f)] private float _staminaCost = 10f;

    public float StaminaCost => Mathf.Max(0f, _staminaCost);
}

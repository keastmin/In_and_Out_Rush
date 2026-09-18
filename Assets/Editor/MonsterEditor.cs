using Fusion.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Monster), true)]
[CanEditMultipleObjects]
public sealed class MonsterEditor : NetworkBehaviourEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("실시간 체력 (읽기 전용)", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 중 생성된 몬스터를 선택하면 현재 체력을 확인할 수 있습니다.", MessageType.Info);
            return;
        }

        foreach (Object selected in targets)
        {
            var monster = selected as Monster;
            if (monster == null)
                continue;

            if (targets.Length > 1)
                EditorGUILayout.LabelField(monster.name);

            if (!monster.TryGetHealthSnapshot(out float currentHealth, out float maximumHealth))
            {
                EditorGUILayout.HelpBox("네트워크 생성 대기 중이거나 비활성화된 몬스터입니다.", MessageType.Info);
                continue;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("현재 체력", currentHealth);
                EditorGUILayout.FloatField("최대 체력", maximumHealth);
            }
        }
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}

using UnityEngine;

public class FogOfWarSystem : MonoBehaviour
{
    [Tooltip("전장의 안개가 적용될 카메라")]
    [SerializeField] private Camera _renderCamera;
    [Tooltip("전장의 안개에 가려져도 살짝 보이게 될 오브젝트들의 레이어")]
    [SerializeField] private LayerMask _fogVisibleLayers;
    [Tooltip("전장의 안개에 가려지면 완전히 모습을 감출 오브젝트들의 레이어")]
    [SerializeField] private LayerMask _fogHiddenLayers;
    [Tooltip("러너의 시야를 표현할 브러쉬 텍스쳐")]
    [SerializeField] private Texture2D _runnerVisionBrush;
    [Tooltip("플레이어 러너의 시야 범위")]
    [SerializeField] private float _playerRunnerVisibleRange = 5f;
    [Tooltip("안개의 밀도: 안개로 가려진 부분이 얼마나 어둡게 보일지 정하는 수치")]
    [SerializeField][Range(0f, 1f)] private float _fogDensity = 0.75f;
    [Tooltip("영역 오브젝트의 메쉬 필터")]
    [SerializeField] private MeshFilter _territoryMeshFilter;
    [Tooltip("영역 오브젝트의 메쉬 렌더러")]
    [SerializeField] private MeshRenderer _territoryMeshRenderer;
    [Tooltip("영역 오브젝트 모양에서 어느 정도 더 넓게 보일 것인지 정하는 수치")]
    [SerializeField][Min(0)] private float _territoryVisibleRange = 4f;

    private PlayerRunner _playerRunner;

    public void InitializeFogOfWarSystem(PlayerRunner playerRunner)
    {
        _playerRunner = playerRunner;
    }
}
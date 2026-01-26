using Fusion;
using System.Collections.Generic;
using UnityEngine;

public sealed class AmplificationTower : SupportTower, ICanDragObject
{
    [SerializeField] private AmplificationTowerBuffParam _buffParam = new();
    [SerializeField] private LayerMask _detectLayers;
    private HashSet<IBuffReceiver> _receivers;
    private Collider[] _detectedColliders;
    private IBuffReceiver[] _detectedReceivers;
    private const int _arraySize = 100;

    private List<IBuffReceiver> _removeCandidates;

    protected override void TowerAwake()
    {
        // 초기화
        _receivers = new HashSet<IBuffReceiver>();
        _detectedColliders = new Collider[_arraySize];
        _detectedReceivers = new IBuffReceiver[_arraySize];
        _removeCandidates = new List<IBuffReceiver>();
    }

    protected override void TowerFixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // 배열 초기화
            for (int i = 0; i < _arraySize; i++)
            {
                _detectedColliders[i] = null;
                _detectedReceivers[i] = null;
            }

            // Collider 객체들 감지
            int detectCount = Physics.OverlapSphereNonAlloc(transform.position, _buffRange / 2, _detectedColliders, _detectLayers);

            // IBuffReceiver 구분
            int receiverCount = 0;
            for(int i = 0; i < detectCount; i++)
            {
                if (_detectedColliders[i].TryGetComponent(out IBuffReceiver receiver))
                {
                    _detectedReceivers[receiverCount++] = receiver;
                }
            }

            // 새로 들어온 객체 감지 -> Enter 인터페이스 호출 후 해시에 추가
            for(int i = 0; i < receiverCount; i++)
            {
                if (!_receivers.Contains(_detectedReceivers[i]))
                {
                    _detectedReceivers[i].BuffEnter(_buffParam);
                    _receivers.Add(_detectedReceivers[i]);
                }
            }

            // 들어왔다가 나간 객체 감지 -> Exit 인터페이스 호출
            foreach(var receiver in _receivers)
            {
                bool isExit = true;
                for(int i = 0; i < receiverCount; i++)
                {
                    if (receiver == _detectedReceivers[i])
                    {
                        isExit = false;
                    }
                }

                if (isExit)
                {
                    receiver.BuffExit(_buffParam);
                    _removeCandidates.Add(receiver);
                }
            }

            // 나간 객체 해시에서 제거
            foreach(var receiver in _removeCandidates)
            {
                _receivers.Remove(receiver);
            }
            _removeCandidates.Clear();
        }
    }

    #region ICanDragObject 구현부
    public void OnDragSelectedThisObject()
    {
        OnLeftMouseDownThisObject();
    }

    public void OnDragOverThisObject()
    {
        OnCancelClickThisObject();
    }

    public void OnDragCompleteThisObject()
    {
        var manager = StageManager.Instance;
        if (manager != null)
        {
            // 빌더의 타워 선택을 함수를 호출하여 자신을 선택된 타워로 넘겨줌
            manager.PlayerBuilder.TowerSelected(this);
        }
    }
    #endregion
}

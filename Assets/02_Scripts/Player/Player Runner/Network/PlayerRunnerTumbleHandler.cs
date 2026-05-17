using DG.Tweening;
using UnityEngine;

public class PlayerRunnerTumbleHandler
{
    private bool _isTumbling;

    public bool IsTumbling => _isTumbling;

    public void StartTumble(PlayerRunner runner, Rigidbody rigidbody)
    {
        if (_isTumbling) return;

        _isTumbling = true;
        rigidbody.linearVelocity = runner.EffectiveMovementSpeed * runner.transform.forward;

        Transform rootTransform = runner.transform.Find("Root");
        rootTransform.DOLocalMoveY(2f, 0.5f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad);
        rootTransform.DOLocalRotate(new Vector3(360f, 0f, 0f), 1f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear);

        DOTween.Sequence()
            .AppendInterval(1.0f)
            .AppendCallback(() => _isTumbling = false);
    }
}

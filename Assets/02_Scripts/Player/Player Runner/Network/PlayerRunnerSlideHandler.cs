using System.Threading.Tasks;
using UnityEngine;

public class PlayerRunnerSlideHandler
{
    private bool _isSliding;

    public bool IsSliding => _isSliding;

    public async Task StartSlide(
        PlayerRunner runner,
        Rigidbody rigidbody,
        PlayerRunnerSwiftnessHandler swiftnessHandler)
    {
        if (_isSliding) return;

        Debug.Log("슬라이드 시작");
        _isSliding = true;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.AddForce(3f * runner.MovementSpeed * runner.transform.forward, ForceMode.Impulse);

        float originalLinearDamping = rigidbody.linearDamping;
        rigidbody.linearDamping = 2f;

        if (swiftnessHandler.IsSwiftnessActive && swiftnessHandler.RemainingSlideCount > 0)
        {
            swiftnessHandler.DecrementRemainingSlideCount();
            Debug.Log($"스위프트니스 슬라이드 남음: {swiftnessHandler.RemainingSlideCount}회");
        }
        else
        {
            runner.Stamina -= 10f;
            StageManager.Instance.UIController.RunnerUI.Display.Player
                .SetStaminaBarRatio(runner.Stamina / 100f);
        }

        await Task.Delay(1000);
        rigidbody.linearDamping = originalLinearDamping;
        _isSliding = false;
        Debug.Log("슬라이드 종료");
    }
}

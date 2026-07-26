using System.Threading.Tasks;
using Dev.Network;
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

        bool canUseSwiftnessSlide =
            swiftnessHandler.IsSwiftnessActive &&
            swiftnessHandler.RemainingSlideCount > 0;

        if (!canUseSwiftnessSlide && runner.Stamina < runner.SlideStaminaCost) return;

        Debug.Log("슬라이드 시작");
        _isSliding = true;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.AddForce(3f * runner.EffectiveMovementSpeed * runner.transform.forward, ForceMode.Impulse);

        float originalLinearDamping = rigidbody.linearDamping;
        rigidbody.linearDamping = 2f;

        if (canUseSwiftnessSlide)
        {
            swiftnessHandler.DecrementRemainingSlideCount();
            Debug.Log($"스위프트니스 슬라이드 남음: {swiftnessHandler.RemainingSlideCount}회");
        }
        else
        {
            runner.Stamina = Mathf.Max(0f, runner.Stamina - runner.SlideStaminaCost);
            runner.OnStaminaChanged();
        }

        await Task.Delay(1000);
        rigidbody.linearDamping = originalLinearDamping;
        _isSliding = false;
        Debug.Log("슬라이드 종료");
    }
}

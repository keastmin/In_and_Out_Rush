using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] Image frameImage;
    [SerializeField] Image backgroundImage;
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] Color activeBackgroundColor = Color.red;
    [SerializeField] Color inactiveBackgroundColor = Color.white;

    public void SetActiveSlot(bool isActive)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isActive ? activeBackgroundColor : inactiveBackgroundColor;
            return;
        }

        if (frameImage != null)
            frameImage.color = isActive ? activeBackgroundColor : inactiveBackgroundColor;
    }

    public void SetItem(RunnerItemType itemType, int count)
    {
        if (iconImage != null)
            iconImage.enabled = itemType != RunnerItemType.None;

        if (countText != null)
            countText.text = count > 0 ? count.ToString() : string.Empty;
    }
}

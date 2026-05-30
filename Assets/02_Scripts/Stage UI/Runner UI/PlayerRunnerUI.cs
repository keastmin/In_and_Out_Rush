using UnityEngine;

public class PlayerRunnerUI : MonoBehaviour
{
    public RunnerDisplay Display;
    public RunnerPopup Popup;

    [SerializeField] ItemSlotUI[] itemSlotViews;

    int selectedItemSlotIndex;

    private void Start()
    {
        SelectItemSlot(selectedItemSlotIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectItemSlot(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectItemSlot(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectItemSlot(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectItemSlot(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SelectItemSlot(4);
        }
    }

    public void SelectItemSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= itemSlotViews.Length)
            return;

        for (int i = 0; i < itemSlotViews.Length; i++)
        {
            itemSlotViews[i].SetActiveSlot(i == slotIndex);
        }

        selectedItemSlotIndex = slotIndex;
    }

    public void SetItemSlot(int slotIndex, RunnerItemType itemType, int count)
    {
        if (slotIndex < 0 || slotIndex >= itemSlotViews.Length)
            return;

        itemSlotViews[slotIndex].SetItem(itemType, count);
    }
}

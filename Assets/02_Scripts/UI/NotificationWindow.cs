using TMPro;
using UnityEngine;

public class NotificationWindow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _descriptText;

    public void SetDescript(string descript)
    {
        _descriptText.text = descript;
    }

    public void OnClickOKButton()
    {
        Destroy(this.gameObject);
    }
}

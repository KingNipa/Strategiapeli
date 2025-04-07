using UnityEngine;
using UnityEngine.EventSystems;

public class PanelPointerHandler : MonoBehaviour, IPointerUpHandler
{
    public void OnPointerUp(PointerEventData eventData)
    {
        // Tarkistetaan, ett� kyseess� on oikea painike
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            MinimalNationInfoPanel.Instance.Hide();
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class BlockClickPropagation : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        eventData.Use(); // T‰m‰ est‰‰ eventin etenemisen heti painalluksen alussa
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        eventData.Use(); // Varmistetaan viel‰ klikkauksen j‰lkeen
    }

}
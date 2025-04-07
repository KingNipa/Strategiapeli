using UnityEngine;
using UnityEngine.UI;

public class DiplomacyUI : MonoBehaviour
{
    public Text relationshipText; // Viite UI-tekstielementtiin
    public Nation targetNation;   // Esimerkiksi tarkkailtava valtio

    void Update()
    {
        if (DiplomacyManager.Instance != null && targetNation != null)
        {
            // Oletetaan, että GameManagerissa on pelaajan valtio
            int relScore = DiplomacyManager.Instance.GetRelationship(GameManager.Instance.playerNation, targetNation);
            relationshipText.text = $"{targetNation.Name} suhde: {relScore}";
        }
    }
}

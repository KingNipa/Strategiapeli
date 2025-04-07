using UnityEngine;
using UnityEngine.UI;

public class MinimalNationInfoPanel : MonoBehaviour
{
    public static MinimalNationInfoPanel Instance;

    public Text nationNameText;
    public Text militaryPowerText;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // P‰ivitt‰‰ ja n‰ytt‰‰ minimipaneelin
    public void ShowMinimalInfo(Nation nation)
    {
        if (nation == null) return;
        nationNameText.text = nation.Name;
        militaryPowerText.text = "Power: " + nation.MilitaryPower.ToString("F0");
        gameObject.SetActive(true);
    }

    


    // Piilottaa paneelin
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

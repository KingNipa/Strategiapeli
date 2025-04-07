using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AttackResultPanel : MonoBehaviour
{
    public static AttackResultPanel Instance;

    [Header("UI Viitteet")]
    public GameObject panel;
    public TextMeshProUGUI attackerPowerText;
    public TextMeshProUGUI defenderPowerText;
    public TextMeshProUGUI attackerLossesText;
    public TextMeshProUGUI defenderLossesText;
    public TextMeshProUGUI capturedTilesText;
    public Button continueButton;

    public TextMeshProUGUI attackerNameText;
    public TextMeshProUGUI defenderNameText;

    public TextMeshProUGUI defenderAllianceBonusText;

    // voidaan seurata, onko paneeli suljettu
    public bool IsClosed { get; private set; } = true;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    /// <summary>
    /// Näyttää hyökkäystulokset.
    /// </summary>
    /// <param name="attackerPower">Hyökkääjän käytetty voima</param>
    /// <param name="defenderPower">Puolustajan voima</param>
    /// <param name="attackerLosses">Hyökkääjän tappiot</param>
    /// <param name="defenderLosses">Puolustajan tappiot</param>
    /// <param name="capturedTiles">Vallattujen ruutujen määrä</param>
    public void Show(float attackerPower, float defenderPower, float allianceBonus, int attackerLosses, int defenderLosses, int capturedTiles, string attackerName, string defenderName)
    {
        attackerNameText.text = "Attacker: " + attackerName;
        defenderNameText.text = "Defender: " + defenderName;
        attackerPowerText.text = "Attacker's Power: " + attackerPower.ToString("F1");
        defenderPowerText.text = "Defender's Power: " + defenderPower.ToString("F1");

        defenderAllianceBonusText.text = "Alliance Bonus: " + allianceBonus.ToString("F1");

        attackerLossesText.text = "Losses of the Attacking army: " + attackerLosses;
        defenderLossesText.text = "Losses of the Defending army: " + defenderLosses;
        capturedTilesText.text = "Captured Tiles: " + capturedTiles;

        panel.SetActive(true);
        IsClosed = false;

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinue);
    }

    private void OnContinue()
    {
        panel.SetActive(false);
        IsClosed = true;
    }
}

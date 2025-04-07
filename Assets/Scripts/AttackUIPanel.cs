using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
public class AttackUIPanel : MonoBehaviour
{
    public static AttackUIPanel Instance;

    [Header("UI Viitteet")]
    public GameObject panel;
    public TextMeshProUGUI attackText;
    public Slider attackSlider;
    public Button confirmButton;
    public Button cancelButton;

    // Tallennetaan hyökkäyksen kohde-ruutu sekä osapuolten viitteet
    private SquareTile targetTile;
    private Nation playerNation;
    private Nation enemyNation;
    public TextMeshProUGUI attackAmountText;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    /// <summary>
    /// Näyttää hyökkäyspaneelin.
    /// </summary>
    /// <param name="targetTile">Vihollisruutu, josta hyökkäys laukaistaan</param>
    /// <param name="playerNation">Pelaajan valtakunta (hyökkääjä)</param>
    /// <param name="enemyNation">Vihollisen valtakunta (puolustaja)</param>
    public void Show(SquareTile targetTile, Nation playerNation, Nation enemyNation)
    {
        // Estetään hyökkäys, jos hyökkääjä ja puolustaja ovat sama
        if (playerNation == enemyNation)
        {
            //Debug.LogWarning("Pelaaja ei voi hyökätä omaan valtakuntaansa!");
            return;
        }

        this.targetTile = targetTile;
        this.playerNation = playerNation;
        this.enemyNation = enemyNation;
        attackText.text = "";
        attackSlider.value = 100f; // Oletuksena täysis eli 100%
        UpdateAttackAmountText(attackSlider.value); // Päivitetään tekstikenttä aluks
        panel.SetActive(true);

       
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirm);

        cancelButton.onClick.RemoveAllListeners();       
        cancelButton.onClick.AddListener(Hide);

        // Varmistetaan (tarvittaessa ensin RemoveListener)
        attackSlider.onValueChanged.RemoveAllListeners();
        attackSlider.onValueChanged.AddListener(UpdateAttackAmountText);
    }

    private void UpdateAttackAmountText(float sliderValue)
    {
        // Lasketaan, kuinka monta yksikköä armeijasta vastaa sliderin arvoa.
        int totalArmy = playerNation.Military;
        int unitsToUse = Mathf.RoundToInt(totalArmy * (sliderValue / 100f));
        attackAmountText.text = unitsToUse.ToString();
    }

   // Lisää tiedoston yläosaan, jos ei jo ole

void OnConfirm()
{
        StartCoroutine(ExecuteAttackAndRefresh());
        Hide();
}

private IEnumerator ExecuteAttackAndRefresh()
{
    // Odotetaan, että ExecuteAttackCoroutine suorittaa hyökkäyksen loppuun
    yield return CombatManager.Instance.ExecuteAttackCoroutine(playerNation, enemyNation, attackSlider.value);
    // Päivitetään UI heti hyökkäyksen jälkeen
    GameManager.Instance.UpdateUI();
}


public void Hide()
    {
        panel.SetActive(false);
    }

}

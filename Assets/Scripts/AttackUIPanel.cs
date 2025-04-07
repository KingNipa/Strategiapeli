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

    // Tallennetaan hy�kk�yksen kohde-ruutu sek� osapuolten viitteet
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
    /// N�ytt�� hy�kk�yspaneelin.
    /// </summary>
    /// <param name="targetTile">Vihollisruutu, josta hy�kk�ys laukaistaan</param>
    /// <param name="playerNation">Pelaajan valtakunta (hy�kk��j�)</param>
    /// <param name="enemyNation">Vihollisen valtakunta (puolustaja)</param>
    public void Show(SquareTile targetTile, Nation playerNation, Nation enemyNation)
    {
        // Estet��n hy�kk�ys, jos hy�kk��j� ja puolustaja ovat sama
        if (playerNation == enemyNation)
        {
            //Debug.LogWarning("Pelaaja ei voi hy�k�t� omaan valtakuntaansa!");
            return;
        }

        this.targetTile = targetTile;
        this.playerNation = playerNation;
        this.enemyNation = enemyNation;
        attackText.text = "";
        attackSlider.value = 100f; // Oletuksena t�ysis eli 100%
        UpdateAttackAmountText(attackSlider.value); // P�ivitet��n tekstikentt� aluks
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
        // Lasketaan, kuinka monta yksikk�� armeijasta vastaa sliderin arvoa.
        int totalArmy = playerNation.Military;
        int unitsToUse = Mathf.RoundToInt(totalArmy * (sliderValue / 100f));
        attackAmountText.text = unitsToUse.ToString();
    }

   // Lis�� tiedoston yl�osaan, jos ei jo ole

void OnConfirm()
{
        StartCoroutine(ExecuteAttackAndRefresh());
        Hide();
}

private IEnumerator ExecuteAttackAndRefresh()
{
    // Odotetaan, ett� ExecuteAttackCoroutine suorittaa hy�kk�yksen loppuun
    yield return CombatManager.Instance.ExecuteAttackCoroutine(playerNation, enemyNation, attackSlider.value);
    // P�ivitet��n UI heti hy�kk�yksen j�lkeen
    GameManager.Instance.UpdateUI();
}


public void Hide()
    {
        panel.SetActive(false);
    }

}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class NationInfoPanel : MonoBehaviour
{
    // Singleton-esimerkki
    public static NationInfoPanel Instance;

    // UI-komponenttien viitteet:
    public Text nationNameText;
    public Text nationPopulationText;
    public Text controlledTilesText;
    public Text nationMilitaryPowerText;
    public Text nationGDPText;
    public Text nationMilitaryText;
    public Text nationMoraleText;
    public Text relationshipText;
    public Text nationTechnologyText;
    public Text nationIncomeText;
    public Text allianceText;

    public GameObject panel;
    public Button warButton;
    public Button tradeButton;
    public Button allianceButton;  // Uusi painike liittoehdotuksille


    void Awake()
    {
        // Asetetaan singleton-instanssi (varmistetaan, ettei useampaa instanssia synny)
        if (Instance == null)
        {
            Instance = this;
            panel.SetActive(false);  // Piilotetaan paneeli aluksi
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowNationInfo(Nation nation, SquareTile enemyTile)
    {
        currentNation = nation;
        currentEnemyTile = enemyTile;

        // N‰ytet‰‰n perustiedot
        nationNameText.text = "Nation: " + nation.Name;
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        int tileCount = grid != null
            ? grid.GetControlledTiles().FindAll(t => t.controllingNation == nation).Count
            : 0;
        controlledTilesText.text = "Controlled Tiles: " + tileCount;
        nationPopulationText.text = "Population: " + (nation.Population / 1000000f).ToString("F1") + " million";
        nationGDPText.text = "Currency: " + nation.GDP.ToString("F1");
        nationMilitaryText.text = "Army Size: " + nation.Military;
        nationMilitaryPowerText.text = "Power: " + nation.MilitaryPower.ToString("F0");
        nationMoraleText.text = "Morale: " + nation.Morale.ToString("F0");
        relationshipText.text = "Relationship: " + DiplomacyManager.Instance.GetRelationship(GameManager.Instance.playerNation, nation);

        float tileIncome = nation.CalculateTileIncome(grid);
        float populationIncome = nation.Population / 150000f;
        float preMilitaryIncome = tileIncome + populationIncome + nation.IncomeBonusPerTurn;
        nationIncomeText.text = "Income: " + preMilitaryIncome.ToString("F1");

        string techEra = nation.Technology switch
        {
            0 => "Primitive Technological Era",
            1 => "Early Technological Era",
            2 => "Medieval Technology",
            _ => "Advanced Technology"
        };
        nationTechnologyText.text = "Technology: " + techEra;

        Alliance alliance = DiplomacyManager.Instance.GetAllianceForNation(nation);
        allianceText.text = alliance != null ? "Alliance: " + alliance.Name : "";

        panel.SetActive(true);
        Nation playerNation = GameManager.Instance.playerNation;

        if (nation == playerNation)
        {
            warButton.gameObject.SetActive(false);
            tradeButton.gameObject.SetActive(false);
            relationshipText.gameObject.SetActive(false);

            if (alliance != null)
            {
                allianceButton.gameObject.SetActive(true);
                var btnText = allianceButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "Leave Allies";

                allianceButton.onClick.RemoveAllListeners();
                allianceButton.onClick.AddListener(() =>
                {
                    alliance.RemoveMember(playerNation);
                    playerNation.allianceJoinCooldown = 10;
                    GameManager.Instance.ShowNotificationWindow($"{playerNation.Name} has left the alliance {alliance.Name}.");
                    Hide();
                });
            }
            else allianceButton.gameObject.SetActive(false);
        }
        else
        {
            relationshipText.text = "Relationship: " + DiplomacyManager.Instance.GetRelationship(playerNation, nation);
            relationshipText.gameObject.SetActive(true);

            var allianceBtnText = allianceButton.GetComponentInChildren<Text>();
            if (allianceBtnText != null) allianceBtnText.text = "Alliance";

            Alliance playerAlliance = DiplomacyManager.Instance.GetAllianceForNation(playerNation);
            Alliance targetAlliance = DiplomacyManager.Instance.GetAllianceForNation(nation);
            bool sameAlliance = playerAlliance != null && playerAlliance == targetAlliance;
            bool cannotAttack = playerNation.AttacksThisTurn >= 2 || playerNation.Morale < 40f;
            bool isNeighbor = grid.GetControlledTiles()
                .Any(t => t.controllingNation == nation && grid.GetNeighbors(t).Any(n => n.controllingNation == playerNation));

            if (!sameAlliance && !cannotAttack && isNeighbor)
            {
                warButton.gameObject.SetActive(true);
                warButton.onClick.RemoveAllListeners();
                warButton.onClick.AddListener(() =>
                {
                    Hide();
                    AttackUIPanel.Instance.Show(enemyTile, playerNation, nation);
                });
            }
            else warButton.gameObject.SetActive(false);

            tradeButton.gameObject.SetActive(isNeighbor && TradeManager.IsTradePossible(nation, playerNation));
            if (tradeButton.gameObject.activeSelf)
            {
                tradeButton.onClick.RemoveAllListeners();
                tradeButton.onClick.AddListener(() =>
                {
                    var tradePanel = Resources.FindObjectsOfTypeAll<TradePanel>().FirstOrDefault();
                    if (tradePanel != null)
                    {
                        tradePanel.gameObject.SetActive(true);
                        tradePanel.InitializeTrade(nation, playerNation);
                    }
                });
            }

            bool targetHasBeenSeen = grid.GetControlledTiles()
                .Any(t => t.controllingNation == nation && t.HasBeenExplored);
            int targetRelScore = DiplomacyManager.Instance.GetRelationship(playerNation, nation);

            if (targetHasBeenSeen && targetRelScore >= DiplomacyManager.Instance.allianceThreshold && !DiplomacyManager.Instance.IsInAlliance(nation))
            {
                allianceButton.gameObject.SetActive(true);
                allianceButton.onClick.RemoveAllListeners();
                allianceButton.onClick.AddListener(() =>
                {
                    bool accepted = DiplomacyManager.Instance.ProposeAlliance(playerNation, nation);
                    if (accepted)
                    {
                        GameManager.Instance.allianceConfirmationPanel.ShowResult($"The state {nation.Name} accepted your alliance request.");
                        ShowNationInfo(nation, enemyTile);
                    }
                    else
                    {
                        GameManager.Instance.allianceConfirmationPanel.ShowResult($"The state {nation.Name} rejected your alliance request.");
                    }
                });
            }
            else allianceButton.gameObject.SetActive(false);
        }
    } 




    // Metodi paneelin sulkemiseksi
    public void Hide()
    {
        panel.SetActive(false);
    }

    private Nation currentNation;
    private SquareTile currentEnemyTile;


    /// <summary>
    /// P‰ivitt‰‰ panelin tiedot uudelleen nykyiselle valtiolle, jos paneeli on auki.
    /// </summary>
    public void Refresh()
    {
        if (panel.activeSelf && currentNation != null)
            ShowNationInfo(currentNation, currentEnemyTile);
    }
}




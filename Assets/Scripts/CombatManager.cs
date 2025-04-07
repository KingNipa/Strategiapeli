using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class CombatManager : MonoBehaviour
{
    public SquareGrid grid; // Viite ruudukkoon, josta haetaan ruudut
    public static CombatManager Instance;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Suorittaa hyökkäyksen.
    /// </summary>
    /// <param name="attacker">Hyökkääjän valtakunta (esim. pelaaja)</param>
    /// <param name="defender">Puolustajan valtakunta</param>
    /// <param name="attackPercentage">Hyökkäykseen käytettävä prosenttiosuus hyökkääjän sotilaallisesta powersta (0–100)</param>
    public IEnumerator ExecuteAttackCoroutine(Nation attacker, Nation defender, float attackPercentage)
    {
        if (attacker == GameManager.Instance.playerNation)
        {
            if (attacker.AttacksThisTurn >= 2)
            {
                //Debug.Log("Pelaaja on jo suorittanut maksimimäärän hyökkäyksiä tältä vuorolta (2).");
                yield break; // Lopetetaan hyökkäys, koska vuoron raja on saavutettu.
            }
            // Kasvatetaan hyökkäysten laskuria
            attacker.AttacksThisTurn++;
        }

        {
            float defenderInitialPopulation = defender.Population;

            // Lasketaan hyökkääjän panoksen teho
            float attackerAttackPower = attacker.MilitaryPower * (attackPercentage / 100f);
            // puolustajan full power laskenta
         
            float defenderFullPower = defender.MilitaryPower * (1 + defender.DefenseBonus);

            // Lisätään liittouman puolustusbonus, jos puolustajalla on liitto
            float allianceDefenseBonus = 0f;
            Alliance alliance = DiplomacyManager.Instance.GetAllianceForNation(defender);
            if (alliance != null)
            {
                foreach (Nation member in alliance.Members)
                {
                    if (member != defender)
                    {
                        allianceDefenseBonus += member.MilitaryPower * 0.05f;
                    }
                }
                defenderFullPower += allianceDefenseBonus;
                //Debug.Log($"liiton puolustusbonari: {allianceDefenseBonus}");
            }

            // Lasketaan satunnainen kertoimen arvo väliltä 0.85 - 1.2
            float randomDefMultiplier = UnityEngine.Random.Range(0.85f, 1.2f);
            float randomDefMultiplier_two = UnityEngine.Random.Range(0.85f, 1.2f);
            // Lasketaan kaavan mukainen vahinko:
            float damage = (attackerAttackPower / 2.8f) - (defenderFullPower * randomDefMultiplier);
            damage = Mathf.Max(0f, damage);

          
            // Lasketaan, mikä osuus puolustajan powersta "tuhoutuu"
            float lostPercentage = (defenderFullPower > 0f) ? (damage / defenderFullPower * 100f) : 0f;
            //Debug.Log($"Hyökkäys: Hyökkääjän panos: {attackerAttackPower:F2}, Puolustajan power: {defenderFullPower:F2}, Vahinko: {damage:F2}, Tuotettu osuus: {lostPercentage:F2}%");

            // Haetaan kaikki puolustajan hallitsemat ruudut
            List<SquareTile> defenderTiles = grid.GetControlledTiles().Where(t => t.controllingNation == defender).ToList();
            int totalDefenderTiles = defenderTiles.Count;
            int tilesToTransfer = Mathf.RoundToInt(totalDefenderTiles * (lostPercentage / 100f));

            bool anyTilesTransferred = true;
            if (tilesToTransfer <= 0)
            {
                //Debug.Log("Hyökkäys ei vallannut yhtään ruutua, mutta taistelussa syntyi tappioita.");
                anyTilesTransferred = false;
                // tilesToTransferen arvo 0, jolloin ruutujen siirtoa ei yritetä:
                tilesToTransfer = 0;
            }

            // Käytetään satunnaislukugeneraattoria
            System.Random rand = new System.Random();

            // Alustetaan laskuri vallatuille ruuduille
            int capturedTilesCount = 0;
            // toistetaan, kunnes siirrettävien ruutujen määrä loppuu
            while (tilesToTransfer > 0)
            {
                // Lasketaan eligible-ruudut dynaamisesti: etsitään kaikki puolustajan ruudut,
                // joiden ainakin yksi naapuri kuuluu hyökkääjän valtakuntaan.
                List<SquareTile> eligibleTiles = grid.GetControlledTiles()
                    .Where(t => t.controllingNation == defender &&
                                grid.GetNeighbors(t).Any(n => n.controllingNation == attacker) &&
                                (attacker != GameManager.Instance.playerNation || t.IsVisible))
                    .ToList();

                if (eligibleTiles.Count == 0)
                {
                    //Debug.Log("Ei löydy enää siirrettäviä ruutuja hyökkääjän reunoilta.");
                    break;
                }

                // Valitaan satunnaisesti yksi eligible-ruutu
                int index = rand.Next(eligibleTiles.Count);
                SquareTile tileToTransfer = eligibleTiles[index];

                // Siirretään ruutu hyökkääjän hallintaan ja kasvatetaan vallattujen ruutujen laskuria
                grid.TransferTileOwnership(tileToTransfer, attacker);
                capturedTilesCount++;

                tileToTransfer.ApplyOneTimeEffects(attacker, 0.5f);

                // Merkitään ruutu valloitetuksi
                tileToTransfer.capturedThisTurn = true; // Oletuksena tämä boolean on määritelty SquareTile-luokassa
                SpriteRenderer renderer = tileToTransfer.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetFloat("_Captured", 1.0f);  // Merkitään ruutu "valloitetuksi"
                    renderer.SetPropertyBlock(mpb);
                }

                // Jokaisesta vallatusta ruudusta 60% mahdollisuus menettää 1 moraali
                if (UnityEngine.Random.value < 0.60f)
                {
                    attacker.Morale -= 1f;
                }

                // Vähennetään siirrettävien ruutujen lukumäärää
                tilesToTransfer--;
            }

            // aina kun hyökätään niin mahdollisuus menettää moraalia
            if (UnityEngine.Random.value < 0.5f)
            {
                attacker.Morale -= 2f;
            }
            // Lasketaan tappioiden suuruus: voi määritellä tappioille eri kertoimet sen mukaan,
            // onnistuiko ruutujen valtaus vai ei.
            float defenderLossRatio;
            float attackerLossRatio;
            if (anyTilesTransferred)
            {
                // Peruslogiikassa: tappiot perustuvat hyökkäyksen tehoon
                defenderLossRatio = Mathf.Clamp(lostPercentage / 100f, 0f, 0.5f);
                attackerLossRatio = 0.1f;
            }
            else
            {
                // Epäonnistuneessa hyökkäyksessä tappiot ovat pienemmät (tai vaihtoehtoisesti suuremmat, riippuen pelitasapainosta)
                defenderLossRatio = 0.05f * randomDefMultiplier_two; // Esimerkiksi 5% tappiot ja random kerroin
                attackerLossRatio = 0.1f * randomDefMultiplier;
            }

            int defenderLosses = Mathf.RoundToInt(defender.Military * defenderLossRatio);
            int attackerLosses = Mathf.RoundToInt(attacker.Military * attackerLossRatio);

            defender.Military = Mathf.Max(defender.Military - defenderLosses, 0);
            attacker.Military = Mathf.Max(attacker.Military - attackerLosses, 0);

            //Debug.Log($"Puolustaja menetti {defenderLosses} yksikköä, hyökkääjä menetti {attackerLosses} yksikköä.");

            //vähennetään väkilukua menetettyjen armeija­yksiköiden verran
            defender.Population = Mathf.Max(defender.Population - defenderLosses, 0f);
            attacker.Population = Mathf.Max(attacker.Population - attackerLosses, 0f);

            // Laske lisämenetys: jokaisesta menetetystä laatasta 2% alkuperäisestä väkiluvusta.
            int additionalPopulationLoss = Mathf.RoundToInt(defenderInitialPopulation * 0.02f * capturedTilesCount);
            defender.Population = Mathf.Max(defender.Population - additionalPopulationLoss, 0f);
            //Debug.Log($"Lisämenetys: Puolustajan väkiluvusta poistettiin {additionalPopulationLoss} yksikköä {capturedTilesCount} menetetystä laatasta (2% per laatta).");



            DiplomacyManager.Instance.RegisterAttack(attacker, defender);
            // Käytetään bool-arvoa selvittämään, onko pelaaja mukana
            Nation player = GameManager.Instance.playerNation;
            bool isPlayerInvolved = (attacker == player || defender == player);

            // Tarkistetaan myös, onko hyökkääjä tai puolustaja liittolaisvaltakuntaa
            if (!isPlayerInvolved)
            {
                Alliance playerAlliance = DiplomacyManager.Instance.GetAllianceForNation(player);
                if (playerAlliance != null)
                {
                    isPlayerInvolved = playerAlliance.Members.Contains(attacker) || playerAlliance.Members.Contains(defender);
                }
            }

            if (isPlayerInvolved)
            {
                GameManager.Instance.UpdateUI();
                AttackResultPanel.Instance.Show(attackerAttackPower, defenderFullPower, allianceDefenseBonus, attackerLosses, defenderLosses, capturedTilesCount, attacker.Name, defender.Name);
                yield return new WaitUntil(() => AttackResultPanel.Instance.IsClosed);
            }


            // Tarkistetaan, onko puolustajalla enää hallittuja ruutuja
            List<SquareTile> remainingDefenderTiles = grid.GetControlledTiles()
                .Where(t => t.controllingNation == defender)
                .ToList();
            if (remainingDefenderTiles.Count == 0)
            {
                if (defender == GameManager.Instance.playerNation)
                {
                    //Debug.Log("Pelaaja on menettänyt viimeisen ruutunsa. Pelin tilanne: häviö!");
                    GameManager.Instance.GameOver();
                }
                else
                {
                    // Jos puolustaja on tekoäly, poistetaan se pelistä
                    AIController aiController = FindObjectOfType<AIController>();
                    if (aiController != null)
                    {
                        aiController.RemoveAIManagerForNation(defender);
                    }
                }
            }

        }
    }
}


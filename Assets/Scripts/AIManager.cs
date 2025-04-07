using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum AIType
{
    Aggressive, // Hyökkää heti, jos sen armeija on suhteessa vahva
    Neutral,    // Hyökkää vain, jos tilanne on selvästi edullinen tai suhteet ovat huonot
    Peaceful    // Ei aloita sotia itse, keskittyy kehitykseen ja laajentumiseen
}

public class AIManager : MonoBehaviour
{
    [Header("Viittaukset")]
    public SquareGrid grid;      // Viite ruudukkoon
    public Nation aiNation;      // Tekoälyn valtakunta

    [Header("Laajentumisparametrit")]
    public int expansionInterval = 5; // Laajennetaan joka 5. vuoro 

    [Header("AI Parametrit")]
    public AIType aiType = AIType.Neutral; // Oletuksena neutraali, mutta voi satunnaistaa!


    void Awake()
    {
        if (aiNation == null)
        {
            // Luodaan uusi Nation ja asetetaan sille satunnainen väri
            aiNation = new Nation();
            aiNation.Morale = 80;
            aiNation.EmpireColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.7f, 1f); // Satunnainen, hyvin näkyvä väri

            // Aseta AI:n persoonallisuus satunnaisesti
            float roll = Random.value;
            if (roll < 0.25f)
                aiType = AIType.Aggressive;
            else if (roll < 0.45f)
                aiType = AIType.Neutral;
            else
                aiType = AIType.Peaceful;

            //Debug.Log("Asetettu AI-tyyppi: " + aiType);
        }
    }

    // asetetaan tekoälyvallakunnalle aloitusruutu
    void Start()
    {
        if (grid != null)
        {
            grid.InitializeStartingAreaForNation(aiNation, false);
        }
        else
        {
            //Debug.LogWarning("AIManager: Grid ei ole asetettu.");
        }
    }

    public bool IsActive { get; private set; } = true;

    public void MarkForRemoval()
    {
        IsActive = false;
        // Mahdollisesti myös muita cleanup-toimenpiteitä!
    }

    // Apumetodi, joka määrittelee laajennusalueen koon nykyisen vuoron perusteella
    private (int min, int max) GetExpansionRange(int currentTurn)
    {
        if (currentTurn < 30)
        {
            return (1, 7);
        }
        else if (currentTurn < 80)
        {
            return (1, 15);
        }
        else if (currentTurn < 105)
        {
            return (5, 25);
        }
        else if (currentTurn < 145)
        {
            return (10, 40);
        }
        else if (currentTurn < 270)
        {
            return (10, 55);
        }
        else
        {
            // Fallback: sama kuin aikakausi 5
            return (10, 55);
        }
    }

    void BalanceArmyAndEconomy(Nation nation, int currentTurn)
    {
        float netIncome = nation.LastTurnNetGDPGrowth;

        if (netIncome < -10f)
        {
            // Tulot alle -10: pienennetään armeijaa 20 %
            int reduction = Mathf.RoundToInt(nation.Military * 0.20f);
            nation.Military = Mathf.Max(nation.Military - reduction, 100);
            //Debug.Log($"Negatiiviset tulot alle -10 ({netIncome}). Armeijaa pienennetty 20%:lla, vähennys: {reduction} yksikköä.");
        }
        else if (netIncome >= -10f && netIncome < 0f)
        {
            // Tulot -10 - 0: pienennetään armeijaa 10 %
            int reduction = Mathf.RoundToInt(nation.Military * 0.10f);
            nation.Military = Mathf.Max(nation.Military - reduction, 100);
            //Debug.Log($"Negatiiviset tulot -10 - 0 ({netIncome}). Armeijaa pienennetty 10%:lla, vähennys: {reduction} yksikköä.");
        }
        else if (netIncome >= 0f && netIncome < 3f)
        {
            // Tulot 0-5: ei muutosta
            //Debug.Log($"Tulot 0-5 ({netIncome}). Armeijaa ei kehitetty.");
        }
        else if (netIncome >= 3f && netIncome < 10f)
        {
            // 2.5%
            float increaseFactor = 0.0250f;
            int increase = Mathf.RoundToInt(nation.Military * increaseFactor);
            nation.Military += increase;
            //Debug.Log($"Tulot 5-10 ({netIncome}). Armeijaa kasvatettu 1.25%:lla, lisäys: {increase} yksikköä.");
        }
        else if (netIncome >= 10f && netIncome <= 25f)
        {
            // Tulot 10-25: kasvatetaan armeijaa 10 %
            int increase = Mathf.RoundToInt(nation.Military * 0.1f);
            nation.Military += increase;
            //Debug.Log($"Tulot 10-25 ({netIncome}). Armeijaa kasvatettu 5%:lla, lisäys: {increase} yksikköä.");
        }
        else if (netIncome > 25f)
        {
            // Tulot yli 25: kasvatetaan armeijaa 30 %
            int increase = Mathf.RoundToInt(nation.Military * 0.30f);
            nation.Military += increase;
        }

        // Varmistetaan, että armeija ei ylitä 10 % kansan määrästä ja että se pysyy vähintään 100 yksikössä.
        int maxAllowed = Mathf.FloorToInt(nation.Population * 0.8f);
        if (nation.Military > maxAllowed)
        {
            nation.Military = maxAllowed;
        }
        if (nation.Military < 100)
        {
            nation.Military = 100;
        }
    }

    /// <summary>
    /// Tarkistaa jokaisella AI:n vuorolla, kannattaako teknologiatason nostoa yrittää.
    /// Perustodennäköisyys on 1% ja jos vähintään yksi naapurivaltio on teknologiaa korkeampi,
    /// lisätään 5% lisäys.
    /// </summary>
    /// <param name="currentTurn">Nykyinen vuoro</param>
    private void ProcessTechAdvancement(int currentTurn)
    {
        // Perustodennäköisyys on 1%
        float baseChance = 0.01f;
        bool neighborHasHigherTech = false;

        // Haetaan kaikki ruudut, joita tämä AI hallitsee
        List<SquareTile> controlledTiles = grid.GetControlledTiles().Where(t => t.controllingNation == aiNation).ToList();
        HashSet<Nation> neighborNations = new HashSet<Nation>();

        // Käydään läpi kaikkien hallittujen ruutujen naapurit ja kerätään niistä eri valtioita
        foreach (SquareTile tile in controlledTiles)
        {
            foreach (SquareTile neighbor in grid.GetNeighbors(tile))
            {
                if (neighbor.controllingNation != null && neighbor.controllingNation != aiNation)
                {
                    neighborNations.Add(neighbor.controllingNation);
                }
            }
        }

        // Jos vähintään yhdellä naapurivaltiossa on korkeampi teknologia, nostetaan todennäköisyyttä
        foreach (Nation neighbor in neighborNations)
        {
            if (neighbor.Technology > aiNation.Technology)
            {
                neighborHasHigherTech = true;
                break;
            }
        }

        float advancementChance = baseChance + (neighborHasHigherTech ? 0.05f : 0f);

        // Rajoitetaan teknologian nousu:
        // Jos nykyinen teknologiataso on 1, teknologia 2 ei ole mahdollista ennen vuoroa 25.
        if (aiNation.Technology == 1 && currentTurn < 25)
        {
            return;
        }
        // Jos nykyinen teknologiataso on 2, teknologia 3 ei ole mahdollista ennen vuoroa 70.
        if (aiNation.Technology == 2 && currentTurn < 70)
        {
            return;
        }

        // Suoritetaan satunnaisheitto teknologiatason nostoa varten
        if (Random.value < advancementChance)
        {
            aiNation.Technology++;
            //Debug.Log($"AINation {aiNation.Name} nousee teknologia tasolle {aiNation.Technology} (todennäköisyys oli {advancementChance * 100:F1}%).");
            ApplyTechAdvancementEffects(aiNation);
        }
    }

    /// <summary>
    /// Aktivoi teknologiatasoon liittyvät kehitysvaikutukset.
    /// Esimerkiksi teknologia tasolla 1 aktivoidaan Swordsmen-vaikutukset.
    /// </summary>
    /// <param name="nation">Tekoälyn valtakunta</param>
    private void ApplyTechAdvancementEffects(Nation nation)
    {
        // Voi laajentaa tapauskohtaisia vaikutuksia tarpeen mukaan.
        switch (nation.Technology)
        {
            case 1:
                // Teknologia tasolla 1: esim. aktivoi Swordsmen-vaikutus, joka nostaa sotilaallista tehoa
                if (nation.MilitaryTech < 0.1f)
                {
                    nation.SetMilitaryTech(0.1f);
                    nation.MilitaryConsumptionRate *= 1.42f; // pelaajalla on 1.5
                    //Debug.Log("Swordsmen-vaikutus aktivoitu tekoälylle teknologia tasolla 1.");
                }
                break;
            case 2:
                if (nation.MilitaryTech < 0.2f)
                {
                    nation.SetMilitaryTech(0.2f);
                    nation.MilitaryConsumptionRate *= 1.42f; //pelaajalla on 1.5
                    //Debug.Log("Tekoäly saavutti teknologia tason 2 – päivitetään sotilaalliset ominaisuudet.");
                }
                break;
            // TODO, tää kehittyy myöhemmin
            default:
                //Debug.Log($"Tekoälylle teknologia tasolla {nation.Technology} ei ole määriteltyjä lisävaikutuksia.");
                break;
        }
    }

    public void ProcessAITurn(int currentTurn)
    {
        // Tarkistetaan ensin, onko tekoälyllä enää hallittuja ruutuja
        int controlledTilesCount = grid.GetControlledTiles().Count(t => t.controllingNation == aiNation);
        if (controlledTilesCount == 0)
        {
            //Debug.Log($"AIManager ({aiNation.EmpireColor}): Ei enää hallittuja ruutuja, poistetaan tekoäly.");
            MarkForRemoval();
            return;
        }

        if (!IsActive)
        {
            // Tää AIManager on jo merkitty poistettavaksi, joten ohitetaan vuoro.
            return;
        }
        ProcessTechAdvancement(currentTurn);

        // Jos nykyinen vuosi on 900–1500, arvotaan Musta surma -tapahtuma tekoälylle
        if (GameManager.Instance.CurrentYear >= 900 && GameManager.Instance.CurrentYear <= 1500 && !aiNation.blackDeathOccurred)
        {
            if (Random.value < 0.03f)
            {
                float reduction = Random.Range(0.15f, 0.36f);
                aiNation.Population *= (1 - reduction);
                aiNation.blackDeathOccurred = true; // Merkitään, että musta surma on tapahtunut
                //Debug.Log($"Musta surma osui AI:lle {aiNation.Name}, väkiluku vähentyi {reduction * 100:F1}%.");
            }
        }

        // Päivitetään ensin mahdolliset aktiiviset kaupat tekoälyn kohdalla.
        TradeManager.ProcessIronTradeForNation(aiNation);


        // Jos tekoälyltä ei ole aktiivista raudan kauppaa, yritetään ensin kaupankäynti pelaajan kanssa...
        if (!aiNation.HasActiveIronTrade)
        {
            Nation player = GameManager.Instance.playerNation;
            if (TradeManager.playerTradeDeclineCooldown > 0)
            {
                //Debug.Log("Pelaaja on kieltäytynyt raudankaupasta, ohitetaan kauppayritys.");
            }
            else if (!player.HasActiveIronTrade && TradeManager.IsTradePossible(player, aiNation))
            {
                // Satunnaisuus: käynnistetään kauppapyyntö vain 30 % mahdollisuudella
                if (Random.value < 0.3f)
                {
                    if (GameManager.Instance.tradePanel != null)
                    {
                        GameManager.Instance.tradePanel.gameObject.SetActive(true);
                        GameManager.Instance.tradePanel.InitializeTrade(player, aiNation);
                    }
                    else
                    {
                        //Debug.LogWarning("TradePanel-viitettä ei ole asetettu GameManagerissa!");
                    }
                }
            }

            // ... ja sitten, mikäli pelaakauppa ei ole käynnissä, yritetään kauppaa tekoälystä tekoälyyn.
            List<AIManager> allAIs = FindObjectsOfType<AIManager>().ToList();
            List<AIManager> potentialSellers = allAIs
                .Where(m => m.aiNation != aiNation &&
                            m.aiNation.HasIronCard &&
                            !m.aiNation.HasActiveIronTrade &&
                            TradeManager.IsTradePossible(m.aiNation, aiNation))
                .ToList();
            if (potentialSellers.Count > 0)
            {
                // Satunnaisuus: yritetään aloittaa kauppa vain 30 % mahdollisuudella
                if (Random.value < 0.3f)
                {
                    AIManager sellerManager = potentialSellers[Random.Range(0, potentialSellers.Count)];
                    //Debug.Log("Tekoäly yrittää ostaa raudasta toiselta tekoälyltä.");
                    TradeManager.ExecuteTrade(sellerManager.aiNation, aiNation);
                }
            }
        }

        if (currentTurn <= 40 && currentTurn % 5 == 0)
        {
            if (aiNation.Religion == null && Random.value < 0.04f) // 4% mahis 5 vuoron välein löytää uskonto. 
            {
                // Luodaan uusi uskonto. nyt nimi perustuu valtakunnan nimeen TODO tätä pitöö kehittää
                string religionName = "Uskonto_" + aiNation.Name;
                // Voi myös generoida satunnaisen värin uskonnalle:
                Color religionColor = new Color(Random.value, Random.value, Random.value);
                // Kehitetään uskonto tekoälyn valtakunnalle
                aiNation.DevelopReligion(religionName, religionColor);
                //Debug.Log($"AI '{aiNation.Name}' löysi uskonnon: {religionName}");

                // Asetetaan uskonto satunnaiselle tekoälyn hallitsemalle ruudulle
                List<SquareTile> myTiles = grid.GetControlledTiles()
                                               .Where(t => t.controllingNation == aiNation)
                                               .ToList();
                if (myTiles.Count > 0)
                {
                    SquareTile chosenTile = myTiles[Random.Range(0, myTiles.Count)];
                    chosenTile.Religion = aiNation.Religion;
                }
                // Päivitetään diplomaattiset suhteet uskonnon perusteella
                DiplomacyManager.Instance.UpdateReligionRelationshipsForNation(aiNation);
            }
        }
        // Uskonnon levittäminen joka 2. vuoro
        if (currentTurn % 2 == 0)
        {
            // Kerätään tekoälyn hallitsemat ruudut, joissa on uskontoa
            List<SquareTile> religionTiles = grid.GetControlledTiles()
                                                 .Where(t => t.controllingNation == aiNation && t.Religion != null)
                                                 .ToList();
            if (religionTiles.Count > 0 && Random.value < 0.5f)
            {
                // Selvitetään, jos alueella on useampia uskontoja, mikä on yleisin
                var religionFrequency = religionTiles.GroupBy(t => t.Religion)
                                                     .Select(g => new { Religion = g.Key, Count = g.Count() })
                                                     .OrderByDescending(x => x.Count)
                                                     .ToList();
                Religion religionToSpread = religionFrequency[0].Religion;

                // Valitaan satunnaisesti 1–10 ruutua, joihin uskonto levitetään
                int tilesToSpread = Random.Range(1, 11); // Random.Range(int, int) eli 1,11 on 1-10

                // Käytetään jo olemassa olevaa ReligionManager.SpreadReligionDigging -metodia.
                // (HUOM: SpreadReligionDigging-metodii pitäää ehkä muokata että se ottaa suoraan
                //    public static void SpreadReligionDigging(SquareGrid grid, Religion religion, int maxTilesToSpread) )
                ReligionManager.SpreadReligionDigging(grid, aiNation, tilesToSpread);
            }
        }

        // Päivitetään AI:n virallinen uskonto sen hallitsemien ruutujen perusteella
        aiNation.UpdateMajorityReligion(grid);


        // Jos moraali on alle 20, 15 % mahdollisuus sisällissotaan.
        if (aiNation.Morale < 20f)
        {
            if (Random.value < 0.15f) //15% mahis joka vuoro
            {
                //Debug.Log("AIManager: Sisällissota laukaistu!");
                TriggerCivilWar();
                // Voidaan palauttaa tässä, mikäli halutaan, että vuoro loppuu sisällissodan jälkeen.
                return;
            }
        }

        if (aiNation.Morale < 20f)
        {
            // Kun moraali on alle 20, lisätään moraalia joka vuoro ilman satunnaisuutta
            int moraleGain = Random.Range(1, 6); // Random-arvo väliltä 1–5
            aiNation.IncreaseMorale(moraleGain);
        }
        else if (currentTurn % 2 == 0)
        {
            // Muutoin 50% mahdollisuus joka toinen vuoro
            if (Random.value < 0.5f)
            {
                int moraleGain = Random.Range(1, 7); // 50% mahis saada 1-6 moraalia 2 vuoron välei
                aiNation.IncreaseMorale(moraleGain);
            }
        }

        // jos AI:n moraali on yli 95, niin joka toinen vuoro (parilliset vuorot)
        // 50% mahdollisuus, että moraalia laskee 2.
        if (currentTurn % 2 == 0 && aiNation.Morale > 95f)
        {
            if (Random.value < 0.30f)
            {
                aiNation.DecreaseMorale(2f);
                
            }
        }

        if (currentTurn % 5 == 0)
        {
            // 2% mahdollisuus kaupungin perustamiseen
            if (Random.value < 0.02f)
            {
                // Lasketaan, kuinka monta kaupunkia AI:lla on jo
                int cityCount = grid.GetControlledTiles().Count(t => t.controllingNation == aiNation && t.HasCity);
                if (cityCount < 3)
                {
                    // Valitaan satunnainen ruutu, jossa ei vielä ole kaupunkia ja joka ei ole vesialue (MakeaVesi)
                    SquareTile freeTile = grid.GetControlledTiles()
                        .Where(t => t.controllingNation == aiNation && !t.HasCity && t.Terrain != SquareTile.TerrainType.MakeaVesi)
                        .OrderBy(t => Random.value)
                        .FirstOrDefault();

                    if (freeTile != null)
                    {
                        freeTile.HasCity = true;
                        aiNation.IncomeBonusPerTurn += 5;
                        //Debug.Log($"AIManager: Kaupunki perustettu ruutuun ({freeTile.X}, {freeTile.Y}).");

                        // Jos ruutu on vielä tutkimaton pelaajan näkökulmasta, pidetään se piilossa:
                        if (!freeTile.HasBeenExplored)
                        {
                            freeTile.SetVisibility(false, false);
                        }
                    }
                    else
                    {
                        //Debug.Log("AIManager: Ei vapaita ruutuja kaupungille.");
                    }
                }
            }
        }


        // jos naapurivaltion MilitaryPower on suurempi,
        // niin joka 5. vuoro 50% mahdollisuus, että moraalia laskee 1.
        if (currentTurn % 5 == 0)
        {
            List<SquareTile> myTiles = grid.GetControlledTiles().Where(t => t.controllingNation == aiNation).ToList();
            bool foundStrongerNeighbor = false;
            foreach (SquareTile tile in myTiles)
            {
                foreach (SquareTile neighbor in grid.GetNeighbors(tile))
                {
                    if (neighbor.controllingNation != null && neighbor.controllingNation != aiNation &&
                        neighbor.controllingNation.MilitaryPower > aiNation.MilitaryPower)
                    {
                        foundStrongerNeighbor = true;
                        break;
                    }
                }
                if (foundStrongerNeighbor)
                    break;
            }
            if (foundStrongerNeighbor && Random.value < 0.5f)
            {
                aiNation.DecreaseMorale(1f);
                //Debug.Log("AIManager: Naapurivaltion suurempi sotilaallinen voima aiheutti moraalin laskua (1).");
            }
        }

        // Varmistetaan, että AI:n liittoon liittymiskylmäaika vähenee joka vuoro, mikäli asetettu.
        if (aiNation.allianceJoinCooldown > 0)
        {
            aiNation.allianceJoinCooldown--;
        }

        // Tarkistetaan, onko AI tällä hetkellä liitossa.
        Alliance currentAlliance = DiplomacyManager.Instance.GetAllianceForNation(aiNation);
        if (currentAlliance != null)
        {
            // 1% todennäköisyys lähteä liitosta.
            if (Random.value < 0.01f)
            {
                currentAlliance.RemoveMember(aiNation);
                aiNation.allianceJoinCooldown = 10; // Estetään uudelleen liittyminen tähän liittoon 10 vuoron ajan.
                //Debug.Log($"{aiNation.Name} on jättänyt liiton {currentAlliance.Name} ja ei voi liittyä siihen uudelleen 10 vuoron ajan!!");
            }
        }

        //Debug.Log($"AIManager ({aiNation.EmpireColor}): Käynnistetään tekoälyvuoro {currentTurn}.");
        BalanceArmyAndEconomy(aiNation, currentTurn);
        // Tarkastellaan laajennusmahdollisuutta joka 5. vuoro
        if (currentTurn % expansionInterval == 0)
        {
            //Debug.Log("AIManager: Laajennusvaihe käynnissä.");

            // 50 % mahdollisuus laajentaa
            if (Random.value < 0.5f)
            {
                // Määritellään nykyisen vuoron perusteella laajennusalueen minimi ja maksimi
                (int minExpansion, int maxExpansion) = GetExpansionRange(currentTurn);
                // Arvotaan laajennettavien ruutujen määrä
                int expansionCount = Random.Range(minExpansion, maxExpansion + 1); // +1 koska eksulsiivisuus!
                //Debug.Log($"AIManager: Päätettiin laajentaa {expansionCount} ruutua (aikakauden perusteella).");

                int successfulExpansions = 0;
                int attempts = 0;
                int maxAttempts = expansionCount * 3; // Esimerkiksi 3 yritystä per haluttu ruutu 

                while (successfulExpansions < expansionCount && attempts < maxAttempts)
                {
                    attempts++;
                    SquareTile newTile = grid.ExpandTerritoryForNation(aiNation);
                    if (newTile != null)
                    {
                        //Debug.Log($"AIManager: Laajennettu ruutuun ({newTile.X}, {newTile.Y}).");
                        successfulExpansions++;
                        // Jokaisesta laajennetusta ruudusta 40% mahdollisuus laskea moraalia yhdellä.
                        if (Random.value < 0.4f)
                        {
                            aiNation.DecreaseMorale(1f);
                        }
                    }
                }
                if (successfulExpansions < expansionCount)
                {
                    //Debug.Log("AIManager: Ei löydetty tarpeeksi laajennettavia ruutuja.");
                }
            }
            else
            {
                //Debug.Log("AIManager: Laajennusvaihe käynnistyi, mutta satunnaisuus esti laajennuksen.");
            }
        }
        else
        {
            //Debug.Log("AIManager: Ei laajennusta tällä vuorolla. Voidaan toteuttaa muita strategisia toimintoja.");
        }

        // Jos teknologia taso on vähintään 2, yritetään lisätä lisäkehityksiä
        if (aiNation.Technology >= 2)
        {
            // Yritetään rakentaa Castle 4% todennäköisyydellä
            if (Random.value < 0.04f)
            {
                // Tarkistetaan, ettei linnan rakentaminen ole jo käytetty kahdesti
                if (aiNation.CastleCardActivations < 2)
                {
                    // Etsitään vapaa ruutu, jossa ei ole jo kaupunkia, linnaa eikä ruudun tyyppi ole MakeaVesi
                    SquareTile freeTile = grid.GetControlledTiles()
                        .Where(t => t.controllingNation == aiNation && !t.HasCity && !t.HasCastle && !t.HasPort && t.Terrain != SquareTile.TerrainType.MakeaVesi)
                        .OrderBy(t => Random.value)
                        .FirstOrDefault();
                    if (freeTile != null)
                    {
                        freeTile.HasCastle = true;
                        aiNation.DefenseBonus += 0.05f;
                        aiNation.IncomeBonusPerTurn -= 1;
                        aiNation.CastleCardActivations++;
                        //Debug.Log($"AI {aiNation.Name} on rakentanut linnan ruutuun ({freeTile.X}, {freeTile.Y}).");
                    }
                    else
                    {
                        //Debug.Log("AI ei löytänyt vapaata ruutua linnalle.");
                    }
                }
                else
                {
                    //Debug.Log("AI: Castle on jo käytetty kahdesti.");
                }
            }

            List<Nation> potentialAllies = new List<Nation>();
            foreach (Nation other in DiplomacyManager.Instance.GetAllRegisteredNations())
            {
                if (other != aiNation && !DiplomacyManager.Instance.IsInAlliance(other))
                {
                    potentialAllies.Add(other);
                }
            }
            potentialAllies = potentialAllies.Distinct().ToList();

            foreach (Nation potentialAlly in potentialAllies)
            {
                int rel = DiplomacyManager.Instance.GetRelationship(aiNation, potentialAlly);
                if (rel >= DiplomacyManager.Instance.allianceThreshold && UnityEngine.Random.value < 0.2f)
                {
                    DiplomacyManager.Instance.ProposeAlliance(aiNation, potentialAlly);
                }
            }

            // Yritetään rakentaa Port 1.5% todennäköisyydellä
            if (aiNation.PortCardActivations < 2 && Random.value < 0.015f)
            {
                // Tarkistetaan, onko hallituissa ruuduissa vähintään yksi, jonka naapureista löytyy meriruutu (Merivesi)
                List<SquareTile> seaBorderTiles = grid.GetControlledTiles()
                    .Where(t => t.controllingNation == aiNation && grid.GetNeighbors(t).Any(n => n.Terrain == SquareTile.TerrainType.Merivesi))
                    .ToList();
                if (seaBorderTiles.Count > 0)
                {
                    // Etsitään vapaa ruutu, johon satama voidaan perustaa (ei kaupunkia, linnaa tai satamaa, ja ruudun naapurissa on Merivesi)
                    SquareTile freeTile = grid.GetControlledTiles()
                        .Where(t => t.controllingNation == aiNation && !t.HasCity && !t.HasCastle && !t.HasPort && grid.GetNeighbors(t).Any(n => n.Terrain == SquareTile.TerrainType.Merivesi))
                        .OrderBy(t => Random.value)
                        .FirstOrDefault();
                    if (freeTile != null)
                    {
                        freeTile.HasPort = true;
                        aiNation.IncomeBonusPerTurn += 7;
                        aiNation.PortCardActivations++;
                        //Debug.Log($"AI {aiNation.Name} on perustanut sataman ruutuun ({freeTile.X}, {freeTile.Y}).");
                    }
                    else
                    {
                        //Debug.Log("AI ei löytänyt vapaata ruutua satamalle.");
                    }
                }
                else
                {
                    //Debug.Log("AI: Ei ole merirajaa, joten Portia ei voi perustaa.");
                }
            }
        }


        aiNation.ApplyBasicGrowth(grid);
        AttemptAttack(currentTurn);
    }

    private void AttemptAttack(int currentTurn)
    {

        // Jos tekoälyn moraali on alle 50, se ei voi hyökätä muita vastaan
        if (aiNation.Morale < 50f)
        {
            return;
        }


        // Kerätään AI:n hallitsemat ruudut
        List<SquareTile> myTiles = grid.GetControlledTiles().Where(t => t.controllingNation == aiNation).ToList();
        // Kerätään vihollisvaltioiden lista
        List<Nation> enemyNations = new List<Nation>();

        foreach (SquareTile tile in myTiles)
        {
            List<SquareTile> neighbors = grid.GetNeighbors(tile);
            foreach (SquareTile neighbor in neighbors)
            {
                // Jos naapurin valtakunta on olemassa ja eri kuin meidän
                if (neighbor.controllingNation != null && neighbor.controllingNation != aiNation)
                {
                    if (!enemyNations.Contains(neighbor.controllingNation))
                        enemyNations.Add(neighbor.controllingNation);
                }
            }
        }

        foreach (Nation enemy in enemyNations)
        {
            // Varmistetaan, että hyökkäävällä AI:lla on armeijaa
            if (aiNation.Military <= 0)
            {
                //Debug.Log("Hyökkäystä ei suoriteta, koska oma armeija on 0.");
                continue;
            }

            // Haetaan diplomaattinen suhde tekoälyn ja vihollisen välillä
            int relationship = DiplomacyManager.Instance.GetRelationship(aiNation, enemy);

            // Määritellään kynnysarvo tekoälyn tyypin perusteella
            if (aiType == AIType.Neutral && relationship >= 5)
            {
                //Debug.Log($"Neutral AI: {aiNation.Name} ja {enemy.Name} suhde ({relationship}) on liian hyvä, hyökkäystä ei suoriteta.");
                continue;
            }
            else if (aiType == AIType.Aggressive && relationship >= 10)
            {
                //Debug.Log($"Aggressive AI: {aiNation.Name} ja {enemy.Name} suhde ({relationship}) on liian hyvä, hyökkäystä ei suoriteta.");
                continue;
            }

            // Hyökkäyksen logiikka tekoälyn tyypin mukaan
            switch (aiType)
            {
                case AIType.Aggressive:
                    if (aiNation.MilitaryPower >= 1.65 * enemy.MilitaryPower)
                    {
                        if (Random.value < 0.8f)  // 80 % todennäköisyys hyökkäykseen
                        {
                            
                            StartCoroutine(CombatManager.Instance.ExecuteAttackCoroutine(aiNation, enemy, 88f));
                        }
                        return; // Hyökkäys yritetään vain kerran per vuoro.
                    }
                    break;
                case AIType.Neutral:
                    if (aiNation.MilitaryPower >= 3.25 * enemy.MilitaryPower)
                    {
                        if (Random.value < 0.8f)  // 80 % todennäköisyys hyökkäykseen
                        {
                            
                            StartCoroutine(CombatManager.Instance.ExecuteAttackCoroutine(aiNation, enemy, 83f));
                        }
                        return;
                    }
                    break;
                case AIType.Peaceful:
                    // Peaceful AI ei hyökää koskaan.
                    break;
            }
        }
    }

        private void TriggerCivilWar()
    {
        // Haetaan tekoälyn hallitsemat ruudut.
        List<SquareTile> controlledTiles = grid.GetControlledTiles().Where(t => t.controllingNation == aiNation).ToList();
        int totalTiles = controlledTiles.Count;
        if (totalTiles == 0)
            return;

        // Ryhmitellään hallitut ruudut kontiguoituihin alueisiin käyttäen BFS:ää.
        List<HashSet<SquareTile>> connectedGroups = new List<HashSet<SquareTile>>();
        HashSet<SquareTile> visited = new HashSet<SquareTile>();

        foreach (SquareTile tile in controlledTiles)
        {
            if (!visited.Contains(tile))
            {
                HashSet<SquareTile> group = new HashSet<SquareTile>();
                Queue<SquareTile> queue = new Queue<SquareTile>();
                queue.Enqueue(tile);
                group.Add(tile);
                while (queue.Count > 0)
                {
                    SquareTile current = queue.Dequeue();
                    foreach (SquareTile neighbor in grid.GetNeighbors(current))
                    {
                        if (neighbor.controllingNation == aiNation && !group.Contains(neighbor))
                        {
                            group.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                connectedGroups.Add(group);
                visited.UnionWith(group);
            }
        }

        // Valitse suurin kontiguoitu ryhmä
        HashSet<SquareTile> largestGroup = connectedGroups.OrderByDescending(g => g.Count).FirstOrDefault();
        if (largestGroup == null || largestGroup.Count == 0)
            return;

        // Päivitetään tileFraction ja tilesToSplit suhteen vain suurimmasta ryhmästä.
        float tileFraction = Random.Range(0.2f, 0.8f);
        int tilesToSplit = Mathf.RoundToInt(largestGroup.Count * tileFraction);
        if (tilesToSplit == 0)
            tilesToSplit = 1;

        // Valitaan kontiguoitu joukko ruutuja aloittaen satunnaisesta ruudusta suurimmassa ryhmässä.
        HashSet<SquareTile> splitTiles = new HashSet<SquareTile>();
        List<SquareTile> largestGroupList = largestGroup.ToList();
        SquareTile startTile = largestGroupList[Random.Range(0, largestGroupList.Count)];
        Queue<SquareTile> bfsQueue = new Queue<SquareTile>();
        bfsQueue.Enqueue(startTile);
        splitTiles.Add(startTile);

        while (bfsQueue.Count > 0 && splitTiles.Count < tilesToSplit)
        {
            SquareTile current = bfsQueue.Dequeue();
            foreach (SquareTile neighbor in grid.GetNeighbors(current))
            {
                if (neighbor.controllingNation == aiNation && largestGroup.Contains(neighbor) && !splitTiles.Contains(neighbor))
                {
                    splitTiles.Add(neighbor);
                    bfsQueue.Enqueue(neighbor);
                    if (splitTiles.Count >= tilesToSplit)
                        break;
                }
            }
        }

        // Lasketaan uuden valtion populaatio ja armeija dynaamisesti vanhasta.
        float popFraction = Random.Range(0.2f, 0.8f);

        float randomMultiplier = Random.Range(0.1f, 0.85f);
        float newStatePopulation = aiNation.Population * randomMultiplier;
        aiNation.Population -= newStatePopulation;

        float militaryFraction = Random.Range(0.05f, 0.85f);
        int newStateMilitary = Mathf.RoundToInt(aiNation.Military * militaryFraction);
        aiNation.Military -= newStateMilitary;

        // Luodaan uusi Nation-olio uudelle valtiolle.
        Nation newNation = new Nation();
        newNation.InitializeCivilWarNation();
        newNation.Name = aiNation.Name + " (Civil war)";
        newNation.Technology = aiNation.Technology;
        newNation.InitializeCivilWarNation(newStatePopulation);
        newNation.Military = newStateMilitary;
        newNation.GDP = aiNation.GDP * popFraction;
        newNation.EmpireColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.7f, 1f);
        newNation.CivilWarTurnCreated = GameManager.Instance.CurrentTurn;

        // Siirretään valitut ruudut uuteen valtioon.
        foreach (SquareTile tile in splitTiles)
        {
            tile.SetControlled(true, newNation, false);
        }

        // Hajoavan valtion moraalia nostetaan 40 pistettä.
        aiNation.IncreaseMorale(40f);

        //Debug.Log($"Sisällissota: uusi valtio '{newNation.Name}' muodostui. Se sai {splitTiles.Count} ruutua, väkiluku {newStatePopulation:F0} ja armeija {newStateMilitary}.");

        // Luodaan uusi AIManager uudelle valtiolle.
        AIManager newAI = Instantiate(GameManager.Instance.aiManagerPrefab, transform).GetComponent<AIManager>();
        newAI.aiNation = newNation;
        newAI.grid = grid;

        // Rekisteröidään uusi AIManager pää-AIControlleriin, jotta se osallistuu vuoron prosessointiin.
        AIController aiController = FindObjectOfType<AIController>();
        if (aiController != null)
        {
            aiController.RegisterAIManager(newAI);
        }
    }

}

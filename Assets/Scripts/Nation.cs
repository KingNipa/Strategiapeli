using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Nation
{

    public string Name { get; set; }
    public float Population { get; set; }
    public float GDP { get; set; }
    private int military; // Yksityinen kenttä armeijalle
    public int Military
    {
        get { return military; }
        set
        {
            // Lasketaan maksimissaan sallitut armeijan yksiköt: 25% väestöstä.
            int maxAllowed = Mathf.FloorToInt(Population * 0.25f);
            // Asetetaan military arvo, mutta ei yli 25%:n.
            military = Mathf.Max(Mathf.Min(value, maxAllowed), 1);
            //Debug.Log($"Armeijan määrä asetettu: {military} (maksimi {maxAllowed} yksikköä, 25% väestöstä)");
        }
    }

    private float morale;
    public float Morale
    {
        get => morale;
        set
        {
            morale = Mathf.Clamp(value, 0f, 100f); // min 0 max 100
        }
    }
    private float populationIncomeAccumulator = 0f;

    public float cheatPowerBonus = 0f;
    private int technology;
    public int Technology
    {
        get => technology;
        set => technology = value;
    }

    // Tallentaa viimeisimmän vuoron GDP:n kasvun
    public float LastTurnNetGDPGrowth { get; private set; } = 0f;

    // Sotilasteknologiakerroin, alkaa 0.05:stä.
    public float MilitaryTech { get; set; } = 0.05f;

    // Lopullinen sotilaallinen voima
    public float MilitaryPower
    {
        get
        {
            float multiplier = 1.0f;
            if (Morale >= 80f)
            {
                multiplier = 1.1f;
            }
            else if (Morale >= 60f)
            {
                multiplier = 1.0f;
            }
            else if (Morale >= 40f)
            {
                multiplier = 0.9f;
            }
            else // Moraali alle 40:
            {
                multiplier = 0.8f;
            }
            return Mathf.Max(Military * MilitaryTech * multiplier, 1f) + cheatPowerBonus;
        }
    }

    // Lisää armeijan pienennyksen prosentti, oletuksena 100% (ei pienennystä)
    public float ArmyReductionPercentage { get; set; } = 100f;

    // Lisää metodi palauttamaan armeijan pienennysprosentti 100%:iin
    public void ResetArmyReduction()
    {
        ArmyReductionPercentage = 100f;
    }

    public Color EmpireColor { get; set; } = Color.red; //Aluksi pelaajan valtakunta on vaikka punanen

    public float IncomeBonusPerTurn { get; set; } = 0;

    public int TurnsSinceAttack { get; set; } = 0;

    // Lisätään lippu tehostuksen aktivoitumiselle
    public bool IsValtakuntaTehostusActive { get; set; } = false;
    public int AttacksThisTurn { get; set; } = 0;

    // Teknologiatason raja-arvot ja niihin liittyvät tapahtumat
    private Dictionary<int, List<Action>> techThresholds = new Dictionary<int, List<Action>>();

    // BKT-tehokkuus
    public float GDPEfficiency { get; private set; } = 0.01f;

    // Kasvumodifikaattorit
    public float PopulationGrowthMultiplier { get; private set; } = 1.0f;
    public float GDPEfficiencyMultiplier { get; private set; } = 1.0f;

    public float LastTurnPopulationGrowth { get; private set; } = 0f;

    // Väliaikaiset kasvumodifikaattorit
    private float temporaryPopulationGrowthMultiplier = 1.0f;
    private float temporaryGDPEfficiencyMultiplier = 1.0f;

    // Esimerkki "Viljasiilot" -buffista, joka kestää 3 seuraavaa vuoroa
    public int viljasiilotBuffTurns = 0; // Kuinka monella vuorolla vielä +2% lisäkasvu

    // Esimerkki, jos haluat että "Laajentamisen tehostus" on jo aktivoitu
    // ja "Valtakunnan laajentaminen" -kortti valtaa 5 ruutua tms.
    // Laita boolean. Täällä esimerkissä ei ole toteutettu laajennuslogiikkaa.
    public bool isExpansionBuff5Active = false;
    public bool isExpansionBuff10Active = false; // Voimakas laajennus
    public bool isSecondEraExpansionBuffActive = false;

    // Uskonnon tallentaminen; aluksi null, kunnes uskontoa kehitetään
    public Religion Religion { get; set; } = null;

    public bool HasIronCard { get; set; } = false;

    public float TradeIncome { get; set; } = 0f;


    public bool blackDeathOccurred = false;
    // Kategorian painotukset
    public Dictionary<string, float> CategoryWeights { get; private set; } = new Dictionary<string, float>
    {
        { "Teknologia", 20f },
        { "Hyvinvointi", 20f },
        { "Infrastruktuuri", 20f },
        { "Uskonto/Kulttuuri", 20f },
        { "Armeija", 20f }
    };

    public Nation()
    {
        Population = 0;  // Aloitusväkiluku
        GDP = 10f;           // Aloitus-BKT
        Military = 3000;     // Aloitusarmeija
        Technology = 0;       // Teknologiataso
        Morale = 80f;         // Aloitusmoraali    

        Name = GenerateRandomNationName();
    }

    private string GenerateRandomNationName()
    {
        return NationNameGenerator.GetUniqueName();
    }

    public void AddTechThreshold(int threshold, Action onThresholdReached)
    {
        if (!techThresholds.ContainsKey(threshold))
        {
            techThresholds[threshold] = new List<Action>();
        }
        techThresholds[threshold].Add(onThresholdReached);
    }

    // Armeijan kulutusparametri: kuinka paljon armeija kuluttaa per yksikkö
    public float MilitaryConsumptionRate { get; set; } = 0.001f; // Eli 1000 yksikköä kuluttaa 1 rahan.

    // Metodi kategorian painon muuttamiseen
    public void AdjustCategoryWeight(string category, float adjustment)
    {
        if (CategoryWeights.ContainsKey(category))
        {
            CategoryWeights[category] += adjustment;
            CategoryWeights[category] = Mathf.Clamp(CategoryWeights[category], 5f, 60f);
            NormalizeCategoryWeights();
            //Debug.Log($"Kategorian '{category}' paino nyt: {CategoryWeights[category]}%");
        }
        else
        {
            //Debug.LogWarning($"Kategoriaa '{category}' ei löydetty.");
        }
    }



    // Varmistaa, että kaikkien kategorioiden yhteissumma on 100%
    private void NormalizeCategoryWeights()
    {
        float total = CategoryWeights.Values.Sum();
        if (total != 100f)
        {
            foreach (var key in CategoryWeights.Keys.ToList())
            {
                CategoryWeights[key] = (CategoryWeights[key] / total) * 100f;
            }
        }
    }

    // Metodi pysyvien kasvumodifikaattoreiden lisäämiseen
    public void AddPopulationGrowthMultiplier(float multiplier)
    {
        PopulationGrowthMultiplier += multiplier;
        //Debug.Log($"Väkiluvun kasvumodifikaattori lisätty: {multiplier}. Uusi kerroin: {PopulationGrowthMultiplier}");
    }

    public void AddGDPEfficiencyMultiplier(float multiplier)
    {
        GDPEfficiencyMultiplier += multiplier;
        //Debug.Log($"BKT-tehokkuuden kasvumodifikaattori lisätty: {multiplier}. Uusi kerroin: {GDPEfficiencyMultiplier}");
    }

    // Metodit väliaikaisten kasvumodifikaattoreiden lisäämiseen
    public void AddTemporaryPopulationGrowthMultiplier(float multiplier)
    {
        temporaryPopulationGrowthMultiplier += multiplier;
        //Debug.Log($"Väkiluvun väliaikainen kasvumodifikaattori lisätty: {multiplier}. Uusi väliaikainen kerroin: {temporaryPopulationGrowthMultiplier}");
    }

    public void AddTemporaryGDPEfficiencyMultiplier(float multiplier)
    {
        temporaryGDPEfficiencyMultiplier += multiplier;
        //Debug.Log($"BKT-tehokkuuden väliaikainen kasvumodifikaattori lisätty: {multiplier}. Uusi väliaikainen kerroin: {temporaryGDPEfficiencyMultiplier}");
    }

    // Metodi väliaikaisten modifikaattorien nollaamiseen
    public void ResetTemporaryGrowthMultipliers()
    {
        temporaryPopulationGrowthMultiplier = 1.0f;
        temporaryGDPEfficiencyMultiplier = 1.0f;
        //Debug.Log("Väliaikaiset kasvumodifikaattorit nollattu.");
    }

    // Jos haluat korottaa MilitaryTechiä vain korteilla, niin ehkä erillisen metodin
    public void SetMilitaryTech(float newValue)
    {
        MilitaryTech = newValue;
        //Debug.Log("MilitaryTech asetettu arvoon: " + MilitaryTech);
    }

    // Metodi armeijan kulutuksen laskemiseen ja GDP:n vähentämiseen
    public float CalculateMilitaryExpenditure()
    {
        float expenditure = Military * MilitaryConsumptionRate;
        //Debug.Log($"Armeijan kulutus lasketaan: {expenditure:F2} (ei vähennetä GDP:stä tässä).");
        return expenditure;
    }

    // Kutsutaan vuoron ALUSSA
    public void ResetLastTurnPopulationGrowth()
    {
        LastTurnPopulationGrowth = 0f;
    }

    // Kutsutaan laatan valloituksessa
    public void AddPopulationGrowth(float amount)
    {
        Population += amount;
        LastTurnPopulationGrowth += amount;
        // -> nyt valloitusluku kertyy kasvuun
    }

    public float viljasiilotBonusPerTurn = 0f;
    public int maatalousBuffTurns = 0;
    public float maatalousBonusPerTurn = 0f;

    public void ApplyBasicGrowth(SquareGrid grid)
    {
        float previousPopulation = Population;

        // 1) Mahdolliset viljasiilot-buffit
        float localPopMultiplier = 1.0f;
        if (viljasiilotBuffTurns > 0)
        {
            AddPopulationGrowth(viljasiilotBonusPerTurn);
            viljasiilotBuffTurns--;
        }

        if (maatalousBuffTurns > 0)
        {
            AddPopulationGrowth(maatalousBonusPerTurn);
            maatalousBuffTurns--;
        }

        // 2) Peruskasvu: päivitetään väkiluku
        Population *= 1.02f
            * PopulationGrowthMultiplier
            * temporaryPopulationGrowthMultiplier
            * localPopMultiplier;

        float thisTurnBasicGrowth = Population - previousPopulation;
        LastTurnPopulationGrowth += thisTurnBasicGrowth;

        // 3) Lisätään ruutujen tuottama tulo (esim. grassruutu antaa 0.25 rahaa)
        float tileIncome = CalculateTileIncome(grid);
        GDP += tileIncome;

        // Lisätään väkiluvun tulo: 1 tulo per 150 000 asukasta
        float populationIncome = Population / 150000f;
        GDP += populationIncome;

        // 5) Lisätään korttien vaikutuksesta saatava tulo
        GDP += IncomeBonusPerTurn;

        float militaryExpenditure = CalculateMilitaryExpenditure();
        GDP -= militaryExpenditure;
        if (GDP < 0f)
        {
            GDP = 0f;
            //Debug.LogWarning("GDP on loppunut armeijan kulutuksen vuoksi!");
        }

        // Lasketaan tämän vuoron nettotulot (sisältäen myös TradeIncome ja armeijan pienennyksen säästöt)
        float netIncome = (tileIncome + populationIncome + IncomeBonusPerTurn + TradeIncome)
                            - militaryExpenditure + AccumulatedArmyReductionIncome;
        TurnNetIncome = netIncome;
        LastTurnNetGDPGrowth = netIncome;
        TradeIncome = 0f;

        // 7) Nollataan väliaikaiset kasvumodifikaattorit
        temporaryPopulationGrowthMultiplier = 1.0f;
        temporaryGDPEfficiencyMultiplier = 1.0f;

        EnforceMilitaryLimit();
        EnforcePopulationLimit(grid);

        if (GDP <= 0f)
        {
            int reducedMilitary = Mathf.RoundToInt(Military * 0.67f);
            Military = reducedMilitary;
            DecreaseMorale(5f);
            //Debug.Log("Rahaa on 0, joten moraali tippuu 5 pistettä ja armeijan määrä tippuu.");
        }

        //Debug.Log($"ApplyBasicGrowth -> Väkiluku: {Population:F0}, BKT: {GDP:F2}, Kasvu (tältä vuorolta): {thisTurnBasicGrowth:F0}");
    }

    public void IncreaseGDPEfficiency(float amount)
    {
        GDPEfficiency += amount;
        //Debug.Log($"GDP-tehokkuus kasvoi {amount:F4}. Uusi: {GDPEfficiency:F4}");
    }

    public void DecreaseGDPEfficiency(float amount)
    {
        GDPEfficiency = Mathf.Max(0f, GDPEfficiency - amount);
        //Debug.Log($"GDP-tehokkuus pieneni {amount:F4}. Uusi: {GDPEfficiency:F4}");
    }

    // Lisää metodit moraalin lisäämiseen ja vähentämiseen
    public void IncreaseMorale(float amount)
    {
        Morale += amount;
    }

    public void DecreaseMorale(float amount)
    {
        Morale -= amount;
    }

    public void IncreaseGDPbyPercent(float percent)
    {
        GDP *= (1.0f + percent);
        //Debug.Log($"GDP kasvoi {percent * 100}%");
    }

    public void ResetLastTurnNetGDPGrowth()
    {
        LastTurnNetGDPGrowth = 0f;
    }

    public void IncreasePopulationGrowthByPercent(float percent)
    {
        // Jos haluat suoraan skaalata, esim. +3% -> kerrotaan 1.03f
        // Täällä käytän pysyvää kerrointa
        AddPopulationGrowthMultiplier(percent);
    }


    // Kertyneet armeijan pienennyssäästöt nykyiseltä vuorolta
    public float AccumulatedArmyReductionIncome { get; private set; } = 0f;

    //  Edellisen vuoron armeijan pienennyksen säästöt
    public float PreviousArmyReductionIncome { get; set; } = 0f;

    // Metodi, joka nollaa kuluvan vuoron armeijan pienennyksen säästöt
    public void ResetArmyReductionIncome()
    {
        AccumulatedArmyReductionIncome = 0f;
    }

    // Esimerkki metodista, joka soveltaa armeijan pienennystä
    public void ApplyArmyReduction()
    {
        int oldMilitary = Military;
        float reductionFactor = Mathf.Clamp(ArmyReductionPercentage, 0f, 100f) / 100f;
        int newMilitary = Mathf.RoundToInt(Military * reductionFactor);
        int reductionAmount = Military - newMilitary;

        Military = newMilitary;

        float saving = reductionAmount * MilitaryConsumptionRate;
        AccumulatedArmyReductionIncome += saving;

        //Debug.Log($"Armeija pienennetty {100 - ArmyReductionPercentage}%: {reductionAmount} yksikköä poistettu. Uusi armeijan koko: {Military}. Säästö: {saving:F2}");
    }

    public float TurnNetIncome { get; set; }
    public void AddNetGDPGrowth(float amount)
    {
        GDP += amount;
        LastTurnNetGDPGrowth += amount; // 
        //Debug.Log($"BKT:n kasvu lisätty: +{amount}.");
    }

    public void EnforceMilitaryLimit()
    {
        int maxAllowed = Mathf.FloorToInt(Population * 0.25f);
        if (Military > maxAllowed)
        {
            Military = maxAllowed;
            //Debug.Log($"Armeijan määrä ylitti 25% rajan. Uusi armeijan määrä: {Military}");
        }
    }

    // Voi lisätä myös metodin uskonnon kehittämiselle, esim.
    public void DevelopReligion(string religionName, Color color)
    {
        if (Religion == null)
        {
            Religion = new Religion(religionName, color);
            //Debug.Log($"Uskonto kehitetty: {religionName}");
        }
        else
        {
            //Debug.Log("Uskonto on jo kehitetty.");
        }
    }

    public float CalculateTileIncome(SquareGrid grid)
    {
        float totalIncome = 0f;
        // Käydään läpi kaikki pelaajan hallitsemat ruudut
        foreach (SquareTile tile in grid.GetControlledTiles().Where(t => t.controllingNation == this))
        {
            totalIncome += tile.GetTileIncome();
        }
        return totalIncome;
    }

    public void InitializeStartingValues(float startingPopulation, int startingMilitary)
    {
        // Asetetaan väkiluku ensin
        Population = startingPopulation;
        // Sitten asetetaan armeija – Military-setterissä varmistetaan, ettei se ylitä 25 % väkiluvusta.
        Military = startingMilitary;
    }

    public void EnforcePopulationLimit(SquareGrid grid)
    {
        // Lasketaan hallittujen laattojen määrä
        int controlledTileCount = grid.GetControlledTiles().Count(t => t.controllingNation == this);
        // Maksimi populaatio: 10 miljoonaa per hallittu laatta
        float maxPopulation = controlledTileCount * 10000000f;

        // Jos populaatio ylittää sallitun rajan, rajoitetaan se
        if (Population > maxPopulation)
        {
            Population = maxPopulation;
            //Debug.Log($"[{Name}] Väkiluku rajoitettu maksimissaan: {maxPopulation}");
        }

        // Jos populaatio on 0 tai alle, poistetaan kaikki hallitut ruudut
        if (Population <= 0)
        {
            //Debug.Log($"[{Name}] Väkiluku on 0, valtio menettää kaikki ruudut ja poistuu pelistä.");
            // Poistetaan hallitut ruudut
            foreach (SquareTile tile in grid.GetControlledTiles().Where(t => t.controllingNation == this).ToList())
            {
                tile.SetControlled(false, null, false);
            }
            // Voi asettaa jonkin lipun tai kutsua metodia, joka hoitaa valtion poistamisen pelistä.
        }
    }


    /// <summary>
    /// Päivittää valtion virallisen uskonnon sen hallitsemista ruuduista löytyvän enemmistöuskonnon mukaisesti.
    /// Jos enemmistön uskonto eroaa aiemmin asetetusta, kutsutaan DiplomacyManagerin päivitysmetodia.
    /// </summary>
    public void UpdateMajorityReligion(SquareGrid grid)
    {
        List<SquareTile> controlledTiles = grid.GetControlledTiles()
            .Where(t => t.controllingNation == this)
            .ToList();

        if (controlledTiles.Count == 0)
            return;

        var religionGroups = controlledTiles
            .Where(t => t.Religion != null)
            .GroupBy(t => t.Religion.Name)
            .Select(g => new { Religion = g.First().Religion, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (religionGroups.Count == 0)
            return;

        Religion majorityReligion = religionGroups[0].Religion;

        // Jos nykyinen uskonto on null tai eri kuin enemmistön uskonto, päivitetään ja nollataan flagit
        if (this.Religion == null || this.Religion.Name != majorityReligion.Name)
        {
            this.Religion = majorityReligion;
            //Debug.Log($"Nation {this.Name} majority religion updated to {majorityReligion.Name}");
            // Nollataan aikaisemmin sovellettu uskonnollinen vaikutus
            LastAppliedReligionDiplomacy = null;
            // Tyhjennetään flagit, koska uskonto on muuttunut
            ReligiousEffectApplied.Clear();
        }

        // Sovelletaan uskonnollinen vaikutus vain, jos sitä ei ole jo sovellettu tämän valtion osalta
        if (LastAppliedReligionDiplomacy == null || LastAppliedReligionDiplomacy != this.Religion.Name)
        {
            DiplomacyManager.Instance.UpdateReligionRelationshipsForNation(this);
            LastAppliedReligionDiplomacy = this.Religion.Name;
        }
    }

    public Dictionary<Nation, bool> ReligiousEffectApplied { get; private set; } = new Dictionary<Nation, bool>();
    public string LastAppliedReligionDiplomacy { get; set; } = null;

    public int IronTradeTurnsRemaining { get; set; } = 0;
    public Nation IronTradeSeller { get; set; } = null;
    public bool HasActiveIronTrade => IronTradeTurnsRemaining > 0;

    public static void ProcessIronTradeForNation(Nation nation)
    {
        if (nation.HasActiveIronTrade)
        {
            // Ostaja maksaa 1, myyjä saa 1 joka vuoro
            nation.GDP -= 1;
            if (nation.IronTradeSeller != null)
            {
                nation.IronTradeSeller.GDP += 1;
            }
            nation.IronTradeTurnsRemaining--;
            if (nation.IronTradeTurnsRemaining <= 0)
            {
                nation.IronTradeSeller = null;
            }
        }
    }

    public static void ProcessPeaceBonus()
    {
        // Käytetään DiplomacyManagerin kautta rekisteröityjä kansoja
        foreach (Nation nation in DiplomacyManager.Instance.GetAllRegisteredNations())
        {
            nation.TurnsSinceAttack++; // Oletetaan, että tämä kenttä on lisätty Nationiin

            if (nation.TurnsSinceAttack >= 20 && UnityEngine.Random.value < 0.5f)
            {
                foreach (Nation other in DiplomacyManager.Instance.GetAllRegisteredNations())
                {
                    if (other != nation)
                    {
                        DiplomacyManager.Instance.AdjustRelationship(nation, other, +1);
                    }
                }
                nation.TurnsSinceAttack = 0;
            }
        }
    }

    public float DefenseBonus { get; set; } = 0f;

    //näitä käytetään tarkistamaan että pelaaja ei voi saada "liikaa" castleja ja porteja"
    public int PortCardActivations { get; set; } = 0;
    public int CastleCardActivations { get; set; } = 0;
    //tallennetaan se vuoro, jolloin valtio syntyi sisällissodan seurauksena.
    public int CivilWarTurnCreated { get; set; } = -1;
    //katotaan että onko tekoäly aktiivinen. käytetään diplomacy managerissa
    public bool IsActive { get; set; } = true;

    //liitosta lähtenyt ei voi liittyä samaan 10 vuoroon, se alustetaan täällä
    public int allianceJoinCooldown { get; set; } = 0;

    //valtakunnan tulot:
    public float CalculateCurrentNetIncome(SquareGrid grid)
    {
        float tileIncome = CalculateTileIncome(grid);
        float populationIncome = Population / 150000f;
        float income = tileIncome + populationIncome + IncomeBonusPerTurn + TradeIncome;
        float militaryExpenditure = CalculateMilitaryExpenditure(); 
        float netIncome = income - militaryExpenditure + AccumulatedArmyReductionIncome;
        return netIncome;
    }

    private readonly Guid id = Guid.NewGuid();
    public Guid Id => id;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;
        if (obj is Nation other)
            return id.Equals(other.id);
        return false;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }

    //Tästä alas on sisällisodasta syntyvän valtion alustustusta:

    public bool isCivilWarNation { get; set; } = false;
    public void InitializePopulation(float initialPopulation)
    {
        Population = initialPopulation;
    }

    public void InitializeCivilWarNation(float inheritedPopulation)
    {
        Population = inheritedPopulation;
    }

    public void InitializeCivilWarNation()
    {
        isCivilWarNation = true;
        Population = 5000000f;
        // Määrittele muut halutut arvot sisällissotavaltioille  
        GDP = 10f;
        Military = 3000;
        Morale = 60f;
        //ja loput tänne jos tarpee
    }


}
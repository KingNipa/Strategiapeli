using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardDeck : MonoBehaviour
{
    [HideInInspector]
    public List<Card> deck = new List<Card>(); // Varsinainen pakka

    private List<Card> discardPile = new List<Card>(); // Käytettyjen korttien pino
    public Nation playerNation; // Asetetaan GameManagerista
    private SquareGrid squareGrid;

    // Kaikki ensimmäisen aikakauden kortit (21 kpl)
    private List<Card> allFirstEraCards = new List<Card>();

    // Unique-kortit, jotka on jo käytetty
    public HashSet<string> usedUniques = new HashSet<string>();


    // Harvinaisuusjakauma
    private float normalChance = 0.60f;
    private float rareChance = 0.33f;
    private float epicChance = 0.05f;
    private float legendaryChance = 0.02f;

    

void Awake()
{
        //tää lähtee pois kun siirtettiin alustus toisee paikkaa (vältetään tupla-alustukset)
        /*
    if (SceneManager.GetActiveScene().name != "GameScene")
    {
        return;
    }

    squareGrid = FindObjectOfType<SquareGrid>();
    if (squareGrid == null)
    {
        //Debug.LogError("SquareGrid-komponenttia ei löydy!");
    }

    InitializeFirstEraCards();
    deck.AddRange(allFirstEraCards);
    ShuffleDeck();
        */
}

void Start()
{
    if (SceneManager.GetActiveScene().name != "GameScene")
    {
        return;
    }

    if (playerNation == null)
    {
        //Debug.LogError("playerNation ei ole asetettu CardDeckille.");
    }
}

    public void InitializeDeck()
    {
        squareGrid = FindObjectOfType<SquareGrid>();
        if (squareGrid == null)
        {
            //Debug.LogError("SquareGrid-komponenttia ei löydy!");
        }

        // Tyhjennä mahdolliset aiemmat kortit (varmuuden vuoksi)
        deck.Clear();
        usedUniques.Clear();

        InitializeFirstEraCards();
        deck.AddRange(allFirstEraCards);
        ShuffleDeck();
    }

    /// <summary>
    /// Luodaan 21 ensimmäisen aikakauden korttia (listaan allFirstEraCards).
    /// </summary>
    private void InitializeFirstEraCards()
    {
        // 1) Teknologiset innovaatiot
        allFirstEraCards.Add(new Card(
            "Technological Innovations",
            "Sets the technology level to 1.",
            Rarity.Epic,
            true, // uniikki
            0,    // ei vaadi teknologiaa
            (nation, grid) =>
            {
                if (nation.Technology < 1)
                    nation.Technology = 1;
                //Debug.Log("Teknologiset innovaatiot aktivoitu! Technology = 1.");
            }
        ));

        // 2) Miekkamiehet
        allFirstEraCards.Add(new Card(
            "Swordsmen",
            "Greatly increases army power, but also raises maintenance costs.",
            Rarity.Rare,
            true,
            1,
            (nation, grid) =>
            {
                nation.SetMilitaryTech(0.1f);
                nation.MilitaryConsumptionRate *= 1.5f; // Lisää ylläpitokustannusta 1.5-kertaiseksi
                //Debug.Log("Miekkamiehet aktivoitu! MilitaryTech = 0.1f.");
            }
        ));

   

        // 4) Laajentamisen tehostus (1->5)
        allFirstEraCards.Add(new Card(
            "Expansion Boost",
            "The Expand card now captures 5 new territories instead of 1.",
            Rarity.Rare,
            true,
            1,
            (nation, grid) =>
            {
                nation.isExpansionBuff5Active = true;
                //Debug.Log("Laajentamisen tehostus (1->5) aktivoitu.");
            }
        ));

        // 5) Voimakas laajentamisen tehostus (1->10)
        allFirstEraCards.Add(new Card(
            "Major Expansion Boost",
            "Allows expansion to capture up to 10 new territories.",
            Rarity.Legendary,
            true,
            1,
            (nation, grid) =>
            {
                nation.isExpansionBuff10Active = true;

                Card minorExpansionBoost = deck.Find(card => card.Name == "Expansion Boost");
                if (minorExpansionBoost != null)
                {
                    // Poista kortti pakasta, jotta sitä ei voi käyttää myöhemmin
                    deck.Remove(minorExpansionBoost);

                }
            }
        ));

        allFirstEraCards.Add(new Card(
    "Infrastructure",
    "Increases income for each tile you control.",
    Rarity.Normal,
    false,
    0,
    (nation, grid) =>
    {
        int tileCount = grid.GetControlledTiles().Count(t => t.controllingNation == nation);
        nation.AddNetGDPGrowth(tileCount * 0.025f);
        //Debug.Log("Tieverkoston kehittyminen: Nettotulo kasvaa 0.025 per hallussa oleva laatta.");
    }
        ));

        // 7) Viljasiilot (Rare, Tech=1)
        allFirstEraCards.Add(new Card(
    "Grain silos",
    "Boosts population growth for 3 turns.",
    Rarity.Rare,
    false,
    1,
    (nation, grid) =>
    {
        // Asetetaan buffin kesto
        nation.viljasiilotBuffTurns = 3;
        // Asetetaan bonus, joka lisätään joka vuoro (10 000 asukasta)
        nation.viljasiilotBonusPerTurn = 10000f;
        //Debug.Log("Viljasiilot aktivoitu: Väkilukua nostetaan 10k per vuoro 3 vuoron ajan.");
    }
));


        allFirstEraCards.Add(new Card(
    "Grainfield",
    "Boosts population.",
    Rarity.Normal,
    false,
    0,
    (nation, grid) =>
    {
        nation.AddPopulationGrowth(20000f);
        //Debug.Log("Pelto: Väkiluku nostettu 20k.");
    }
));


        // 9) Kaivos (Common)
        allFirstEraCards.Add(new Card(
            "Mine",
            "Increases the realm’s income.",
            Rarity.Normal,
            false,
            0,
            (nation, grid) =>
            {
                nation.IncomeBonusPerTurn += 0.1f; 
            }
        ));

        // 10) Kaupunki (Legendary, Tech=1)
        allFirstEraCards.Add(new Card(
            "Town",
            "Increases the realm’s income.",
            Rarity.Legendary,
            true,
            1,
            (nation, grid) =>
            {
                nation.IncomeBonusPerTurn += 5;


                // Valitaan satunnainen hallittu ruutu, johon asetetaan kaupunki
                SquareTile freeTile = grid.GetControlledTiles()
                    .Where(t => t.Terrain != SquareTile.TerrainType.MakeaVesi && t.controllingNation == nation && !t.HasCity && !t.HasCastle && !t.HasPort)
                    .OrderBy(t => UnityEngine.Random.value)
                    .FirstOrDefault();

                if (freeTile != null)
                {
                    freeTile.HasCity = true;
                    //Debug.Log($"Kaupunki perustettu ruutuun ({freeTile.X}, {freeTile.Y}).");
                }
                else
                {
                    //Debug.LogWarning("Ei vapaita ruutuja, joihin kaupunki voidaan perustaa.");
                }
            }
            
        ));

        // 11) Moraali 1 (Common)
        allFirstEraCards.Add(new Card(
            "Morale Small",
            "Increases morale by +2.",
            Rarity.Normal,
            false,
            0,
            (nation, grid) =>
            {
                nation.IncreaseMorale(2f);
                //Debug.Log("Moraali 1: +2 moraalia.");
            }
        ));

        // 12) Moraali 2 (Rare)
        allFirstEraCards.Add(new Card(
            "Morale Medium",
            "Increases morale by +4.",
            Rarity.Rare,
            false,
            0,
            (nation, grid) =>
            {
                nation.IncreaseMorale(4f);
                //Debug.Log("Moraali 2: +4 moraalia.");
            }
        ));

        // 13) Moraali 3 (Epic)
        allFirstEraCards.Add(new Card(
            "Morale High",
            "Increases morale by +10.",
            Rarity.Epic,
            false,
            0,
            (nation, grid) =>
            {
                nation.IncreaseMorale(10f);
                //Debug.Log("Moraali 3: +8 moraalia.");
            }
        ));

        allFirstEraCards.Add(new Card(
     "Iron",
     "Reveals iron deposits on the map.",
     Rarity.Rare,
     true,  // Uniikki: tätä korttia ei saa uudestaan
     1,     // Vaatimus: Teknologia taso 1
     (nation, grid) =>
     {
        // Merkitään, että pelaaja on saanut rautakortin.
        nation.HasIronCard = true;

        // Lisäys: pakotetaan Resources-button päälle (ON-asento)
        if (GameManager.Instance != null)
         {
             GameManager.Instance.showStrategicResources = true;
            // Päivitetään karttanäkymä, jotta muutokset tulevat heti näkyviin.
            SquareGrid gridInstance = GameObject.FindObjectOfType<SquareGrid>();
             if (gridInstance != null)
             {
                 gridInstance.UpdateMapView();
             }
         }

        // Päivitetään kaikki pelilaudalla olevat ruudut, joissa on rautaa,
        // jotta markerit tulevat näkyviin.
        foreach (SquareTile tile in grid.GetAllTiles())
         {
             if (tile.HasIron)
             {
                 tile.UpdateVisuals();
             }
         }
     }
 ));


        // 14) Kortti "Uskonnon kehittäminen":
        allFirstEraCards.Add(new Card(
            "Religion",
            "Establishes a religion.",
            Rarity.Epic, 
            true,
            0,
            (nation, grid) =>
            {
                // Jos uskontoa ei vielä ole, luodaan se
                if (nation.Religion == null)
                {
                    UpdateReligionCards();
                    nation.IncreaseMorale(2f);
                    // Generoidaan satunnainen väri (varmistetaan, ettei liian sininen)
                    Color religionColor = new Color(
                        UnityEngine.Random.Range(0.2f, 1f),
                        UnityEngine.Random.Range(0.2f, 1f),
                        UnityEngine.Random.Range(0.2f, 1f)
                    );
                    nation.DevelopReligion("Uskonto A", religionColor);
                    //Debug.Log("Uskonnon kehittäminen aktivoitu – uusi uskonto: Uskonto A");
                }

                // Haetaan pelaajan hallitsemat ruudut
                List<SquareTile> controlled = grid.GetControlledTiles()
                    .Where(tile => tile.controllingNation == nation)
                    .ToList();

                // Asetetaan kaikkiin hallittuihin ruutuihin sama uskonto
                foreach (SquareTile tile in controlled)
                {
                    tile.Religion = nation.Religion;
                    tile.SetVisibility(true, true);
                }

                // Päivitetään majority religion ja diplomatiapisteet
                nation.UpdateMajorityReligion(grid);
                DiplomacyManager.Instance.UpdateReligionRelationshipsForNation(nation);
            }
));


        // 15) Pyhiinvaellus 1 (common)
        allFirstEraCards.Add(new Card(
    "Religious festivals",
    "Spreads religion to nearby tiles and raises morale.",
    Rarity.Normal,
    false,
    0,
    (nation, grid) =>
    {
        if (nation == null)
        {
            return;
        }

        ReligionManager.SpreadReligionDigging(grid, nation, 20);
      //  nation.UpdateMajorityReligion(grid);
        DiplomacyManager.Instance.UpdateReligionRelationshipsForNation(nation);
        nation.IncreaseMorale(2f);
    }
));

        // 16) Pyhiinvaellus 2 (Rare)
        // Pyhiinvaellus medium -kortti: levittää uskontoa 35 ruuduun
        allFirstEraCards.Add(new Card(
    "Pilgrimage",
    "Spreads religion effectively and raises morale slightly.",
    Rarity.Rare,
    false,
    0,
    (nation, grid) =>
    {
        if (nation == null)
        {
            return;
        }

        ReligionManager.SpreadReligionDigging(grid, nation, 35);
       // nation.UpdateMajorityReligion(grid);
        DiplomacyManager.Instance.UpdateReligionRelationshipsForNation(nation);
        nation.IncreaseMorale(1f);
    }
));


        // 17) Armeijan kehitys (pieni) - Rare
        allFirstEraCards.Add(new Card(
    "Army Recruitment",
    "Adds to your army based on 15% of its current size.",
    Rarity.Normal,
    false,
    0,
    (nation, grid) =>
    {
        int add = Mathf.RoundToInt(nation.Military * 0.15f);
        nation.Military += add;
        //Debug.Log($"Armeijan kehitys (pieni): +{add} yksikköä.");
    }
));
        // 18) Armeijan kehitys (keskisuuri) - rare
        allFirstEraCards.Add(new Card(
            "Army Recruitment 2",
            "Adds to your army based on 50% of its current size.",
            Rarity.Rare,
            false,
            0,
            (nation, grid) =>
            {
                int add = Mathf.RoundToInt(nation.Military * 0.50f);
                nation.Military += add;
                //Debug.Log($"Armeijan kehitys (keskisuuri): +{add} yksikköä.");
            }
        ));

        // 19) Armeijan kehitys (suuri) - Epic
        allFirstEraCards.Add(new Card(
            "Huge Army Recruitment",
            "Adds significantly to your army. Doubles its current size.",
            Rarity.Epic,
            false,
            0,
            (nation, grid) =>
            {
                int add = Mathf.RoundToInt(nation.Military * 1.0f);
                nation.Military += add;
                //Debug.Log($"Armeijan kehitys (suuri): +{add} yksikköä.");
            }
        ));

        // 20) Valtakunnan laajentaminen - rare
        allFirstEraCards.Add(new Card(
            "Expand the Realm",
            "Adds new territories to your realm.",
            Rarity.Rare,
            false,
            0,
            (nation, grid) =>
            {
                // Oletuksena laajennetaan 1 ruutu, mutta tarkistetaan onko tehostus aktiivinen
                int expansions = 1;
                if (nation.isExpansionBuff10Active)
                {
                    expansions = 10;
                }
                else if (nation.isExpansionBuff5Active)
                {
                    expansions = 5;
                }

                // Suoritetaan laajennus expansions-kertaa
                for (int i = 0; i < expansions; i++)
                {
                    SquareTile newlyControlled = grid.ExpandTerritoryForNation(nation);
                    if (newlyControlled == null)
                    {
                        //Debug.Log("Ei enää laajennettavia reuna-alueita!");
                        break;
                    }

                    // 50% mahdollisuus pudottaa moraalia
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        // Jos laajentamisen tehostuskortti on aktiivinen, pudotus on 1, muuten 2
                        int moraleDrop = (nation.isExpansionBuff5Active || nation.isExpansionBuff10Active) ? 1 : 2;
                        nation.DecreaseMorale(moraleDrop);
                        //Debug.Log($"Laajennuksen seurauksena moraalia pudotettu: -{moraleDrop} pistettä.");
                    }
                }
            }
        ));


        // 21) Löytöretket (Common)
        allFirstEraCards.Add(new Card(
            "Expeditions",
            "Reveals unexplored areas on the map.",
            Rarity.Normal,
            false,
            0,
            (nation, grid) =>
            {
                //Debug.Log("Löytöretket aktivoitu. Avaa 25 satunnaista ruutua."); //tässä pitäis olla 25!!

                // Call a method to reveal 25 random tiles around the player
                RevealDeepTiles(grid, 25, nation);
            }
        ));
    }

    // Uusi metodi keskiajan korttien alustamiseen
    public void InitializeSecondEraCards()
    {
        List<Card> secondEraCards = new List<Card>();

        // Oletetaan, että allFirstEraCards sisältää kaikki ensimmäisen aikakauden kortit
        // Ja että käytettyjä uniikkeja on merkitty käytetyiksi (usedUniques)
        // Tässä kopioidaan ensimmäisen aikakauden uniikit kortit, joita ei käytetty,
        // mutta lasketaan niiden harvinaisuusluokka yhden tason helpommaksi.
        foreach (var card in allFirstEraCards)
        {
            if (card.IsUnique && !usedUniques.Contains(card.Name))
            {
                // Poikkeus: Uskonnon kehittäminen jätetään kokonaan pois
                if (card.Name == "Uskonnon kehittäminen")
                    continue;

                // Luodaan kopio kortista ja lasketaan harvinaisuusluokka yhden tason alemmaksi.
                Card downgradedCard = new Card(
                    card.Name,
                    card.Description,
                    DowngradeRarity(card.Rarity),  // Toteuta DowngradeRarity-metodi
                    card.IsUnique,
                    card.RequiredTechLevel,  // Voi myös säätää tech-vaatimusta, jos tarpeen
                    card.Effect
                );
                secondEraCards.Add(downgradedCard);
            }
        }

        // Lisätään keskiajan uudet kortit

        // 1. Teknologiset innovaatiot: nostaa teknologiatason 2.
        secondEraCards.Add(new Card(
            "Technological Innovations 2",
            "Sets the technology level to 2.",
            Rarity.Rare,
            true,
            1,
            (nation, grid) =>
            {
                if (nation.Technology < 2)
                    nation.Technology = 2;
                //Debug.Log("Keskiajan Teknologiset innovaatiot aktivoitu! Teknologiatason nosto tasolle 2.");
            }
        ));

        // 2. Armeijan kehityskortti: nostaa MilitaryTechin 0.1 -> 0.20.
        secondEraCards.Add(new Card(
            "Military innovations",
            "Increases military efficiency.",
            Rarity.Rare,
            true,
            2,
            (nation, grid) =>
            {
                nation.SetMilitaryTech(0.20f);
                nation.MilitaryConsumptionRate *= 1.5f;
                //Debug.Log("Keskiajan Armeijan kehityskortti aktivoitu! MilitaryTech nostettu 0.25:een.");
            }
        ));

        // 3. Linnat: 
        secondEraCards.Add(new Card(
            "Castle",
            "Grants a defense bonus until the end of the Middle Ages.",
            Rarity.Rare,
            false,
            2,
            (nation, grid) =>
            {
        // Tarkistetaan, onko Castle‑korttia jo aktivoitu 2 kertaa.
        if (nation.CastleCardActivations >= 2)
                {
                    //Debug.LogWarning("Castle-kortti on jo käytetty 2 kertaa, sitä ei voi enää aktivoida.");
                    return;
                }

                // Etsitään vapaa ruutu, jossa:
                // - Ruutu kuuluu pelaajan valtakuntaan,
                // - Ruudulla ei ole kaupunkia eikä linnaa,
                // - Ruudun Terrain ei ole MakeaVesi.
                SquareTile freeTile = grid.GetControlledTiles()
                            .Where(t => t.Terrain != SquareTile.TerrainType.MakeaVesi && t.controllingNation == nation && !t.HasCity && !t.HasCastle && !t.HasPort)
                            .OrderBy(t => UnityEngine.Random.value)
                            .FirstOrDefault();

                if (freeTile == null)
                {
                    //Debug.LogWarning("Ei vapaita ruutuja, joihin linna voidaan perustaa.");
                    return;
                }

        // Asetetaan linna-ikoni ruutuun.
        freeTile.HasCastle = true;
                //Debug.Log($"Linna perustettu ruutuun ({freeTile.X}, {freeTile.Y}).");

        // Lisätään puolustusbonus, jos vuosi on ennen 1500.
        if (GameManager.Instance.CurrentYear < 1500)
                {
                    nation.DefenseBonus += 0.05f;
                    nation.IncomeBonusPerTurn -= 1;
                    //Debug.Log("5% puolustusbonus lisätty.");
                }
                else
                {
                    //Debug.Log("Vuoden 1500 jälkeen linna ei anna puolustusbonusta, vaikka merkki näkyykin pelissä.");
                }

        // Kasvatetaan Castle‑kortin aktivointien laskuria.
        nation.CastleCardActivations++;

        // Jos korttia on aktivoitu 2 kertaa, merkitään se käytetyksi.
        if (nation.CastleCardActivations >= 2)
                {
                    CardDeck cardDeck = UnityEngine.Object.FindObjectOfType<CardDeck>();
                    if (cardDeck != null && !cardDeck.usedUniques.Contains("Castle"))
                    {
                        cardDeck.usedUniques.Add("Castle");
                        //Debug.Log("Castle-kortti on nyt käytetty 2 kertaa, sitä ei enää voi aktivoida.");
                    }
                }
            }
        ));


        // 4. Satamakaupunki: TODO, tätä vois muuttaa niin että merta tarvii olla tarpeeks (ei turhia porteja)
        secondEraCards.Add(new Card(
            "Port",
            "Establishes a port on a coastal tile and increase your income.",
            Rarity.Epic,
            false,
            2,
            (nation, grid) =>
            {
        // Tarkistetaan, onko kansalla meriraja: löytyykö vähintään yksi pelaajan hallitsema ruutu,
        // jonka naapureista löytyy Merivesi
        var seaBorderTiles = grid.GetControlledTiles()
                    .Where(t => t.controllingNation == nation && grid.GetNeighbors(t).Any(n => n.Terrain == SquareTile.TerrainType.Merivesi))
                    .ToList();

                if (seaBorderTiles.Count == 0)
                {
                    //Debug.LogWarning("Valtiolla ei ole merirajaa, joten Portia ei voi perustaa.");
                    return;
                }

        // Etsitään satunnainen pelaajan hallitsema ruutu, joka:
        // 1) EI sisällä kaupunkia (HasCity)
        // 2) EI sisällä linnaa (HasCastle)
        // 3) Ja jolla on ainakin yksi naapuri, jonka Terrain on Merivesi
        SquareTile freeTile = grid.GetControlledTiles()
                    .Where(t => t.controllingNation == nation && !t.HasCity && !t.HasCastle && !t.HasPort && grid.GetNeighbors(t).Any(n => n.Terrain == SquareTile.TerrainType.Merivesi))
                    .OrderBy(t => UnityEngine.Random.value)
                    .FirstOrDefault();

                if (freeTile != null)
                {
                    freeTile.HasPort = true;
                    nation.IncomeBonusPerTurn += 7;
                    //Debug.Log($"Port perustettu ruutuun ({freeTile.X}, {freeTile.Y}). Tulot kasvaa +7 per vuoro.");

            // Kasvatetaan aktivointien laskuria
            nation.PortCardActivations++;

            // Jos portia on aktivoitu kaksi kertaa, poistetaan kortti käytöstä
            if (nation.PortCardActivations >= 2)
                    {
                        CardDeck cardDeck = UnityEngine.Object.FindObjectOfType<CardDeck>();
                        if (cardDeck != null && !cardDeck.usedUniques.Contains("Port"))
                        {
                            cardDeck.usedUniques.Add("Port");
                            //Debug.Log("Port-kortti on nyt käytetty 2 kertaa, sitä ei enää voi aktivoida.");
                        }
                    }
                }
                else
                {
                    //Debug.LogWarning("Ei vapaita ruutuja, joihin Portia voidaan perustaa.");
                }
            }
        ));

        // 6. Laajentamisen tehostus: valtakunnan laajentaminen -15 aluetta.
        secondEraCards.Add(new Card(
          "Expansion enhancement",
           "Increases realm expansion.",
         Rarity.Epic,
         true,
         2,
         (nation, grid) =>
        {
            // Aktivoidaan second era–laajennus-tehostus
            nation.isSecondEraExpansionBuffActive = true;
            //Debug.Log("Keskiajan Laajentamisen tehostus aktivoitu: laajennus 15 ruutua kerralla.");
        }
        ));

        // 7. Löytöretket: avaa 50 satunnaista mustaa ruutua.
        secondEraCards.Add(new Card(
            "Expeditions",
            "Reveals 50 unexplored squares on the map.",
            Rarity.Normal,
            false,
            0,
            (nation, grid) =>
            {
                RevealDeepTiles(grid, 50, nation);
            }
        ));

        secondEraCards.Add(new Card(
    "Grainfield",
    "Boosts population growth.",
    Rarity.Normal,
    false,
    0,
    (nation, grid) =>
    {
        nation.AddPopulationGrowth(30000f);
        //Debug.Log("Pelto: Väkiluku nostettu 30k.");
    }
));


        // 9. Pyhiinvaellus: debug-viesti (uskonto puuttuu).
        secondEraCards.Add(new Card(
            "Pilgrimage",
            "Spreads religion to nearby tiles and raises morale slightly.",
            Rarity.Normal,
            false,
            0,
            (nation, grid) =>
            {
                ReligionManager.SpreadReligionDigging(grid, nation, 20);
                nation.IncreaseMorale(2f);
            }
        ));

        // 10. Maatalous 2% (3 vuoroa)
        secondEraCards.Add(new Card(
            "Farms",
            "Boosts population growth for 3 turns.",
             Rarity.Rare,
            false,
            0,
            (nation, grid) =>
            {
                // Asetetaan buffin kesto
                nation.maatalousBuffTurns = 3;
                // Asetetaan bonus, joka lisätään joka vuoro (30 000 asukasta)
                nation.maatalousBonusPerTurn = 20000f;
            }
));

        // 11. Maatalous 2% (5 vuoroa) – uniikki ja epic
        secondEraCards.Add(new Card(
            "Agricultural development",
            "Boosts population growth for 5 turns.",
            Rarity.Epic,
            true,
            0,
            (nation, grid) =>
            {
                // Asetetaan buffin kesto
                nation.maatalousBuffTurns = 5;
                
                nation.maatalousBonusPerTurn = 20000f;
            }
        ));

        // 12. Armeijan kehitys (pieni)
        secondEraCards.Add(new Card(
            "Army Recruitment",
            "Adds to your army based on 15% of its current size.",
            Rarity.Normal,
            false,
            0,
            (nation, grid) =>
            {
                int add = Mathf.RoundToInt(nation.Military * 0.15f);
                nation.Military += add;
                //Debug.Log($"Keskiajan Armeijan kehitys (pieni): +{add} yksikköä.");
            }
        ));

        // 12. Armeijan kehitys (pieni)
        secondEraCards.Add(new Card(
            "Army Recruitment 2",
            "Adds to your army based on 50% of its current size.",
            Rarity.Rare,
            false,
            0,
            (nation, grid) =>
            {
                int add = Mathf.RoundToInt(nation.Military * 0.50f);
                nation.Military += add;
                //Debug.Log($"Keskiajan Armeijan kehitys (pieni): +{add} yksikköä.");
            }
        ));

        // 13. Armeijan kehitys (normaali)
        secondEraCards.Add(new Card(
            "Huge Army Recruitment",
            "More than doubling the size of the army.",
            Rarity.Epic,
            false,
            0,
            (nation, grid) =>
            {
                int add = Mathf.RoundToInt(nation.Military * 1.2f);
                nation.Military += add;
                //Debug.Log($"Keskiajan Armeijan kehitys: +{add} yksikköä.");
            }
        ));

        // 14. Moraali-kortit
        secondEraCards.Add(new Card(
            "Morale",
            "Increases moral +4.",
            Rarity.Normal,
            false,
            0,
            (nation, grid) =>
            {
                nation.IncreaseMorale(4f);
                //Debug.Log("Keskiajan Moraali 4 aktivoitu: +4 moraalia.");
            }
        ));
        secondEraCards.Add(new Card(
            "Morale 2",
            "Increases moral +8.",
            Rarity.Rare,
            false,
            0,
            (nation, grid) =>
            {
                nation.IncreaseMorale(8f);
                //Debug.Log("Keskiajan Moraali 8 aktivoitu: +8 moraalia.");
            }
        ));


        // 15. Tutkimusmatkat merelle (Explorations at sea)
        secondEraCards.Add(new Card(
            "Explorations at sea",
            "Unlocks Advanced exploration.",
            Rarity.Epic,
            true,
            2,
            (nation, grid) =>
            {
        // Varmistetaan, että Port-korttia on aktivoitu vähintään kerran.
        if (nation.PortCardActivations < 1)
                {
                    
                    return;
                }
        // Haetaan CardDeck-instanssi
        CardDeck cardDeck = GameObject.FindObjectOfType<CardDeck>();
                if (cardDeck != null)
                {
            // Jos "Advanced exploration" -korttia ei vielä ole, luodaan se ja lisätään pakkaan
            if (!cardDeck.deck.Any(c => c.Name == "Advanced exploration"))
                    {
                        Card advancedExploration = new Card(
                            "Advanced exploration",
                            "Reveals unexplored squares on the map, including sea tiles.",
                            Rarity.Rare,
                            false,
                            2,
                            (nation2, grid2) =>
                            {
                                int count = 200;
                                int tilesRevealed = 0;
                                List<SquareTile> discovered = grid2.GetDiscoveredTiles();
                                HashSet<SquareTile> visited = new HashSet<SquareTile>(discovered);
                                Queue<SquareTile> queue = new Queue<SquareTile>(discovered);

                                while (queue.Count > 0 && tilesRevealed < count)
                                {
                                    SquareTile current = queue.Dequeue();
                                    foreach (SquareTile neighbor in grid2.GetNeighbors(current))
                                    {
                                        if (!visited.Contains(neighbor) && !neighbor.IsVisible)
                                        {
                                            neighbor.SetVisibility(true, true);
                                            visited.Add(neighbor);
                                            queue.Enqueue(neighbor);
                                            tilesRevealed++;
                                            if (tilesRevealed >= count)
                                                break;
                                        }
                                    }
                                }
                                
                            }
                        );
                        cardDeck.deck.Add(advancedExploration);
                        //Debug.Log("kehittyny tutkimine lisätty pakkaa");
                    }
                    else
                    {
                        //Debug.Log("tutkimuskortti on jo pakassa.");
                    }
            // Poistetaan mahdolliset "Expeditions" -kortit pakasta
            int removedCount = cardDeck.deck.RemoveAll(c => c.Name == "Expeditions");
                    if (removedCount > 0)
                    {
                        //Debug.Log("Expeditions card removed from the deck, since Advanced exploration is now available.");
                    }
                }
            }
        ));



        // 16. Kaivos: antaa tuloja.
        secondEraCards.Add(new Card(
            "Mine",
            "Increases the realm's income.",
            Rarity.Normal,
            false,
            0,
            (nation, grid) =>
            {
                nation.IncomeBonusPerTurn += 0.15f;
            }
        ));

        //  Kaupunki (Legendary)
        secondEraCards.Add(new Card(
            "Town",
            "Increases the realm's income.",
            Rarity.Legendary,
            true,
            1,
            (nation, grid) =>
            {
                nation.IncomeBonusPerTurn += 7;


                // Valitaan satunnainen hallittu ruutu, johon asetetaan kaupunki
                SquareTile freeTile = grid.GetControlledTiles()
                    .Where(t => t.Terrain != SquareTile.TerrainType.MakeaVesi && t.controllingNation == nation && !t.HasCity && !t.HasCastle && !t.HasPort)
                    .OrderBy(t => UnityEngine.Random.value)
                    .FirstOrDefault();

                if (freeTile != null)
                {
                    freeTile.HasCity = true;
                    //Debug.Log($"Kaupunki perustettu ruutuun ({freeTile.X}, {freeTile.Y}).");
                }
                else
                {
                    //Debug.LogWarning("Ei vapaita ruutuja, joihin kaupunki voidaan perustaa.");
                }
            }

        ));

        secondEraCards.Add(new Card(
        "Infrastructure",
    "Increases income for each tile you control.",
    Rarity.Normal,
    false,
    0,
    (nation, grid) =>
    {
        int tileCount = grid.GetControlledTiles().Count(t => t.controllingNation == nation);
        nation.AddNetGDPGrowth(tileCount * 0.025f);
        //Debug.Log("Tieverkoston kehittyminen (Keskiaika): Nettotulo kasvaa 0.25 per hallussa oleva ruutu.");
    }
));

        secondEraCards.Add(new Card(
    "Expand the Realm",
    "Adds new territories to your realm.",
    Rarity.Rare,
    false,
    0,
    (nation, grid) =>
    {
        // Oletuksena laajennetaan 1 ruutu, mutta tarkistetaan ensin first era -tehostukset…
        int expansions = 1;
        if (nation.isSecondEraExpansionBuffActive) 
        {
            expansions = 15;
        }
        else if (nation.isExpansionBuff10Active) 
        {
            expansions = 10;
        }
        // ...ja sitten second era -tehostus
        else if (nation.isExpansionBuff5Active) 
        {
            // Käytetään korkeinta laajennusmäärää
            expansions = expansions = 5;
        }

        // Suoritetaan laajennus expansions-kertaa
        for (int i = 0; i < expansions; i++)
        {
            SquareTile newlyControlled = grid.ExpandTerritoryForNation(nation);
            if (newlyControlled == null)
            {
                //Debug.Log("Ei enää laajennettavia reuna-alueita!");
                break;
            }
            // 50% mahdollisuus pudottaa moraalia. Jos mikä tahansa laajennuksen tehostuskortti on aktiivinen,
            // pudotus on kevennetty (1 piste normaalin 2 sijaan).
            int moraleDrop = (nation.isExpansionBuff5Active || nation.isExpansionBuff10Active || nation.isSecondEraExpansionBuffActive) ? 1 : 2;
            if (UnityEngine.Random.value < 0.5f)
            {
                nation.DecreaseMorale(moraleDrop);
                //Debug.Log($"Laajennuksen seurauksena moraalia pudotettu: -{moraleDrop} pistettä.");
            }
        }
    }
));

        // Nyt uusi pakka korvaa edellisen (esim. asettamalla deck = secondEraCards)
        deck = secondEraCards;
        ShuffleDeck();
        //Debug.Log("Keskiajan korttipakka alustettu.");
    }


    private void RevealRandomTiles(SquareGrid grid, int count, Nation nation)
    {
        // Hanki kaikki kontrolloidut laatat
        List<SquareTile> discovered = grid.GetDiscoveredTiles();

        // Kerää potentiaaliset laatat avattavaksi: kontrolloitujen laattojen naapurit, jotka eivät ole näkyviä
        HashSet<SquareTile> potentialTiles = new HashSet<SquareTile>();

        foreach (var tile in discovered)
        {
            var neighbors = grid.GetNeighbors(tile);
            foreach (var neighbor in neighbors)
            {
                // Vain ruudut, joita ei ole vielä näkyvissä
                if (!neighbor.IsVisible && neighbor.Terrain != SquareTile.TerrainType.Merivesi)
                {
                    potentialTiles.Add(neighbor);
                }

            }
        }

        //Debug.Log($"Löytöretket: Potentiaalisten avattavien laattojen määrä: {potentialTiles.Count}");

        if (potentialTiles.Count == 0)
        {
            //Debug.LogWarning("Löytöretket: Ei löydetty avoimia ruutuja lähellä kontrolloitua aluetta.");
            return;
        }

        // Sekoita potentiaaliset laatat
        List<SquareTile> shuffled = potentialTiles.OrderBy(t => UnityEngine.Random.value).ToList();
        int tilesRevealed = 0;

        foreach (var tile in shuffled)
        {
            if (tilesRevealed >= count) break;
            tile.SetVisibility(true, true);
            tilesRevealed++;
        }

        //Debug.Log($"Löytöretket: Avaaminen onnistui {tilesRevealed} ruudussa.");
    }


    /// <summary>
    /// Nostaa 'amount' korttia rarityn mukaan (60/25/10/5) 
    /// deck-listasta, ottaen huomioon Tech-vaatimuksen ja Unique-poissulkemisen.
    /// </summary>
    public List<Card> DrawCards(int amount)
    {
        List<Card> drawnCards = new List<Card>();
        for (int i = 0; i < amount; i++)
        {
            Rarity chosen = GetRandomRarity();
            Card selected = FindAvailableCardOfRarityInDeck(chosen);
            if (selected == null)
            {
                // fallback
                Rarity fallback = DowngradeRarity(chosen);
                while (fallback != chosen && selected == null)
                {
                    selected = FindAvailableCardOfRarityInDeck(fallback);
                    fallback = DowngradeRarity(fallback);
                }

                // Lopullinen fallback: jos vieläkin null, ota mikä tahansa kortti
                if (selected == null)
                {
                    selected = FindAnyAvailableCardInDeck();
                }
            }
            if (selected != null)
            {
                // Poistetaan pakasta heti
                deck.Remove(selected);
                drawnCards.Add(selected);
            }
            else
            {
                //Debug.LogWarning($"Ei löytynyt korttia harvinaisuudella {chosen}!");
            }
        }


        return drawnCards;
    }

    private Card FindAnyAvailableCardInDeck()
    {
        // Poimitaan kaikki kortit, jotka eivät ole unique&käytössä
        // JA joiden RequiredTechLevel on <= pelaajan nykyinen Technology.
        var candidates = deck.Where(c =>
            c.RequiredTechLevel <= playerNation.Technology
            && !(c.IsUnique && usedUniques.Contains(c.Name)) &&
            !(c.Name == "Religion" && GameManager.Instance.CurrentTurn > 40)
        ).ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        // Palautetaan satunnainen kortti
        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }

    /// <summary>
    /// Etsitään first-match deckistä (pakasta).
    /// Suodatetaan pois unique & jo käytetyt, tai tech-tason ylittävät kortit.
    /// </summary>
    private Card FindAvailableCardOfRarityInDeck(Rarity rarity)
    {
        // Haetaan SquareGrid-instanssi, jotta voidaan tehdä tarvittavat tarkistukset.
        SquareGrid grid = FindObjectOfType<SquareGrid>();

        var candidates = deck.Where(c =>
            c.Rarity == rarity &&
            c.RequiredTechLevel <= playerNation.Technology &&
            !((c.IsUnique || c.Name == "Port" || c.Name == "Castle") && usedUniques.Contains(c.Name)) &&
            !(c.Name == "Religion" && GameManager.Instance.CurrentTurn > 40) &&
            !(c.Name == "Expansion Boost" && (playerNation.isExpansionBuff10Active || usedUniques.Contains("Major Expansion Boost"))) &&
            !(c.Name == "Explorations at sea" && playerNation.PortCardActivations < 1) &&
            
            (c.Name != "Port" || (grid != null &&
                grid.GetControlledTiles()
                    .Where(t => t.controllingNation == playerNation)
                    .Any(t => grid.GetNeighbors(t).Any(n => n.Terrain == SquareTile.TerrainType.Merivesi))))
        ).ToList();

        // Poistetaan lisäksi muut erikoistarkistukset, kuten "Castle", "Swordsmen" ja "Military innovations".
        candidates = candidates.Where(c =>
        {
            if (c.Name == "Castle")
            {
                if (grid == null)
                    return false;
                bool hasIronFromTiles = grid.GetControlledTiles()
                    .Any(tile => tile.controllingNation == playerNation && tile.HasIron);
                bool hasIronFromTrade = playerNation.HasActiveIronTrade;
                if ((!hasIronFromTiles && !hasIronFromTrade) || !playerNation.HasIronCard)
                    return false;
            }
            if (c.Name == "Swordsmen")
            {
                if (grid == null)
                    return false;
                bool hasIronFromTiles = grid.GetControlledTiles()
                    .Any(tile => tile.controllingNation == playerNation && tile.HasIron);
                bool hasIronFromTrade = playerNation.HasActiveIronTrade;
                if ((!hasIronFromTiles && !hasIronFromTrade) || !playerNation.HasIronCard)
                    return false;
            }
            if (c.Name == "Military innovations")
            {
                if (!usedUniques.Contains("Swordsmen"))
                    return false;
            }
            return true;
        }).ToList();

        if (candidates.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }


    private Rarity GetRandomRarity()
    {
        float roll = UnityEngine.Random.value;
        if (roll <= normalChance)
            return Rarity.Normal;
        else if (roll <= normalChance + rareChance)
            return Rarity.Rare;
        else if (roll <= normalChance + rareChance + epicChance)
            return Rarity.Epic;
        else
            return Rarity.Legendary;
    }

    private Rarity DowngradeRarity(Rarity r)
    {
        switch (r)
        {
            case Rarity.Legendary: return Rarity.Epic;
            case Rarity.Epic: return Rarity.Rare;
            case Rarity.Rare: return Rarity.Normal;
            default: return Rarity.Normal;
        }
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int rnd = UnityEngine.Random.Range(i, deck.Count);
            deck[i] = deck[rnd];
            deck[rnd] = temp;
        }
    }

    public void OnCardUsed(Card card)
    {
        // Unique-kortit merkitään käytetyiksi pysyvästi
        if (card.IsUnique)
        {
            usedUniques.Add(card.Name);
        }
    }

    /// <summary>
    /// Ei-uniikit kortit siirretään discardPileen (siten eivät enää palaa).
    /// Halutessasi voi implementoida "kun deck tyhjä, sekoita discard" tms.
    /// </summary>
    public void DiscardCard(Card card)
    {
        if (card == null)
        {
            //Debug.LogError("DiscardCard: Yritettiin hävittää null-kortti!");
            return;
        }
        if (!card.IsUnique)
        {
            discardPile.Add(card);
            //Debug.Log($"Kortti {card.Name} menee discard-pakkaan.");
        }
        else
        {
            //Debug.Log($"Uniikki kortti {card.Name} ei mene discard-pakkaan (poistettu pysyvästi).");
        }
    }


    private void RevealDeepTiles(SquareGrid grid, int count, Nation nation)
    {
        // Hae kaikki jo löydetyt ruudut (jotka ovat joko näkyviä tai on aiemmin tutkittu)
        List<SquareTile> discovered = grid.GetDiscoveredTiles();

        // Käytetään jonoa ja joukkoa, jotta ei käydä samaa ruutua useaan kertaan läpi
        Queue<SquareTile> queue = new Queue<SquareTile>();
        HashSet<SquareTile> visited = new HashSet<SquareTile>();

        // Lisää kaikki jo löydetyt ruudut jonoon
        foreach (var tile in discovered)
        {
            queue.Enqueue(tile);
            visited.Add(tile);
        }

        int tilesRevealed = 0;

        // Käy jonoa läpi, kunnes paljastettuja ruutuja on count tai jono loppuu
        while (queue.Count > 0 && tilesRevealed < count)
        {
            SquareTile current = queue.Dequeue();

            // Käy läpi tämän ruudun naapurit
            List<SquareTile> neighbors = grid.GetNeighbors(current);
            foreach (SquareTile neighbor in neighbors)
            {
                if (!visited.Contains(neighbor) && !neighbor.IsVisible)
                {
                    // Jos ruutu EI ole merilaatta, paljasta se
                    if (neighbor.Terrain != SquareTile.TerrainType.Merivesi)
                    {
                        neighbor.SetVisibility(true, true);
                        tilesRevealed++;
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                    // Jos ruutu on merilaatta, tarkistetaan onko se mantereeseen kiinni
                    else
                    {
                        // Hae tämän meriruudun naapurit ja tarkista,
                        // löytyykö niistä vähintään yksi, joka ei ole merilaatta.
                        List<SquareTile> seaNeighbors = grid.GetNeighbors(neighbor);
                        bool attachedToLand = seaNeighbors.Any(t => t.Terrain != SquareTile.TerrainType.Merivesi);
                        if (attachedToLand)
                        {
                            neighbor.SetVisibility(true, true);
                            tilesRevealed++;
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }

                    if (tilesRevealed >= count)
                        break;
                }
            }
        }

        //Debug.Log($"Löytöretket (syvä haku): Avaaminen onnistui {tilesRevealed} ruudussa.");
    }

    public bool isFirstEra = true;

    public void UpdateReligionCards()
    {

        if (!isFirstEra)
            return;


        bool playerHasReligion = playerNation.Religion != null ||
    squareGrid.GetControlledTiles().Any(tile => tile.controllingNation == playerNation && tile.Religion != null);

        if (playerHasReligion)
        {
            // Poista Paganism- ja Pagan festivals -kortit
            if (deck.RemoveAll(card => card.Name == "Paganism" || card.Name == "Pagan festivals") > 0)
            {
                //Debug.Log("Paganism-kortit poistettu, sillä uskonto on nyt käytössä.");
            }

            // Varmista, että pakassa on Pilgrimage-kortit
            if (!deck.Any(card => card.Name == "Pilgrimage"))
            {
                Card pilgrimageCard = allFirstEraCards.FirstOrDefault(card => card.Name == "Pilgrimage");
                if (pilgrimageCard != null)
                {
                    deck.Add(pilgrimageCard);
                    //Debug.Log("Pilgrimage-kortti lisätty pakkaan.");
                }
            }
            if (!deck.Any(card => card.Name == "Religious festivals"))
            {
                Card greatPilgrimageCard = allFirstEraCards.FirstOrDefault(card => card.Name == "Religious festivals");
                if (greatPilgrimageCard != null)
                {
                    deck.Add(greatPilgrimageCard);
                    //Debug.Log("Religious festivals -kortti lisätty pakkaan.");
                }
            }
        }
        else
        {
            // Poista mahdolliset Pilgrimage-kortit ja varmista Paganism-korttien olemassaolo
            if (deck.RemoveAll(card => card.Name == "Pilgrimage" || card.Name == "Religious festivals") > 0)
            {
                //Debug.Log("Pilgrimage-kortit poistettu, sillä uskontoa ei ole.");
            }
            if (!deck.Any(card => card.Name == "Paganism"))
            {
                Card paganismCard = new Card(
                    "Paganism",
                    "Increases morale slightly.",
                    Rarity.Normal,
                    false,
                    0,
                    (nation, grid) =>
                    {
                        nation.IncreaseMorale(1f);
                       
                    }
                );
                deck.Add(paganismCard);
                //Debug.Log("Paganism-kortti lisätty pakkaan.");
            }
            if (!deck.Any(card => card.Name == "Pagan festivals"))
            {
                Card paganismFestivalsCard = new Card(
                    "Pagan festivals",
                    "Festivals slightly boost morale.",
                    Rarity.Rare,
                    false,
                    0,
                    (nation, grid) =>
                    {
                        nation.IncreaseMorale(1f);
                       
                    }
                );
                deck.Add(paganismFestivalsCard);
                //Debug.Log("Pagan festivals -kortti lisätty pakkaan.");
            }
        }

    }
}

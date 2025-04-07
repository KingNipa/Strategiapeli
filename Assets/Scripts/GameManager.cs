using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // Vaihda TextMeshPro:n nimiavaruus jos käytät TMP
using TMPro;          // jos käytössä TextMeshPro
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;
public enum MapView
{
    Diplomacy, // Oletusnäkymä, jossa hallitut ruudut näkyvät valtioiden värillä.
    Climatic,   // Näytetään pelkästään maantieteelliset tyypit.
    Religion,   // uskonto-näkymä
    Technology,
    Alliance
}

public enum GameState { ProcessingTurn, WaitingForInput }

public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }
    public MapView CurrentMapView = MapView.Diplomacy;

    // UI Text/TMP-elementit
    public Text turnText;
    public Text populationText;
    public Text gdpText;
    public Text militaryText;
    public Text technologyText;
    public Text militaryPowerText; // Uusi elementti
    public Text moraleText;        // Uusi elementti moraalin näyttämiseen
    public Text yearText;
    public Text militaryExpenditureText; // UI-teksti armeijan kulutukselle
    public Text gdpIncomeText; // Näyttää paljon tuloja tulee per vuoro
    public Text populationGrowthText;

    // Korttipainikkeet
    public Button cardButton1;
    public Button cardButton2;
    public Button cardButton3;
    public Button endTurnButton; // End Turn -painike

    public Nation playerNation { get; private set; } // Päivitetty julkiseksi
    public GameObject aiManagerPrefab;

    private CardDeck cardDeck;
    private SquareGrid squareGrid;

    private int turn = 0;
    private int currentYear = -3000;  // Aloitusvuosi
    private int eraIndex = 0;         // Aikakauden indeksi

    public AIController aiController;

    // Lisää viitteet armeijan pienennys UI-elementteihin
    public Slider armyReductionSlider;
    public Text armyReductionText;

    public Image ironStatusIcon;

    public TradePanel tradePanel;

    public bool cardsHidden = false;
    public Button toggleCardsVisibilityButton;
    public Text allianceBonusText;

    // Aikakausien tiedot: [aloitusvuosi, vuoroja, kuinka monta vuotta per vuoro]
    private List<(int startYear, int turns, int yearsPerTurn)> eras = new List<(int, int, int)>
    {
        (-3000, 30, 100), // 3000 BC - 0 (100 vuotta per vuoro)
        (0, 50, 30),      // 0 - 1500 (30 vuotta per vuoro)
        (1500, 40, 5),    // 1500 - 1700 (5 vuotta per vuoro)
        (1700, 50, 4),    // 1700 - 1900 (4 vuotta per vuoro)
        (1900, 125, 1)    // 1900 - 2025 (1 vuosi per vuoro)
    };

    // Muuttujat nettopopulaation muutoksen laskentaan
    private float populationAtTurnStart;
    private float netPopulationChange;

    public int CurrentYear => currentYear;

    public AllianceConfirmationPanel allianceConfirmationPanel;
    // *** BLACK TURN -KENTÄT ***
    private bool isBlackTurn = false;   // Onko käynnissä Black Turn
    private List<Card> blackTurnCards;  // Sisältää 3 huonoa korttia

    // Vuorolla nostetut kortit
    private List<Card> currentDrawnCards = new List<Card>();
    // Pelaajan valitsema kortti
    private Card chosenCard = null;

    public AIManager aiManager;

    public int CurrentTurn { get { return turn; } }

    private GameState currentGameState = GameState.WaitingForInput;

    public AudioClip endTurnSound;

    private AudioSource audioSource;

    // -----------------------------------------------------------------------
    // Awake – alustetaan pysyvä instanssi ja ei-scene-spesifiset kentät
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // Alustetaan pelaajan valtakunta
        playerNation = new Nation();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();

        }

        CurrentMapView = MapView.Diplomacy;
        //Debug.Log("CurrentMapView asetettu oletukseksi: Diplomacy");

        // Alustetaan BLACK TURN -kortit, jotta niitä ei jää nulliksi myöhemmin
        blackTurnCards = new List<Card>
        {
            new Card(
                "Recession",
                "Population decreases by 5%.",
                Rarity.Normal,
                false,
                0,
                (nation, grid) =>
                {
                    nation.Population *= 0.95f;
                    //Debug.Log("Black Turn - väkiluku -5%");
                },
                true
            ),
            new Card(
                "Military Crisis",
                "Army decreases by 15%.",
                Rarity.Normal,
                false,
                0,
                (nation, grid) =>
                {
                    int newMil = Mathf.RoundToInt(nation.Military * 0.85f);
                    nation.Military = newMil;
                },
                true
            ),
            new Card(
                "Morale Loss",
                "Morale drops by 5 points.",
                Rarity.Normal,
                false,
                0,
                (nation, grid) =>
                {
                    nation.DecreaseMorale(5f);
                },
                true
            )
        };
    }


    private Button notificationConfirmButton;
    // -----------------------------------------------------------------------
    // OnSceneLoaded – suoritetaan aina, kun uusi scene ladataan.
    // Tässä haetaan kaikki GameScene-spesifiset komponentit ja kutsutaan InitializeGameScene()
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            GameObject canvasObj = GameObject.FindWithTag("Canvas");
            if (canvasObj != null)
            {
                mainCanvasGroup = canvasObj.GetComponent<CanvasGroup>();
            }

            // Hae SquareGrid
            squareGrid = FindObjectOfType<SquareGrid>();
            // Aseta squareGrid.nation
            squareGrid.nation = GameManager.Instance.playerNation;
            //  Debug.Log("squareGrid.nation asetettu: " + squareGrid.nation.Name);
            //  Debug.Log("Nation-instanssit ovat samat: " + ReferenceEquals(GameManager.Instance.playerNation, squareGrid.nation));

            // Ensiksi päivitetään asetukset ja generoidaan grid uudelleen:
            SetGameSettings(storedAiNationCount, storedWorldHeight, storedWorldWidth, storedColor, storedSeed);

            aiController = FindObjectOfType<AIController>();
            if (aiController != null)
            {
                aiController.numberOfAIEmpires = storedAiNationCount; // käyttäjän asettama arvo
                aiController.InitializeAIManagers();                  // AIManagerien luonti oikealla määrällä
            }



            // Nyt grid on päivitetty, ja valid starting tiles -määrä pitäisi olla oikein.
            if (GameManager.ForceGrasslandStart)
                squareGrid.InitializeStartingAreaForNationGrassland(squareGrid.nation, true);

            else
                squareGrid.InitializeStartingAreaForNation(squareGrid.nation, true);

            squareGrid.InitializeFogOfWar();

            var start = squareGrid.GetControlledTiles().FirstOrDefault(t => t.controllingNation == playerNation);
            if (start != null)
            {
                // Debug.Log("Paljastetaan starting tile ympäristöstä ruudulle: (" + start.X + ", " + start.Y + ")");
                squareGrid.RevealTileAndSurroundings(start, squareGrid.initialRevealRadius);
            }


            // Hae CardDeck ja kutsu sen alustus (korttipakan generointi)
            cardDeck = FindObjectOfType<CardDeck>();
            if (cardDeck == null)
            {
                //Debug.LogError("CardDeck-komponenttia ei löydy GameScene:stä!");
            }
            else
            {
                cardDeck.playerNation = playerNation;
                cardDeck.InitializeDeck(); // Uusi metodi CardDeckissä, joka alustaa kortit
            }

            // Hae UI-tekstikentät (tarkista, että nimet vastaavat hierarkiassa)
            turnText = GameObject.Find("TurnText")?.GetComponent<Text>();
            populationText = GameObject.Find("PopulationText")?.GetComponent<Text>();
            gdpText = GameObject.Find("GDPText")?.GetComponent<Text>();
            militaryText = GameObject.Find("MilitaryText")?.GetComponent<Text>();
            technologyText = GameObject.Find("TechnologyText")?.GetComponent<Text>();
            militaryPowerText = GameObject.Find("MilitaryP")?.GetComponent<Text>();
            moraleText = GameObject.Find("Morale")?.GetComponent<Text>();
            yearText = GameObject.Find("Year")?.GetComponent<Text>();
            militaryExpenditureText = GameObject.Find("MilitaryGDP")?.GetComponent<Text>();
            gdpIncomeText = GameObject.Find("GDPturn")?.GetComponent<Text>();
            populationGrowthText = GameObject.Find("PopulationChange")?.GetComponent<Text>();

            // Hae korttipainikkeet
            cardButton1 = GameObject.Find("Kortti1")?.GetComponent<Button>();
            cardButton2 = GameObject.Find("Kortti1 (1)")?.GetComponent<Button>();
            cardButton3 = GameObject.Find("Kortti1 (2)")?.GetComponent<Button>();
            endTurnButton = GameObject.Find("EndTurn")?.GetComponent<Button>();

            // Hae slideri ja sen tekstikenttä
            armyReductionSlider = GameObject.Find("ArmyReductionSlider")?.GetComponent<Slider>();
            armyReductionText = GameObject.Find("ArmyReductionText")?.GetComponent<Text>();

            // Hae ironStatusIcon
            ironStatusIcon = GameObject.Find("rautakuva")?.GetComponent<Image>();

            // Hae TradePanel
            tradePanel = GameObject.Find("TradePanel")?.GetComponent<TradePanel>();
            if (tradePanel != null)
            {
                tradePanel.gameObject.SetActive(false); // Piilotetaan TradePanel alussa
            }

            // Hae AIController (jos se on Sceneen)
            aiController = FindObjectOfType<AIController>();


            //Debug.Log("GameScene ladattu ja GameManagerin viitteet päivitetty.");

            // Haetaan GameOverPanel ja sen lapsena oleva GameOverButton, jos niitä ei ole liitetty Inspectorissa
            if (GameOverPanel == null)
            {
                GameOverPanel = GameObject.Find("GameOverPanel");
            }
            if (GameOverPanel != null)
            {
                // Varmistetaan, että paneeli on piilotettu pelin alussa
                GameOverPanel.SetActive(false);

                // Jos haluat hakea myös GameOverButtonin, niin:
                Button gameOverButton = GameOverPanel.transform.Find("GameOverButton")?.GetComponent<Button>();
                if (gameOverButton != null)
                {
                    // Voi lisätä napin toiminnallisuuden, jos en oo jo määritelty ShowGameOverScreen()-metodissa
                    gameOverButton.onClick.RemoveAllListeners();
                    gameOverButton.onClick.AddListener(EndGame);

                    // Päivitetään myös napin tekstin arvo ehkä funtsin
                    Text buttonText = gameOverButton.GetComponentInChildren<Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = "The end of the realm";
                    }
                }
                else
                {
                    //Debug.LogWarning("GameOverButtonia ei löytynyt GameOverPanelin sisältä!");
                }
            }
            else
            {
                //Debug.LogWarning("GameOverPanelia ei löytynyt!");
            }


            // Haetaan toggleCardsVisibilityButton (kortin osittainen piilotuspainike)
            toggleCardsVisibilityButton = GameObject.Find("Cardbutton")?.GetComponent<Button>();
            if (toggleCardsVisibilityButton == null)
            {
                //Debug.LogError("Cardbuttonia ei löydy GameScene:stä!");
            }

            // Haetaan piilotusnapin viite
            completeToggleCardsButton = GameObject.Find("HideCButton")?.GetComponent<Button>();
            if (completeToggleCardsButton != null)
            {
                // completeToggleCardsButton.onClick.RemoveAllListeners();
                //  completeToggleCardsButton.onClick.AddListener(ToggleCompleteCardsVisibility);
                //  UpdateCompleteToggleButtonText(); // Päivitetään napin alkuarvo
            }
            else
            {
                //Debug.LogError("CompleteCardToggleButtonia ei löydy GameScene:stä!");
            }

            // Rekisteröidään painikkeelle napuska

            // Hae ilmoitusikkunalle asetetut UI-elementit dynaamisesti
            notificationWindow = GameObject.Find("NotificationWindow");
            if (notificationWindow != null)
            {
                notificationMessage = notificationWindow.transform.Find("NotificationMessage")?.GetComponent<TextMeshProUGUI>();
                if (notificationMessage == null)
                {
                    //Debug.LogWarning("NotificationMessage-tekstiä ei löytynyt!");
                }

                // Hae ConfirmButton, joka sijaitsee ilmoitusikkunan sisällä
                Button confirmButton = notificationWindow.transform.Find("ConfirmnoButton")?.GetComponent<Button>();
                if (confirmButton != null)
                {
                    // Liitetään napin klikkaustapahtumaan metodi, joka piilottaa ilmoitusikkunan
                    confirmButton.onClick.AddListener(HideNotificationWindow);
                }
                else
                {
                    //Debug.LogWarning("ConfirmButtonia ei löytynyt ilmoitusikkunasta!");
                }

                // Piilotetaan paneeli heti, kun saadaan viite (vaikka se on aluksi aktivoitu, jotta löytyy)
                notificationWindow.SetActive(false);
            }
            else
            {
                //Debug.LogWarning("NotificationWindowa ei löytynyt!");
            }


            // TIPS-UI:
            if (tipsWindow == null)
                tipsWindow = GameObject.Find("TipsWindow");
            if (tipText == null)
                tipText = GameObject.Find("TipText")?.GetComponent<Text>();
            if (tipWindowButton == null)
                tipWindowButton = GameObject.Find("TipWindowButton")?.GetComponent<Button>();
            if (tipsOffButton == null)
                tipsOffButton = GameObject.Find("TipsOffButton")?.GetComponent<Button>();

            if (tipWindowButton != null)
                tipWindowButton.onClick.AddListener(HideTipWindow);
            //  if (tipsOffButton != null)
            //      tipsOffButton.onClick.AddListener(DisableTips);



            //liittouman hyväksymiseen liittyvä paneli

            GameObject alliancePanelObj = GameObject.Find("allianceConfirmationPanel");
            if (alliancePanelObj == null)
            {
                // Yritetään hakea myös inaktiiviset objektit Resources.FindObjectsOfTypeAll avulla
                AllianceConfirmationPanel[] panels = Resources.FindObjectsOfTypeAll<AllianceConfirmationPanel>();
                foreach (var panel in panels)
                {
                    if (panel.gameObject.name == "allianceConfirmationPanel")
                    {
                        allianceConfirmationPanel = panel;
                        alliancePanelObj = panel.gameObject;
                        break;
                    }
                }
            }
            if (alliancePanelObj != null)
            {
                // Jos paneeli on inaktiivinen, aktivoidaan se tilapäisesti
                bool wasActive = alliancePanelObj.activeSelf;
                if (!wasActive)
                {
                    alliancePanelObj.SetActive(true);
                }

                allianceConfirmationPanel = alliancePanelObj.GetComponent<AllianceConfirmationPanel>();
                if (allianceConfirmationPanel != null)
                {
                    // Haetaan "alliancetext" lapsi-objekti ja sen TextMeshProUGUI-komponentti
                    allianceConfirmationPanel.messageText = alliancePanelObj.transform.Find("alliancetext")?.GetComponent<Text>();
                    if (allianceConfirmationPanel.messageText == null)
                    {
                        //Debug.LogWarning("alliancetext-tekstiä ei löytynyt AlliancePanelista!");
                    }

                    // Haetaan "alliButton" lapsi-objekti ja sen Button-komponentti
                    allianceConfirmationPanel.confirmButton = alliancePanelObj.transform.Find("alliButton")?.GetComponent<Button>();
                    if (allianceConfirmationPanel.confirmButton == null)
                    {
                        //Debug.LogWarning("alliButtonia ei löytynyt AlliancePanelista!");
                    }
                    allianceConfirmationPanel.rejectButton = alliancePanelObj.transform.Find("rejectalliButton")?.GetComponent<Button>();
                    if (allianceConfirmationPanel.rejectButton == null)
                    {
                        //Debug.LogWarning("rejectalliButtonia ei löytynyt AlliancePanelista!");
                    }
                }
                else
                {
                    //Debug.LogWarning("AllianceConfirmationPanel-komponenttia ei löytynyt AlliancePanelista!");
                }

                // Palautetaan paneelin alkuperäinen aktiivisuustila
                if (!wasActive)
                {
                    alliancePanelObj.SetActive(false);
                }
            }
            else
            {
                //Debug.LogWarning("AlliancePanelia ei löytynyt!");
            }
            //haetaan alliancebonus ui:hin
            allianceBonusText = GameObject.Find("AllianceBonus")?.GetComponent<Text>();
            if (allianceBonusText == null)
            {
                //Debug.LogWarning("AllianceBonus-tekstiä ei löytynyt!");
            }

            // Kutsutaan erillistä metodia, joka suorittaa pelikohtaisen alustuslogiikan
            InitializeGameScene();

            ZoomToPlayerNation();

        }
    }

    // -----------------------------------------------------------------------
    // InitializeGameScene – alustaa pelikohtaiset asetukset UI:n, korttien ja muiden komponenttien osalta
    private void InitializeGameScene()
    {


        // Alustetaan pelaajan aloitusalue (squareGrid:n avulla)
        /*  if (squareGrid != null)
          {
              if (GameManager.ForceGrasslandStart)
                  squareGrid.InitializeStartingAreaForNationGrassland(playerNation, true);
              else
                  squareGrid.InitializeStartingAreaForNation(playerNation, true);
          } */

        // Aseta endTurnButtonin listener
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(EndTurn);
            endTurnButton.interactable = false;
        }
        else
        {
            //Debug.LogError("End Turn -painiketta ei ole asetettu GameManagerille.");
        }

        // Aseta sliderin arvo ja listener
        if (armyReductionSlider != null)
        {
            armyReductionSlider.value = playerNation.ArmyReductionPercentage;
            armyReductionSlider.onValueChanged.RemoveAllListeners();
            armyReductionSlider.onValueChanged.AddListener(OnArmyReductionSliderChanged);
        }
        else
        {
            //Debug.LogError("ArmyReductionSlider ei ole asetettu GameManagerille.");
        }
        ToggleCardsVisibility();
        // Päivitetään UI heti
        UpdateUI();

        // Nostetaan ensimmäiset vuoron kortit (vuoro 0)
        DrawNewTurn();
    }

    // -----------------------------------------------------------------------
    // (Voi poistaa tai siirtää Start()-metodin, sillä alustus tehdään OnSceneLoaded:ssa.)
    void Start()
    {
        // Start() kutsutaan vain kerran, kun instanssi luodaan menuscenessä.
        // Pelikohtaiset alustukset tehdään OnSceneLoaded:ssa, kun GameScene ladataan.
    }



    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    IEnumerator ResetInputBlockedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // inputBlocked nollataan vain, jos enter on jo vapautettu.
        if (!Input.GetKey(KeyCode.Return))
        {
            inputBlocked = false;
        }
    }

    IEnumerator WaitForEnterReleaseAndReset(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Odotetaan, että Enter on vapautettu
        while (Input.GetKey(KeyCode.Return))
        {
            yield return null;
        }
        inputBlocked = false;
    }
    private bool turnHasEnded = false;

    private float turnInputCooldown = 0f;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsTurnTransitioning)
            return;
        if (currentGameState != GameState.WaitingForInput)
            return;

        // Tarkistetaan myös, ettei inputBlocked ole true
        if (Input.GetKeyDown(KeyCode.Return) && endTurnButton.interactable && !inputBlocked)
        {
            inputBlocked = true; // Estetään uusia painalluksia siihen asti, kunnes Enter vapautetaan
            EndTurn();
            // Vois käynnistää coroutine:n, joka odottaa Enterin vapautumista ja nollaa inputBlocked
            StartCoroutine(WaitForEnterReleaseAndReset(0.1f));
        }


    }


    // Metodi sliderin arvon muutoksen käsittelemiseksi
    void OnArmyReductionSliderChanged(float value)
    {
        playerNation.ArmyReductionPercentage = value;
        UpdateArmyReductionUI();   // Päivittää sliderin tekstiä
        UpdateArmyStatsPreview();    // Päivittää Army Size ja Power -tekstit esikatseluna
    }


    // Metodi armeijan pienennyksen UI:n päivittämiseksi
    void UpdateArmyReductionUI()
    {
        if (armyReductionText != null)
        {
            armyReductionText.text = $"Army size: {playerNation.ArmyReductionPercentage:F0}%";
        }
    }

    private bool blackDeathOccurred = false; // Lisää luokkatason muuttuja
    /// <summary>
    /// Nostetaan vuoron kortit TAI käynnistetään Black Turn, jos arvonta osuu.
    /// </summary>
    void DrawNewTurn()
    {

        // Näytä edellisen vuoron tulot
        float previousTurnIncome = playerNation.LastTurnNetGDPGrowth + playerNation.PreviousArmyReductionIncome;
        gdpIncomeText.text = previousTurnIncome < 0
            ? $"Net income: {previousTurnIncome:F1}"
            : $"Net income: +{previousTurnIncome:F1}";

        playerNation.ResetArmyReductionIncome();
        playerNation.ResetLastTurnPopulationGrowth();
        playerNation.ResetLastTurnNetGDPGrowth();


        populationAtTurnStart = playerNation.Population;
        cardDeck.UpdateReligionCards();

        // Tarkistetaan globaali musta surma vain, jos vuosi on 900–1500 ja tapahtumaa ei ole vielä laukaistu
        if (currentYear >= 900 && currentYear <= 1500 && !blackDeathOccurred)
        {
            if (Random.value < 0.03f)  // 3% mahdollisuus
            {
                blackDeathOccurred = true; // Merkitään, että musta surma on tapahtunut

                // Arvotaan vähenemisprosentti väliltä 15–35 %
                int reductionPercent = Random.Range(15, 36);
                //Debug.Log($"Global Musta surma osui! Kaikkien kansojen väkiluku vähenee {reductionPercent}%.");

                // Sovelletaan vaikutus pelaajan kansaan
                playerNation.Population *= (1 - reductionPercent / 100f);
                //Debug.Log($"Pelaajan väkiluku vähentyi {reductionPercent}%.");

                // Sovelletaan vaikutus tekoälyvaltioihin
                if (aiController != null)
                {
                    foreach (var aiManager in aiController.GetComponentsInChildren<AIManager>())
                    {
                        aiManager.aiNation.Population *= (1 - reductionPercent / 100f);
                        //Debug.Log($"AI:n '{aiManager.aiNation.Name}' väkiluku vähentyi {reductionPercent}%.");
                    }
                }

                UpdateUI();

                // Luo Black Death -kortti ja anna pelaajalle 3 identtistä kopiota siitä
                Card blackDeathCard = new Card(
                    "Black Death",
                    "Your nation is ravaged by the plague. Population suffer.",
                    Rarity.Legendary,
                    true,  
                    0,
                    (nation, grid) =>
                    {
                        // Mahdollinen kortin lisäefekti, esim. lisävahinko tai -ominaisuus
                        //Debug.Log("Black Death -kortin vaikutus aktivoitui.");
                    },
                    true  // Merkintä mustan vuoron kortista
                );

                // Aseta pelaajan korttivalikoimaan 3 identtistä Black Death -korttia
                currentDrawnCards = new List<Card> { blackDeathCard, blackDeathCard, blackDeathCard };
                ShowDrawnCardsOnUI();
                chosenCard = null;
                endTurnButton.interactable = true;


                return; // Lopetetaan tämän vuoron korttien nosto
            }
        }

        if (turn % 6 == 0)
        {
            ShowTipWindow();

        }

        // Määritetään vuoron kortit
        if (turn == 0)
        {
            // Ensimmäisellä vuorolla aina normaali vuoro
            isBlackTurn = false;
            currentDrawnCards = cardDeck.DrawCards(3);

            // Käytetään coroutinea aloitusruudun paljastamiseen viiveellä
            var startTile = squareGrid.GetControlledTiles().FirstOrDefault(t => t.controllingNation == playerNation);
            if (startTile != null)
            {
                //   Debug.Log("Ensimmäisellä vuorolla starting tile: (" + startTile.X + ", " + startTile.Y + ") on valittu. Käynnistetään viiveen kanssa paljastus.");
                StartCoroutine(RevealStartingTileCoroutine(startTile, squareGrid.initialRevealRadius));
            }
        }
        else
        {
            // Määritellään black turnin todennäköisyys moraalin perusteella
            float morale = playerNation.Morale;
            float blackTurnChance = 0f;
            if (morale >= 90f)
                blackTurnChance = 0.03f;
            else if (morale >= 80f)
                blackTurnChance = 0.05f;
            else if (morale >= 55f)
                blackTurnChance = 0.10f;
            else
                blackTurnChance = 0.20f;

            float roll = UnityEngine.Random.value;
            if (roll < blackTurnChance)
            {
                // BLACK TURN!
                isBlackTurn = true;
                currentDrawnCards = new List<Card>(blackTurnCards);
                //Debug.Log("BLACK TURN laukeaa! Pelaajan on valittava 1 kolmesta huonosta kortista.");
            }
            else
            {
                // Normaali vuoro
                isBlackTurn = false;
                currentDrawnCards = cardDeck.DrawCards(3);
            }
        }

        //Debug.Log($"Kortteja nostettu: {currentDrawnCards.Count}");
        ShowDrawnCardsOnUI();

        chosenCard = null;
        endTurnButton.interactable = false;
        //ZoomToPlayerNation(); //tää on pikakorjaus siihe ettei kamera lennä ulos jos kortteja rämppää
        StartTurnTransition();

        // Päivitetään kameran hiiren sijainti
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraController camController = mainCam.GetComponent<CameraController>();
            if (camController != null)
            {
                camController.UpdateLastMouseWorldPos();
                                                         // tai suoraan asettaa lastMouseWorldPos kuten aiemmin.
                                                         // Käynnistetään panningin ohitus
                camController.IgnorePanningForDuration(camController.ignorePanningDuration);
            }
        }

        // Poistetaan turn transition -tila lyhyen viiveen jälkeen
        StartCoroutine(EndTurnTransitionAfterDelay(0.2f));
    }
    IEnumerator EndTurnTransitionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndTurnTransition();
    }


    Dictionary<string, float> GetCurrentCategoryWeights()
    {
        Dictionary<string, float> weights = new Dictionary<string, float>
        {
            { "Teknologia", playerNation.CategoryWeights["Teknologia"] },
            { "Hyvinvointi", playerNation.CategoryWeights["Hyvinvointi"] },
            { "Infrastruktuuri", playerNation.CategoryWeights["Infrastruktuuri"] },
            { "Uskonto/Kulttuuri", playerNation.CategoryWeights["Uskonto/Kulttuuri"] },
            { "Armeija", playerNation.CategoryWeights["Armeija"] }
        };
        return weights;
    }


    void ShowDrawnCardsOnUI()
    {
        UpdateCardButtonsInteractable(true);

        cardButton1.gameObject.SetActive(false);
        cardButton2.gameObject.SetActive(false);
        cardButton3.gameObject.SetActive(false);

        // Käydään 3 korttia läpi
        for (int i = 0; i < currentDrawnCards.Count; i++)
        {
            Card card = currentDrawnCards[i];
            Button btn = GetCardButton(i);
            if (btn != null)
            {
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectCard(card));

                CardUIController cardUI = btn.GetComponent<CardUIController>();
                if (cardUI != null)
                {
                    // CardUIControllerissa asetetaan teksti, väri (musta) ja tarvittaessa myös kuvake sekä border
                    cardUI.SetCardData(card.Name, card.Description, card.Rarity, card.Category, null, card.IsBlackTurnCard);
                }
                else
                {
                    // Jos CardUIControlleria ei löydy, fallback-tilanne:
                    TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.text = $"{card.Name}\n{card.Description}";
                        txt.color = Color.black;
                    }
                }
            }
        }
    }

    private Color GetRarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Normal: return Color.white;
            case Rarity.Rare: return new Color(0.2f, 0.6f, 1f); // Sinertävä
            case Rarity.Epic: return new Color(0.7f, 0.3f, 1f); // Violettihko
            case Rarity.Legendary: return new Color(1f, 0.5f, 0f); // Oranssi
        }
        return Color.white;
    }

    Button GetCardButton(int index)
    {
        switch (index)
        {
            case 0: return cardButton1;
            case 1: return cardButton2;
            case 2: return cardButton3;
            default: return null;
        }
    }

    void SelectCard(Card card)
    {
        chosenCard = card;
        //Debug.Log("Valittu kortti: " + card.Name);

        // Käydään läpi kaikki nostetut korttipainikkeet
        for (int i = 0; i < currentDrawnCards.Count; i++)
        {
            Button btn = GetCardButton(i);
            if (btn != null)
            {
                CardUIController cardUI = btn.GetComponent<CardUIController>();
                if (cardUI != null)
                {
                    // Asetetaan highlight true vain jos kyseinen kortti on valittu, muuten false
                    bool isSelected = currentDrawnCards[i] == chosenCard;
                    cardUI.SetHighlight(isSelected);
                  
                }
            }
        }

        if (endTurnButton != null)
        {
            endTurnButton.interactable = true;
        }

    }

    private Vector3 lastCameraPosition;
    /// <summary>
    /// Pelivuoron päättäminen.
    /// </summary>
    void EndTurn()
    {
        EventSystem.current.SetSelectedGameObject(null);
        endTurnButton.interactable = false;
        if (currentGameState != GameState.WaitingForInput)
        {
            return;
        }
        currentGameState = GameState.ProcessingTurn;
        Card cardToProcess = chosenCard;
        chosenCard = null;



        // Tallenna kameran sijainti vuoron lopussa

        // 1) Käytetään pelaajan valitsema kortti (jos valittu)
        if (cardToProcess != null)
        {
            cardToProcess.Effect?.Invoke(playerNation, squareGrid);
        }
        else
        {
            //Debug.Log("Ei valittua korttia tällä vuorolla (EndTurn).");
        }

        // 2) Mikäli EI ollut Black Turn, käsitellään normaalit korttidekki-logiikat
        if (!isBlackTurn)
        {
            // Palautetaan valittu kortti (jos ei-uniikki) tai poistetaan pysyvästi (jos uniikki)
            if (cardToProcess != null)
            {
                if (cardToProcess.IsUnique)
                {
                    //Debug.Log($"Uniikki kortti {cardToProcess.Name} poistuu pysyvästi.");
                    cardDeck.usedUniques.Add(cardToProcess.Name);
                }
                else
                {
                    // Ei-uniikki palaa pakkaan
                    cardDeck.deck.Add(cardToProcess);
                    //Debug.Log($"Ei-uniikki kortti {cardToProcess.Name} palaa pakkaan.");
                }
            }

            // Kaikki valitsematta jääneet kortit palautetaan myös pakkaan
            foreach (var c in currentDrawnCards)
            {
                if (c == cardToProcess) continue;
                cardDeck.deck.Add(c);
                //Debug.Log($"Valitsematta jäänyt kortti {c.Name} palaa pakkaan.");
            }

            // Sekoitetaan pakka
            cardDeck.ShuffleDeck();
        }
        else
        {
            // *** BLACK TURN ***: Näitä 3 pahaa korttia ei sekoiteta pakkaan.
            // Ne ovat ikään kuin "virtuaalisia" eikä palaa minnekään.
            //Debug.Log("Black Turn päättyi, negatiivisia kortteja ei palauta pakkaan.");
        }

        // 3) Armeijan pienennys: siirretään kuluvan vuoron säästöt seuraavalle vuorolle
        // Jos pelaaja on käyttänyt armeijan pienennyksen, säästö (AccumulatedArmyReductionIncome) siirretään
        playerNation.PreviousArmyReductionIncome = playerNation.AccumulatedArmyReductionIncome;
        playerNation.ResetArmyReductionIncome(); // Nollaa nykyisen säästötilin

        // 4) Tyhjennetään nostetut kortit & valinta
        currentDrawnCards.Clear();
        chosenCard = null;
        isBlackTurn = false; // varmistetaan, ettei seuraava vuoro ole "jäänyt päälle"

        // 5) Sovelletaan peruskasvut (väkiluku, BKT)
        playerNation.ApplyBasicGrowth(squareGrid);
        // Lasketaan nyt, paljonko väkiluku on muuttunut tämän vuoron aikana
        float netChange = playerNation.Population - populationAtTurnStart;
        netPopulationChange = netChange;


        foreach (SquareTile tile in squareGrid.GetAllTiles())
        {
            if (tile.capturedThisTurn)
            {
                tile.capturedThisTurn = false;
                SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetFloat("_Captured", 0.0f);
                    renderer.SetPropertyBlock(mpb);
                }

            }
        }
        //tsekataan iron tilanne
        TradeManager.ProcessIronTradeForNation(GameManager.Instance.playerNation);
        //trade cooldown jos pelaaja ei halunnu myydä ironia niin ai ei spämmi ostotarjouksia
        if (TradeManager.playerTradeDeclineCooldown > 0)
        {
            TradeManager.playerTradeDeclineCooldown--;
        }

        Nation.ProcessPeaceBonus();
        // 6) Siirrytään seuraavaan vuoteen

        if (playerNation.allianceJoinCooldown > 0)
        {
            playerNation.allianceJoinCooldown--;
        }
        GameManager.Instance.playerNation.AttacksThisTurn = 0;

        AdvanceYear();
        turn++;
        UpdateUI();

        ResetArmyReduction();

        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.UpdateMapView();
        }
        if (grid.GetControlledTiles().Where(t => t.controllingNation == GameManager.Instance.playerNation).Count() < 1)
        {
            GameManager.Instance.GameOver();
            return;
        }
        if (aiController != null)
        {
            aiController.ProcessAllAITurns(turn);
        }
        else if (aiManager != null) // Jos käytät vain yhtä AIManageria
        {
            aiManager.ProcessAITurn(turn);
        }
        else
        {
            //Debug.LogWarning("AIControlleria tai AIManageria ei ole asetettu, joten tekoälyvuoroa ei suoriteta.");
        }
        playerNation.UpdateMajorityReligion(squareGrid);
        DiplomacyManager.Instance.DecayRelationships();
        if (turn > 0 && turn % 10 == 0)
        {
            DiplomacyManager.Instance.ApplyRandomRelationshipAdjustment();
        }
        //tässä vaa tarkastetaa että jos ekoäly synty civil warin seurauksena niin se civil war teksti poistuu 10 vuoron jälkee
        if (aiController != null)
        {
            foreach (var aiManager in aiController.GetComponentsInChildren<AIManager>())
            {
                Nation nation = aiManager.aiNation;
                // Tarkistetaan, että valtio on syntynyt sisällissodan seurauksena
                if (nation.CivilWarTurnCreated != -1 && nation.Name.EndsWith(" (Civil war)"))
                {
                    if (GameManager.Instance.CurrentTurn - nation.CivilWarTurnCreated >= 10)
                    {
                        nation.Name = nation.Name.Replace(" (Civil war)", "");
                        //Debug.Log($"Sisällissotavaltion nimi päivitetty: {nation.Name}");
                    }
                }
            }
        }
      
        // 7) Nostetaan uudet kortit (tai Black Turn) seuraavalle vuorolle
        DrawNewTurn();


        // Resetoidaan korttien kokonainen piilotustila ja päivitetään napin teksti
        cardsCompletelyHidden = false;
        UpdateCompleteToggleButtonText();


        currentGameState = GameState.WaitingForInput;
        // Nollataan lipun, jotta seuraava vuoron lopetus voidaan käsitellä
        turnHasEnded = false;

    }

    // Lisää uusi metodi armeijan pienennyksen palauttamiseen
    private void ResetArmyReduction()
    {
        // Palautetaan armaijan pienennysprosentti
        playerNation.ResetArmyReduction();

        // Palautetaan sliderin arvo 100%:iin
        if (armyReductionSlider != null)
        {
            armyReductionSlider.value = 100f;
        }

        // Päivitetään UI vastaamaan uutta arvoa
        UpdateArmyReductionUI();
    }


    public void UpdateUI()
    {
        populationText.text = "Population: " + (playerNation.Population / 1000000f).ToString("F1") + " million";
        gdpText.text = "Currency: " + playerNation.GDP.ToString("F1");
        militaryText.text = "Army Size: " + playerNation.Military;
        technologyText.text = "Technology: " + playerNation.Technology;
        militaryPowerText.text = "Power: " + playerNation.MilitaryPower.ToString("F1");
        turnText.text = "Turn: " + turn;
        moraleText.text = "Morale: " + playerNation.Morale.ToString("F0");
        yearText.text = $"Year: {(currentYear < 0 ? (-currentYear) + " BC" : currentYear.ToString())}";
        populationGrowthText.text = $"Grow: {netPopulationChange:F0}";

        // Armeijan kulutus
        float expenditure = playerNation.Military * playerNation.MilitaryConsumptionRate;
        militaryExpenditureText.text = $"Expenses: {expenditure:F1}";

        // Esimerkiksi UpdateUI‑metodissa:
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        float currentNetIncome = playerNation.CalculateCurrentNetIncome(grid);
        if (currentNetIncome < 0)
        {
            gdpIncomeText.text = $"Net income: {currentNetIncome:F1}";
            gdpIncomeText.color = Color.red;
        }
        else
        {
            gdpIncomeText.text = $"Net income: +{currentNetIncome:F1}";
            gdpIncomeText.color = Color.green;
        }
        /*
        // Käytetään suoraan tämän vuoron nettotuloja
        float displayedIncome = playerNation.TurnNetIncome;
        if (displayedIncome < 0)
        {
            gdpIncomeText.text = $"Net income: {displayedIncome:F2}";
            gdpIncomeText.color = Color.red;
        }
        else
        {
            gdpIncomeText.text = $"Net income: +{displayedIncome:F2}";
            gdpIncomeText.color = Color.green;
        } */

        //Debug.Log($"UpdateUI: MilitaryPower = {playerNation.MilitaryPower:F1}");

        if (armyReductionText != null)
        {
            armyReductionText.text = $"Army size: {playerNation.ArmyReductionPercentage:F0}%";
        }

        bool hasIronTile = grid.GetControlledTiles()
                               .Any(tile => tile.controllingNation == playerNation && tile.HasIron);
        bool ironActive = playerNation.HasActiveIronTrade || (playerNation.HasIronCard && hasIronTile);
        if (ironStatusIcon != null)
        {
            ironStatusIcon.gameObject.SetActive(ironActive);
        }
        // Päivitetään liittobonus-teksti
        Alliance alliance = DiplomacyManager.Instance.GetAllianceForNation(playerNation);
        if (alliance != null)
        {
            float bonus = 0f;
            foreach (Nation member in alliance.Members)
            {
                if (member != playerNation)
                {

                    bonus += member.MilitaryPower * 0.05f;
                }
            }
            allianceBonusText.text = "Alliance Bonus: " + bonus.ToString("F0");
            allianceBonusText.gameObject.SetActive(true);
        }
        else
        {
            allianceBonusText.gameObject.SetActive(false);
        }

        if (NationInfoPanel.Instance != null && NationInfoPanel.Instance.panel.activeSelf)
            NationInfoPanel.Instance.Refresh();
    }


    void UpdateCardButtonsInteractable(bool interactable)
    {
        cardButton1.interactable = interactable;
        cardButton2.interactable = interactable;
        cardButton3.interactable = interactable;
    }

    public void OnApplyArmyReductionButtonClicked()
    {
        if (playerNation.Military <= 1)
        {
            //Debug.Log("ei voi pienentää enempää, testi.");
            return;
        }
        playerNation.ApplyArmyReduction();
        UpdateArmyReductionUI(); // Päivittää sliderin tekstiä
        UpdateExpensesUI();      // Päivittää armeijan kulutuksen näytön

        // Uusi rivi: päivitetään net income heti
        UpdateNetIncomeUI();
    }

    void UpdateExpensesUI()
    {
        // Päivitetään armeijan kulutuksen näyttö
        float expenditure = playerNation.Military * playerNation.MilitaryConsumptionRate;
        militaryExpenditureText.text = $"Expenses: {expenditure:F1}";

        // Päivitetään sliderin teksti
        if (armyReductionText != null)
        {
            armyReductionText.text = $"Army size: {playerNation.ArmyReductionPercentage:F0}%";
        }
    }



    public GameObject notificationWindow;  // UI-paneeli, joka sisältää ilmoituksen
    public TextMeshProUGUI notificationMessage;  // Tekstikomponentti, jossa viesti näkyy

    public void ShowNotificationWindow(string message)
    {
        if (notificationWindow != null && notificationMessage != null)
        {
            notificationMessage.text = message;
            notificationWindow.SetActive(true);
        }
        else
        {
            //Debug.LogWarning("Notification UI-elementtejä ei ole asetettu oikein GameManagerissa!");
        }
    }

    public void HideNotificationWindow()
    {
        if (notificationWindow != null)
        {
            notificationWindow.SetActive(false);
        }
    }

    void AdvanceYear()
    {
        if (eraIndex < eras.Count)
        {
            var (startYear, totalTurns, yearsPerTurn) = eras[eraIndex];
            currentYear += yearsPerTurn;

            if (currentYear == 1500)
            {
                ShowNotificationWindow("Thank you for trying out the game prototype! You can continue playing, but nations will not progress technologically beyond this point.");
            }


            int eraEndYear = startYear + (totalTurns * yearsPerTurn);
            if (currentYear >= eraEndYear && eraIndex < eras.Count - 1)
            {
                eraIndex++;
                //Debug.Log($"Aikakausi vaihtui! Uusi aikakausi alkaa vuodesta {eras[eraIndex].startYear}");
                currentYear = eras[eraIndex].startYear;

                // Kun siirrytään toiselle aikakaudelle (vuosi 0 ja siitä eteenpäin), vaihdetaan korttipakka
                if (eraIndex == 1)
                {
                    cardDeck.InitializeSecondEraCards();
                }
            }
        }
    }

    public void SetClimaticMapView()
    {

        // Asetetaan globaaliksi tilaksi Climatic-näkymä
        CurrentMapView = MapView.Climatic;

        // Haetaan ruudukon skripti ja päivitetään ruutujen ulkonäkö
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.UpdateMapView();
        }
        //Debug.Log("climatic kutsuttu");
    }

    public void SwitchToClimaticMapView()
    {
        //Debug.Log("Climatic kutsuttu");
        GameManager.Instance.CurrentMapView = MapView.Climatic;
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.UpdateMapView();
        }
    }

    public void SetDiplomacyMapView()
    {
        //Debug.Log("Diplomacy kutsuttu");
        // Asetetaan karttanäkymäksi Diplomacy
        CurrentMapView = MapView.Diplomacy;

        // Haetaan ruudukon skripti ja päivitetään ruutujen ulkonäkö
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.UpdateMapView();
        }
    }

    public void SetReligionMapView()
    {
        //Debug.Log("Religion kutsuttu");
        CurrentMapView = MapView.Religion;
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.UpdateMapView();
        }
    }

    public bool showStrategicResources = true;

    public void ToggleStrategicResources()
    {
        showStrategicResources = !showStrategicResources;
        // Päivitetään karttanäkymä, jotta muutokset tulevat heti näkyviin.
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.UpdateMapView();
        }
    }

    void UpdateArmyStatsPreview()
    {
        // Otetaan nykyinen armeijan koko
        int currentMilitary = playerNation.Military;
        // Lasketaan sliderin asettama vähennyskerroin
        float reductionFactor = playerNation.ArmyReductionPercentage / 100f;
        // Lasketaan esikatseltu uusi armeijan koko
        int previewMilitary = Mathf.RoundToInt(currentMilitary * reductionFactor);

        // Lasketaan sotilaallinen voima samoin kuin nykyisessä logiikassa
        float multiplier = 1f;
        if (playerNation.Morale >= 80f)
        {
            multiplier = 1.1f;
        }
        else if (playerNation.Morale < 60f)
        {
            multiplier = 0.9f;
        }
        float previewPower = Mathf.Max(previewMilitary * playerNation.MilitaryTech * multiplier, 1f);

        // Päivitetään UI:n tekstit esikatsellulla arvolla
        militaryText.text = "Army Size: " + previewMilitary;
        militaryPowerText.text = "Power: " + previewPower.ToString("F1");
    }

    public void SetTechnologyMapView()
    {
        //Debug.Log("SetTechnologyMapView kutsuttu");
        CurrentMapView = MapView.Technology;
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.UpdateMapView();
        }
        //Debug.Log("Uusi CurrentMapView: " + CurrentMapView);
    }

    public void SetPlayerNationName(string name)
    {
        if (playerNation != null)
        {
            playerNation.Name = name;
            //Debug.Log($"Pelaajan valtakunnan nimi asetettu: {name}");
        }
    }

    public void SetGameSettings(int aiNationCount, int height, int width, Color color, int seed)
    {
        // Tallennetaan asetukset
        storedAiNationCount = aiNationCount;
        storedWorldHeight = height;
        storedWorldWidth = width;
        storedColor = color;
        storedSeed = seed;

        // Aseta tekoälyvaltioiden määrä, jos käytössä on AIController
        if (aiController != null)
        {
            aiController.numberOfAIEmpires = aiNationCount;
            //Debug.Log($"Tekoälyvaltioiden määrä asetettu: {aiNationCount}");
        }

        // Päivitä ruudukon koko
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.height = height;
            grid.width = width;
            grid.seed = seed;
            grid.GenerateSquareGrid();
        }

        // Aseta pelaajan valtakunnan väri
        if (playerNation != null)
        {
            playerNation.EmpireColor = color;
            //Debug.Log($"Pelaajan valtakunnan väri asetettu: {color}");
        }
    }

    // Tallennetaan start menun asetukset pysyvästi
    private int storedAiNationCount = 50;
    private int storedWorldHeight = 100;
    private int storedWorldWidth = 100;
    private Color storedColor = Color.red;
    private int storedSeed = 0;


    public void FocusOnPlayerNation()
    {
        Nation playerNation = GameManager.Instance.playerNation;
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid == null)
            return;

        // Haetaan pelaajan valtakunnan hallitsemat ruudut
        List<SquareTile> playerTiles = grid.GetAllTiles()
            .Where(t => t.controllingNation == playerNation)
            .ToList();
        if (playerTiles.Count == 0)
        {
            //Debug.Log("Pelaajan valtakunnalla ei ole hallittuja ruutuja.");
            return;
        }

        // Lasketaan ruutujen keskipiste ja siirretään kamera siihen
        Vector3 sum = Vector3.zero;
        foreach (SquareTile tile in playerTiles)
        {
            sum += tile.transform.position;
        }
        Vector3 center = sum / playerTiles.Count;
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(center.x, center.y, mainCam.transform.position.z);
        }
        else
        {
            //Debug.LogWarning("Pääkameraa ei löytynyt.");
        }

        // Käynnistetään korostusanimaatio (kuten StatisticsPanelin HighlightNationCoroutine)
        StartCoroutine(HighlightPlayerNation(playerNation, 4f));
    }

    private IEnumerator HighlightPlayerNation(Nation nation, float duration)
    {
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid == null)
            yield break;

        // Haetaan pelaajan valtakunnan näkyvät ruudut
        List<SquareTile> nationTiles = grid.GetAllTiles()
            .Where(t => t.controllingNation == nation && t.IsVisible)
            .ToList();

        // Tallennetaan alkuperäiset värit
        Dictionary<SquareTile, Color> originalColors = new Dictionary<SquareTile, Color>();
        foreach (SquareTile tile in nationTiles)
        {
            originalColors[tile] = tile.spriteRenderer.color;
        }

        float flashFrequency = 2f; // vilkkuu 2 kertaa sekunnissa
        float flashInterval = 1f / (flashFrequency * 2f);
        float elapsedTime = 0f;
        bool isHighlighted = false;

        while (elapsedTime < duration)
        {
            isHighlighted = !isHighlighted;
            foreach (SquareTile tile in nationTiles)
            {
                if (isHighlighted)
                {
                    // Blendataan alkuperäinen väri 80 % valkoiseksi
                    Color highlightColor = Color.Lerp(originalColors[tile], Color.white, 0.8f);
                    tile.spriteRenderer.color = highlightColor;
                }
                else
                {
                    tile.spriteRenderer.color = originalColors[tile];
                }
            }
            yield return new WaitForSeconds(flashInterval);
            elapsedTime += flashInterval;
        }

        // Palautetaan alkuperäiset värit
        foreach (SquareTile tile in nationTiles)
        {
            tile.spriteRenderer.color = originalColors[tile];
        }
    }


    public GameObject GameOverPanel;
    public Text GameOvertext;

    public void GameOver()
    {
        ShowGameOverScreen();
    }

    public void ShowGameOverScreen()
    {
        if (GameOverPanel == null)
        {
            GameOverPanel = GameObject.Find("GameOverPanel");
        }
        if (GameOvertext == null && GameOverPanel != null)
        {
            GameOvertext = GameOverPanel.transform.Find("GameOvertext")?.GetComponent<Text>();
        }

        if (GameOverPanel != null && GameOvertext != null)
        {
            GameOvertext.text = "Your realm was conquered.";
            GameOverPanel.SetActive(true);

            Button gameOverButton = GameOverPanel.transform.Find("GameOverButton")?.GetComponent<Button>();
            if (gameOverButton != null)
            {
                gameOverButton.onClick.RemoveAllListeners();
                gameOverButton.onClick.AddListener(EndGame);
                Text buttonText = gameOverButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Game Over!";
                }
            }
            else
            {
                //Debug.LogWarning("GameOverButtonia ei löytynyt GameOverPanelin sisältä!");
            }
        }
        else
        {
            //Debug.LogWarning("GameOverPanelia tai GameOvertextiä ei löytynyt!");
        }
    }

    public void EndGame()
    {
        Destroy(gameObject);
        SceneManager.LoadScene("Menu");
    }


    public void ToggleCardsVisibility()
    {
        cardsHidden = !cardsHidden;
    }

    public void SetupToggleButton()
    {
        if (toggleCardsVisibilityButton != null)
        {
            toggleCardsVisibilityButton.onClick.AddListener(ToggleCardsVisibility);
        }
    }

    // Uusi muuttuja korttien kokonaiselle piilottamiselle
    private bool cardsCompletelyHidden = false;

    // Viite uuteen painikkeeseen, jonka tekstiä päivitämme On/Off tilan mukaan.
    // Lisää tämä kenttä ja määritä se Inspectorissa.
    public Button completeToggleCardsButton;
    public void ToggleCompleteCardsVisibility()
    {
        // Kytketään korttien kokonainen näkyvyys päälle/pois
        cardsCompletelyHidden = !cardsCompletelyHidden;

        // Oletuksena kortit näkyvät korttipainikkeissa: cardButton1, cardButton2, cardButton3.
        if (cardButton1 != null)
            cardButton1.gameObject.SetActive(!cardsCompletelyHidden);
        if (cardButton2 != null)
            cardButton2.gameObject.SetActive(!cardsCompletelyHidden);
        if (cardButton3 != null)
            cardButton3.gameObject.SetActive(!cardsCompletelyHidden);

        // Päivitetään painikkeen teksti sen mukaan, ovatko kortit näkyvissä
        UpdateCompleteToggleButtonText();
    }

    private void UpdateCompleteToggleButtonText()
    {
        if (completeToggleCardsButton != null)
        {
            // Jos käytössä on tavallinen UI.Text:
            Text buttonText = completeToggleCardsButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = cardsCompletelyHidden ? "Off" : "On";
            }
            else
            {
                // Jos käytetään TextMeshProa:
                TMPro.TextMeshProUGUI tmpText = completeToggleCardsButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.text = cardsCompletelyHidden ? "Off" : "On";
                }
            }
        }
    }

    public void ZoomToPlayerNation()
    {
        Nation playerNation = GameManager.Instance.playerNation;
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid == null)
            return;

        // Haetaan pelaajan hallitsemat ruudut
        List<SquareTile> playerTiles = grid.GetAllTiles()
            .Where(t => t.controllingNation == playerNation)
            .ToList();
        if (playerTiles.Count == 0)
        {
            //Debug.Log("Pelaajan valtakunnalla ei ole hallittuja ruutuja.");
            return;
        }

        // Lasketaan ruutujen keskipiste
        Vector3 sum = Vector3.zero;
        foreach (SquareTile tile in playerTiles)
        {
            sum += tile.transform.position;
        }
        Vector3 center = sum / playerTiles.Count;

        // Siirretään pääkamera keskipisteen kohdalle ilman highlightausta
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(center.x, center.y, mainCam.transform.position.z);
        }
        else
        {
            //Debug.LogWarning("Pääkameraa ei löytynyt.");
        }
    }

    public bool IsProcessingTurn
    {
        get { return currentGameState == GameState.ProcessingTurn; }
    }

    public CanvasGroup mainCanvasGroup; // liitä tämä Inspectorissa

    public bool IsTurnTransitioning { get; private set; }

    // Kun vuoron vaihto alkaa:
    public void StartTurnTransition()
    {
        IsTurnTransitioning = true;
        if (mainCanvasGroup != null)
            mainCanvasGroup.blocksRaycasts = false;
    }

    // Kun uusi vuoro alkaa:
    public void EndTurnTransition()
    {
        IsTurnTransitioning = false;
        if (mainCanvasGroup != null)
            mainCanvasGroup.blocksRaycasts = true;
    }


    private bool inputBlocked = false;

    public void SetAllianceMapView()
    {
        //Debug.Log("Alliance-näkymä asetettu");
        CurrentMapView = MapView.Alliance;
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid != null)
        {
            grid.UpdateMapView();
        }
    }

    void UpdateNetIncomeUI()
    {
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        float currentNetIncome = GameManager.Instance.playerNation.CalculateCurrentNetIncome(grid);
        if (currentNetIncome < 0)
        {
            gdpIncomeText.text = $"Net income: {currentNetIncome:F1}";
            gdpIncomeText.color = Color.red;
        }
        else
        {
            gdpIncomeText.text = $"Net income: +{currentNetIncome:F1}";
            gdpIncomeText.color = Color.green;
        }
    }

    public static bool ForceGrasslandStart = false;

    IEnumerator RevealStartingTileCoroutine(SquareTile startingTile, int revealRadius)
    {
        // Odota frame:n loppuun tai pienen sekunnin
        yield return new WaitForEndOfFrame();
        // ehkä myös kokeilla yield return new WaitForSeconds(0.1f);

        // Paljasta starting tilen ympäristö
        if (startingTile != null)
        {
            //  Debug.Log("Viiveen jälkeen paljastetaan starting tile: (" + startingTile.X + ", " + startingTile.Y + ")");
            // Kutsutaan paljastusmetodia uudelleen
            startingTile.SetVisibility(true, true);

            
            var surroundingTiles = FindObjectOfType<SquareGrid>().GetTilesInRadius(startingTile, revealRadius);
            foreach (var tile in surroundingTiles)
            {
                tile.SetVisibility(true, true);
            }
        }
    }


    //tipsit
    public GameObject tipsWindow;
    public Text tipText;
    public Button tipWindowButton;
    public Button tipsOffButton;
    private bool tipsEnabled = true;
    private string[] tips = {
"Tip: You can make a maximum of 2 attacks per turn.",
"Tip: If your relations with other realms are high enough, you can propose an alliance to them.",
"Tip: A card’s rarity is indicated by its border.",
"Tip: Cards have different rarities: Normal, Rare, Epic, and Legendary.",
"Tip: Lower morale increases the likelihood of a Black Turn: you will have to choose a bad card.",
"Tip: Low morale can prevent you from attacking other realms!",
"Tip: Some tiles are more favorable for population growth than others.",
"Tip: Religions affect relations between realms.",
"Tip: If there is no iron in your region, your realm should buy it from another realm.",
"Tip: You can only trade with neighboring realms." ,
"Tip: You can only attack neighboring realms." ,
"Tip: Left‑clicking on a realm opens its detailed information." ,
"Tip: Right‑clicking on a realm opens a brief overview of its information." ,
"Tip: An alliance grants a defense bonus when defending, based on allies military strength." ,
"Tip: Power depends on army size, military technology level, and morale." ,
"Tip: Clicking on a realm’s entry in the Statistics menu will locate it on the map.",
"Tip: Constant warfare causes other realms to view you as a threat.",
"Tip: Peacefulness leads other realms to view you as a good ally.",
"Tip: If the realm’s savings drop to zero, its army and morale will weaken.",
"Tip: Increasing your technology level and advancing through time will open up new cards for you.",
"Tip: Expanding your realm lowers morale. Attacking does too.",
"Tip: If a realm's morale becomes too low, it risks falling into civil war.",
"Tip: If you go on the offensive, you should have at least triple the Power of the defender!"


    };

    void ShowTipWindow()
    {
        if (!tipsEnabled) return;
        // Jos vuoro on ensimmäinen, näytetään aina sama viesti
        if (turn == 0)
        {
            tipText.text = "Tip: Left‑clicking on a realm opens its detailed information.";
        }
        else
        {
            int index = Random.Range(0, tips.Length);
            tipText.text = tips[index];
        }
        tipsWindow.SetActive(true);
    }


    void HideTipWindow()
    {
        tipsWindow.SetActive(false);
    }

    public void DisableTips()
    {
        tipsEnabled = false;
        tipsWindow.SetActive(false);
    }

    public void ToggleTips()
    {
        tipsEnabled = !tipsEnabled; // Päivitetään napin teksti: jos tipsit päällä, näytetään "Tips: On", muuten "Tips: Off"
        if (tipsOffButton != null)
        {
            Text buttonText = tipsOffButton.GetComponentInChildren<Text>();
            if (buttonText != null) buttonText.text = tipsEnabled ? "Tips: On" : "Tips: Off";
        }

    }



  
 
}
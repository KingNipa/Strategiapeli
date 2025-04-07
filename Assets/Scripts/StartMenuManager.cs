using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
public class StartMenuManager : MonoBehaviour
{
    [Header("Pelin otsikko")]
    public TMP_Text gameTitleTMP;
    public TMP_Text gameVersioTMP;

    [Header("Aloitusruudun UI-elementit")]
    public GameObject startMenuPanel; // Alkuper�inen ruutu, jossa on Play ja Quit
    public Button playButton;
    public Button quitButton;
    public Button manualButton;
    public Button upcomingDevButton;

    [Header("Asetuspaneelin UI-elementit")]
    public GameObject settingsPanel;  // Paneeli, jossa pelaaja sy�tt�� asetukset
    public InputField nationNameInput;
    public Slider aiNationSlider;
    public InputField heightInput;
    public InputField widthInput;
    public Image colorDisplay;
    public Button startGameButton;    // Painike, joka k�ynnist�� pelin asetusten j�lkeen

    private Color selectedColor = Color.red; // Oletusv�ri
    private ColorPaletteButton selectedColorButton;

    public InputField seedInput;

    public Text aiNationValueText;

    public Button randomizeSeedButton;

    public Button goodStartingButton;
    public Button randomStartingButton;

    private bool forceGrasslandStart = false;
    private bool isGoodSelected = true; 

    [Header("Manuaali UI-elementit")]
    public GameObject manualPanel;  // Paneeli, jossa esitet��n manuaali sis�lt��
    public Button manualBackButton; // Painike, jolla palataan takaisin aloitusn�ytt��n
    public TMP_Text manualText;

    [Header("Upcoming Development UI-elementit")]
    public GameObject upcomingDevPanel;   // Paneeli, jossa n�ytet��n tulevat p�ivitykset
    public TMP_Text upcomingDevText;      // Tekstikentt� tuleville p�ivityksille
    public Button upcomingDevBackButton;  // Back-painike, jolla palataan alkuvalikkoon




    void Start()
    {
        // Asetetaan pelin otsikon teksti ja tyyli
        if (gameTitleTMP != null)
        {
            gameTitleTMP.text = "Arc of the Realms";
        }
        //oletusasetukset
        aiNationSlider.value = 50;
        heightInput.text = "60";
        widthInput.text = "40";
        HighlightStartingSelection();
        // Alkuper�isen ruudun painikkeet
        playButton.onClick.AddListener(ShowSettingsPanel);
        quitButton.onClick.AddListener(OnQuitButton);
        manualButton.onClick.AddListener(ShowManualPanel);
        upcomingDevButton.onClick.AddListener(ShowUpcomingDevPanel);
        // Asetuspaneelin painike
        startGameButton.onClick.AddListener(OnStartGameButton);
        goodStartingButton.onClick.AddListener(OnGoodStartingSelected);
        randomStartingButton.onClick.AddListener(OnRandomStartingSelected);

        EventSystem.current.SetSelectedGameObject(goodStartingButton.gameObject);
        // M��rit� manuaalin sis�lt�
        string manualContent = @"
1. Turn Structure and Cards:
The game starts in 3000 BC and ends in 2025 (in this demo, it ends in 1500). Each turn advances time by a certain amount. 
Early on, one turn corresponds to multiple years, whereas later on each turn corresponds to a single year.
On each turn, you receive three cards and must choose one of them. 
The cards come in different rarities: normal, rare, epic, and legendary. On some turns, you may have to pick from three unfavorable cards.

2. Population:
Population in the game grows thanks to fertile terrain and controlled tiles. Additionally, certain cards boost population growth.

3. Morale:
Morale affects not only your army�s fighting spirit but also the types of cards you receive each turn. 
High morale improves combat effectiveness and reduces the risk of negative events, while low morale can lead to harmful effects. 
Make sure your morale does not drop too low. If your morale is too low, you won�t be able to attack other realms, and the risk of civil war will also rise.

4. Money and Income:
Income is generated by tile production, population output, and any additional bonuses. The greater your income, the better you can maintain and grow your army. 
Exercise caution, as hitting zero money weakens both your people�s morale and your army.

5. The Army and Military Power:
Your army defends your realm. You can also use it to attack. Its size depends on the number of soldiers, but the final military power also factors in morale and the level of military tech. 

6. Technology:
Your level of technology determines how effective the cards you can obtain each turn will be. Advancing your technology grants strategic advantages that boost both your military power and economic efficiency.

7. Resources:
Managing resources opens up access to new cards and strengthens your realm. Ensure that your realm has resources like iron or consider purchasing them on the market.

8. Religion and Diplomacy:
Religion influences relationships between realms. Developing religion and maintaining a majority faith can improve diplomacy, open new strategic opportunities, and help avert conflicts. 
Poor relations with other realms can drive away potential trade partners. If your realm is warlike, it may negatively affect how other realms perceive you.

9. Climate and Terrain:
Different climate conditions affect agricultural efficiency and population growth. Some tiles provide more favorable terrain that benefits both population growth and economic development. 
Certain terrains are better suited for specific resources than others.

10. Special Events and Eras:
The game progresses through various eras, during which turn lengths and card themes change. Random events, such as the Black Death, can dramatically impact realms.

11. User Interface:
You can click on the countries using the left and right mouse buttons. You have access to several different map views. By clicking the �Statistics� button, you can view detailed information about the realms: population, the size of each realm, etc. . 
If you want to know the location of a realm, you can click on it in the statistics window.";
        manualText.text = manualContent;


        string upcomingDevContent = @"What's Coming:

An improved user interface
Images for the cards
Bug fixes
New technological eras: Colonialism, Industrialization, Modern Age
Ideologies
Ships and maritime connections
Enhanced warfare and better attack mechanics
Game saving functionality
World creation tools
A mobile version
More statistics
New cards
Ending 
More advanced world creation
Audio";
        upcomingDevText.text = upcomingDevContent;


        // Piilotetaan asetuspaneeli alussa
        settingsPanel.SetActive(false);
        manualPanel.SetActive(false);
        upcomingDevPanel.SetActive(false);

        manualBackButton.onClick.AddListener(ShowStartMenuPanel);
        upcomingDevBackButton.onClick.AddListener(ShowStartMenuPanel);

        aiNationSlider.onValueChanged.AddListener(UpdateAINationText);
        UpdateAINationText(aiNationSlider.value); // P�ivitt�� aloitusarvon
                                                  // Liit� randomisoidulle napille kuuntelija

       

        if (randomizeSeedButton != null)
        {
            randomizeSeedButton.onClick.AddListener(RandomizeSeed);
        }

    }

    void RandomizeSeed()
    {
        int randomSeed = Random.Range(0, 40000); // seedin raja-arvot 0-40k
        seedInput.text = randomSeed.ToString();
    }

    // Kun Playa painetaan alkuper�isell� ruudulla
    void ShowSettingsPanel()
    {
        startMenuPanel.SetActive(false);   // Piilotetaan alkuper�inen ruutu
        settingsPanel.SetActive(true);       // N�ytet��n asetuspaneeli
        EventSystem.current.SetSelectedGameObject(goodStartingButton.gameObject);
        OnGoodStartingSelected(); 
        goodStartingButton.Select();
    }

    void ShowManualPanel()
    {
        startMenuPanel.SetActive(false);  // Piilotetaan alkuper�inen ruutu
        manualPanel.SetActive(true);      // N�ytet��n manuaalipaneeli
    }

    void ShowUpcomingDevPanel()
    {
        startMenuPanel.SetActive(false);
        upcomingDevPanel.SetActive(true);
    }

    // Kun Back-painiketta painetaan manuaalipaneelissa
    void ShowStartMenuPanel()
    {
        // Piilotetaan mahdollisesti auki olevat paneelit
        manualPanel.SetActive(false);
        upcomingDevPanel.SetActive(false);
        settingsPanel.SetActive(false);
        // N�ytet��n alkuper�inen aloitusruutu
        startMenuPanel.SetActive(true);
    }

    // Kun pelaaja painaa asetuspaneelissa "Start Game"
    void OnStartGameButton()
    {
        if (!ValidateInputs())
        {
            return;
        }
        GameManager.ForceGrasslandStart = forceGrasslandStart;

        string nationName = nationNameInput.text.Trim();
        int aiNations = Mathf.RoundToInt(aiNationSlider.value);
        int gridHeight = int.Parse(heightInput.text);
        int gridWidth = int.Parse(widthInput.text);
        int worldSeed = int.Parse(seedInput.text);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerNationName(nationName);
            // T�ss� asetetaan ja tallennetaan k�ytt�j�n sy�tt�m�t asetukset
            GameManager.Instance.SetGameSettings(aiNations, gridHeight, gridWidth, selectedColor, worldSeed);
        }
        else
        {
            //Debug.LogError("GameManager.Instance on null! Varmista, ett� GameManager on asetettu oikein ja pysyy scenejen v�lill�.");
        }

        SceneManager.LoadScene("GameScene");
    }



    public Text errorText; // Lis�� t�m� virheviestikomponenttina Unity-editorissa

    bool ValidateInputs()
    {
        // Reset error messages at the beginning
        errorText.text = "";
        bool valid = true;

        // Validate realm name
        string nationName = nationNameInput.text.Trim();
        if (nationName.Length < 1 || nationName.Length > 25)
        {
            errorText.text += "Realm name must be between 1 and 25 characters.\n";
            valid = false;
        }

        // Validate that a color is selected
        if (selectedColorButton == null)
        {
            errorText.text += "You must select a color.\n";
            valid = false;
        }

        // Validate number of AI nations
        int aiNations = Mathf.RoundToInt(aiNationSlider.value);
        if (aiNations < 1 || aiNations > 150)
        {
            errorText.text += "Number of AI nations must be between 1 and 150.\n";
            valid = false;
        }

        // Validate world height
        int height;
        if (!int.TryParse(heightInput.text, out height) || height < 20 || height > 100)
        {
            errorText.text += "World width must be between 20 and 100.\n";
            valid = false;
        }

        // Validate world width
        int width;
        if (!int.TryParse(widthInput.text, out width) || width < 20 || width > 100)
        {
            errorText.text += "World height must be between 20 and 100.\n";
            valid = false;
        }

        int seed;
        if (string.IsNullOrWhiteSpace(seedInput.text))
        {
            errorText.text += "Seed must be provided.\n";
            valid = false;
        }
        else if (!int.TryParse(seedInput.text, out seed) || seed < 0 || seed > 40000)
        {
            errorText.text += "Seed must be a number between 0 and 40000.\n";
            valid = false;
        }

        return valid;
    }


    public void OnQuitButton()
    {
        Application.Quit();
    }

    // T�t� metodia kutsutaan esim. v�ri� valittaessa
    
    public void OnColorSelected(Color newColor)
    {
        selectedColor = newColor;
        if (colorDisplay != null)
        {
            colorDisplay.color = newColor;
        }
    } 

    // Metodi, joka huolehtii valitun v�ripainikkeen highlightauksesta
    public void HighlightSelectedButton(ColorPaletteButton selectedButton)
    {
        // Poista highlight edellisest� painikkeesta, jos sellainen on
        if (selectedColorButton != null && selectedColorButton != selectedButton)
        {
            selectedColorButton.SetHighlight(false);
        }

        // Tallenna uusi valinta ja aktivoi highlight
        selectedColorButton = selectedButton;
        selectedColorButton.SetHighlight(true);
    }

    void UpdateAINationText(float value)
    {
        int realms = Mathf.RoundToInt(value);
        aiNationValueText.text = "Number of realms: " + realms;
    }

    public void OnGoodStartingSelected()
    {
        isGoodSelected = true;
        forceGrasslandStart = true; 
        HighlightStartingSelection();
    }

    public void OnRandomStartingSelected()
    {
        isGoodSelected = false;
        forceGrasslandStart = false; 
        HighlightStartingSelection();
    }


    private void HighlightStartingSelection()
    {
        // Hae kummankin napin nykyiset v�ripaletit
        ColorBlock goodColors = goodStartingButton.colors;
        ColorBlock randomColors = randomStartingButton.colors;

        if (isGoodSelected)
        {
            // Vihre� tila valitulle painikkeelle kaikissa tiloissa
            goodColors.normalColor = Color.green;
            goodColors.highlightedColor = Color.green;
            goodColors.pressedColor = Color.green;
            goodColors.selectedColor = Color.green;

            // Muutetaan toisen painikkeen v�rit normaaleiksi
            randomColors.normalColor = Color.white;
            randomColors.highlightedColor = Color.white;
            randomColors.pressedColor = Color.white;
            randomColors.selectedColor = Color.white;
        }
        else
        {
            // Vihre� tila toisen painikkeen osalta
            randomColors.normalColor = Color.green;
            randomColors.highlightedColor = Color.green;
            randomColors.pressedColor = Color.green;
            randomColors.selectedColor = Color.green;

            // Ensimm�isen painikkeen v�rit normaaleiksi
            goodColors.normalColor = Color.white;
            goodColors.highlightedColor = Color.white;
            goodColors.pressedColor = Color.white;
            goodColors.selectedColor = Color.white;
        }

        goodStartingButton.colors = goodColors;
        randomStartingButton.colors = randomColors;
    }

}

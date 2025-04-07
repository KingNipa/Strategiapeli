using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Viite main menu -canvas-objektiin GameSceness‰.
    // Varmista, ett‰ t‰m‰ kentt‰ on asetettu Inspectorissa tai ett‰ sen nimi GameSceness‰ on "MainMenuCanvas"
    public GameObject mainMenuCanvas;
    public SquareGrid squareGrid; // Jos k‰yt‰t esimerkiksi RevealEntireMap()-metodia

    // UI-painike, joka n‰kyy p‰‰valikossa
    public Button muteButton;
    // Painikkeen tekstikomponentti
    public Text muteButtonText;

    void Start()
    {
        // P‰ivitet‰‰n painikkeen teksti alussa
        UpdateMuteButtonText();
        // Liitet‰‰n ToggleMute-metodi painikkeen onClick-tapahtumaan
        if (muteButton != null)
        {
            muteButton.onClick.AddListener(ToggleMute);
        }
    }

    public void ToggleMute()
    {
        AudioListener.pause = !AudioListener.pause;
        UpdateMuteButtonText();
    }

    void UpdateMuteButtonText()
    {
        if (muteButtonText != null)
        {
            muteButtonText.text = AudioListener.pause ? "Unmute" : "Mute";
        }
    }

    void Awake()
    {
        // Yritet‰‰n hakea mainMenuCanvas, jos sit‰ ei ole viel‰ asetettu Inspectorissa.
        if (mainMenuCanvas == null)
        {
            mainMenuCanvas = GameObject.Find("MainMenuCanvas");
            if (mainMenuCanvas == null)
            {
                //Debug.LogWarning("MainMenuCanvas ei ole asetettu tai lˆydy GameScene:st‰.");
            }
        }
        // Varmistetaan, ett‰ menu on piilotettu alussa
        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);
    }

    // Jos MainMenuManager on osa persistent (DontDestroyOnLoad) GameManageria,
    // kannattaa myˆs p‰ivitt‰‰ viittaukset, kun GameScene ladataan.
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jos GameScene ladataan, haetaan tarvittavat viittaukset uudelleen
        if (scene.name == "GameScene")
        {
            // Hae mainMenuCanvas uudelleen (jos sen nimi on "MainMenuCanvas")
            if (mainMenuCanvas == null)
            {
                mainMenuCanvas = GameObject.Find("MainMenuCanvas");
            }
            // Varmista, ett‰ se on piilotettu alussa
            if (mainMenuCanvas != null)
                mainMenuCanvas.SetActive(false);

            // Varmistetaan, ett‰ muteButton on asetettu
            if (muteButton != null)
            {
                // Poistetaan mahdolliset edelliset tapahtumakuuntelijat
                muteButton.onClick.RemoveAllListeners();
                // Lis‰t‰‰n ToggleMute-metodi painikkeen klikkaustapahtumaan
                muteButton.onClick.AddListener(ToggleMute);
            }

            // hakea myˆs SquareGridin, mik‰li sit‰ k‰ytet‰‰n RevealEntireMap()-metodissa, hmm?
            squareGrid = FindObjectOfType<SquareGrid>();
        }
    }

    private int qPressCount = 0;
    private int rPressCount = 0;
    private float lastQPressTime = 0f;
    private float lastRPressTime = 0f;
    private float maxIntervalBetweenPresses = 1f; // Esim. 1 sekunt

    void Update()
    {
        // ESC togglaa p‰‰valikkoa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMainMenu();
        }

        // Q-painikkeen k‰sittely (esim. kartan paljastus)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (Time.time - lastQPressTime <= maxIntervalBetweenPresses)
            {
                qPressCount++;
            }
            else
            {
                qPressCount = 1; // jos liian pitk‰ v‰li, aloitetaan alusta
            }
            lastQPressTime = Time.time;

            if (qPressCount >= 3)
            {
                // Suoritetaan karttakoodi (vain, jos p‰‰valikko on auki)
                if (mainMenuCanvas != null && mainMenuCanvas.activeSelf)
                {
                    RevealEntireMap();
                    //Debug.Log("Cheat activated: Map revealed");
                }
                qPressCount = 0;
            }
        }

        // R-painikkeen k‰sittely (esim. power cheat)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (Time.time - lastRPressTime <= maxIntervalBetweenPresses)
            {
                rPressCount++;
            }
            else
            {
                rPressCount = 1;
            }
            lastRPressTime = Time.time;

            if (rPressCount >= 3)
            {
                GameManager.Instance.playerNation.cheatPowerBonus += 20000f;
                //Debug.Log("Cheat activated: Added 20000 power bonus");
                GameManager.Instance.UpdateUI();
                rPressCount = 0;
            }
        }
    }

    public void ToggleMainMenu()
    {
        if (mainMenuCanvas != null)
        {
            bool newState = !mainMenuCanvas.activeSelf;
            mainMenuCanvas.SetActive(newState);
            // Jos menu on auki, pys‰ytet‰‰n aika
            Time.timeScale = newState ? 0 : 1;
        }
    }



    public void CloseMainMenu()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(false);
            Time.timeScale = 1;
        }
    }

    public void RevealEntireMap()
    {
        if (squareGrid == null)
        {
            squareGrid = FindObjectOfType<SquareGrid>();
            if (squareGrid == null)
            {
                //Debug.LogWarning("SquareGrid ei lˆytynyt, karttaa ei voida paljastaa.");
                return;
            }
        }
        foreach (var tile in squareGrid.GetAllTiles())
        {
            tile.SetVisibility(true, true);
        }
    }


}



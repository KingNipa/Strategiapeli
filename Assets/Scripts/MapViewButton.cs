using UnityEngine;

public class MapViewButton : MonoBehaviour
{
    public void OnDiplomacyButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDiplomacyMapView();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnTechnologyButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTechnologyMapView();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnReligionButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetReligionMapView();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnClimaticButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetClimaticMapView();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnResourceButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleStrategicResources();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }
    //pistetään tää vielä tänne säätelee armeijan kokoo ettei tarvii tehä sille omaa skriptii:
    public void OnConfirmButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnApplyArmyReductionButtonClicked();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnRealmButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FocusOnPlayerNation();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnMainBackClicked()
    {
        if (GameManager.Instance != null)
        {
            MainMenuManager mainMenuManager = GameManager.Instance.GetComponent<MainMenuManager>();
            if (mainMenuManager != null)
            {
                mainMenuManager.CloseMainMenu();
            }
            else
            {
                //Debug.LogError("MainMenuManager-komponenttia ei löytynyt GameManager-objektista!");
            }
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnCardButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleCardsVisibility();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnCompleteCardButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleCompleteCardsVisibility();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnAllianceButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetAllianceMapView();
        }
        else
        {
            //Debug.LogError("GameManager-instanssia ei löytynyt!");
        }
    }

    public void OnTipsToggleButtonClicked()
    {
        if (GameManager.Instance != null)
        { 
            GameManager.Instance.ToggleTips(); 
        } 
    }

    public void OnToggleMuteClicked()
    {
        if (GameManager.Instance != null)
        {
            MainMenuManager mainMenuManager = GameManager.Instance.GetComponent<MainMenuManager>();
            if (mainMenuManager != null)
            {
                mainMenuManager.ToggleMute();
            }
            else
            {
                Debug.LogError("MainMenuManager-komponenttia ei löytynyt GameManager-objektista!");
            }
        }
        else
        {
            Debug.LogError("GameManager-instanssia ei löytynyt!");
        }

    }

}
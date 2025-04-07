using UnityEngine;
using UnityEngine.UI;

public class ColorPaletteButton : MonoBehaviour
{
    public Color buttonColor = Color.white; // Aseta Inspectorissa
    public StartMenuManager menuManager; // Liit‰ t‰m‰ Inspectorissa

    private Outline outline;

    void Awake()
    {
        // Hae Outline-komponentti ja varmista, ett‰ se on oletuksena pois p‰‰lt‰
        outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClicked);
        }
    }

    void OnButtonClicked()
    {
        // Ilmoitetaan StartMenuManagerille valinta ja pyydet‰‰n highlightausta
        if (menuManager != null)
        {
            menuManager.OnColorSelected(buttonColor);
            menuManager.HighlightSelectedButton(this);
        }
        else
        {
            //Debug.LogError("StartMenuManager-viitett‰ ei ole asetettu ColorPaletteButton-skriptiss‰!");
        }
    }

    // Metodi, jolla voidaan aktivoida/deaktivoida highlight
    public void SetHighlight(bool highlight)
    {
        if (outline != null)
        {
            outline.enabled = highlight;
        }
    }
}


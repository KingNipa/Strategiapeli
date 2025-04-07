using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
public class CardUIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Uudet viitteet erillisille tekstielementeille
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardDescriptionText;

    public Image cardIcon;
    public Outline cardOutline;

    // Kenttä highlightausta varten 
    public Color highlightColor = Color.yellow;
    // Tallennetaan oletusväri
    private Color defaultOutlineColor;

    private Vector2 defaultPosition;
    private Vector2 hiddenPosition;
    public float hiddenOffset = 20f;  // Kuinka paljon kortti siirtyy alaspäin, kun se on piilotettu
    public float hoverOffset = 20f;    // Kuinka paljon kortti nousee hiiren ollessa sen päällä 20
    public float moveSpeed = 10f;      // Liikenopeus
    private bool isHovered = false;

    void Start()
    {
        // Haetaan RectTransform-komponentti ja asetetaan lähtö- ja piilotusasemat anchoredPositionin avulla
        RectTransform rect = GetComponent<RectTransform>();
        defaultPosition = rect.anchoredPosition;
        hiddenPosition = defaultPosition + new Vector2(0, -hiddenOffset);    
        
    }


    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsTurnTransitioning)
            return; // Estää kameran inputit vuoron vaihtohetkellä
        // Oletuksena kortti pysyy lähtöasennossa
        Vector2 targetPosition = defaultPosition;

        // Jos GameManagerin cardsHidden on true, asetetaan kohdeasennokseksi hiddenPosition.
        // Jos hiiri on kortin päällä, lisätään hover-offset.
        if (GameManager.Instance != null && GameManager.Instance.cardsHidden)
        {
            targetPosition = hiddenPosition;
            if (isHovered)
            {
                targetPosition += new Vector2(0, hoverOffset);
            }
        }

        // Sujuva siirtyminen käyttämällä RectTransformin anchoredPositiona
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, Time.deltaTime * moveSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    // Päivitetty metodi, joka asettaa kortin nimen ja kuvauksen erikseen
    public void SetCardData(string name, string description, Rarity rarity, string category, Sprite iconSprite, bool isBlackTurnCard = false)
    {
        if (cardNameText != null)
        {
            cardNameText.text = name;
            cardNameText.color = Color.black;
        }
        if (cardDescriptionText != null)
        {
            cardDescriptionText.text = description;
            cardDescriptionText.color = Color.black;
        }
        if (cardIcon != null && iconSprite != null)
        {
            cardIcon.sprite = iconSprite;
        }
        if (cardOutline != null)
        {
            // Määritellään oletusväri rarityn tai mustan vuoron mukaan
            if (isBlackTurnCard)
                defaultOutlineColor = Color.black;
            else
                defaultOutlineColor = GetColorForRarity(rarity);
            cardOutline.effectColor = defaultOutlineColor;
        }
    }

    // Metodi, jolla määritellään värit raritylle
    private Color GetColorForRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Rare:
                return new Color(0.2f, 0.6f, 1f); // Sinertävä
            case Rarity.Epic:
                return new Color(0.7f, 0.3f, 1f); // Violetti
            case Rarity.Legendary:
                return new Color(1f, 0.5f, 0f);   // Oranssi
            default:
                return Color.white;              // Normal
        }
    }

    public void SetHighlight(bool highlighted)
    {
        if (cardOutline != null)
        {
            // Jos kortti on valittu, käytetään highlightColor, muuten palautetaan oletusväri
            cardOutline.effectColor = highlighted ? highlightColor : defaultOutlineColor;
        }
    }

 

}



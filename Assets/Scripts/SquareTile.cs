using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
public class SquareTile : MonoBehaviour
{
    public enum TerrainType { Grassland, Desert, Forest, Mountain, Tundra, Snow, Savannah, Merivesi, MakeaVesi }

    private TerrainType terrain;
    public TerrainType Terrain
    {
        get => terrain;
        set
        {
            terrain = value;
            AssignTerrainEffects();
            AssignResourceProduction();
            AssignResourceProduction();
        }
    }

    public bool HasIron { get; private set; } = false;
    public GameObject ironMarkerPrefab;
    private GameObject ironMarkerInstance;

    public bool capturedThisTurn = false;

    public Nation controllingNation;
    public bool IsControlled { get; private set; } // Määrittää, kuka omistaa neliön
    public bool IsVisible { get; private set; }      // Determines if the tile is currently visible
    public bool HasBeenExplored { get; private set; } // Determines if the tile has been explored before

    // Uskonto, joka on voimassa kyseisellä ruudulla (null, jos ei uskontoa)
    public Religion Religion { get; set; } = null;

    public int X { get; set; }
    public int Y { get; set; }

    // Overlay sprites for visibility (e.g., fog overlay)
    public SpriteRenderer fogOverlay; // Assign a semi-transparent dark sprite in the Inspector


    // Resurssit
    public int FoodProduction { get; private set; }
    public int MineralProduction { get; private set; }

    public SpriteRenderer spriteRenderer; // Julkinen tarkistamisen helpottamiseksi

  

    void Awake()
    {
        if (fogOverlay != null)
        {
            fogOverlay.enabled = false;
            fogOverlay.gameObject.SetActive(false);
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();

        if (sr != null && boxCollider != null)
        {
            // Asetetaan colliderin koko spritea vastaavaksi
            boxCollider.size = sr.sprite.bounds.size;
            // Lasketaan sprite-rendererin bounds.center (maailman koordinaateissa)
            // ja muunnetaan se paikallisiksi koordinaateiksi.
            Vector2 computedOffset = (Vector2)transform.InverseTransformPoint(sr.bounds.center);
            boxCollider.offset = computedOffset;
        }

        // Initialize visibility
        SetVisibility(false, false);
    }

    void Start()
    {
        // Jätetty tyhjäksi tai siirretty logiikka Awake-sisään
    }

    public void UpdateVisuals()
    {
        // Jos ruutua ei ole tutkittu, piilotetaan markerit
        if (!HasBeenExplored)
        {
            if (ironMarkerInstance != null)
                ironMarkerInstance.SetActive(false);
            if (cityMarkerInstance != null)
                cityMarkerInstance.SetActive(false);
            if (castleMarkerInstance != null)
                castleMarkerInstance.SetActive(false);
            if (portMarkerInstance != null)
                portMarkerInstance.SetActive(false);

            return;
        }

        // Päivitetään kaupungin marker (jos sitä käytetään)
        if (hasCity && cityMarkerInstance != null)
        {
            cityMarkerInstance.SetActive(IsVisible && HasBeenExplored);
        }

        // Päivitetään linna‑marker erillisellä metodilla
        UpdateCastleMarker();

        // Port-merkin käsittely:
        if (HasPort)
        {
            if (portMarkerInstance == null && portMarkerPrefab != null)
            {
                portMarkerInstance = Instantiate(portMarkerPrefab, transform.position, Quaternion.identity, transform);
                portMarkerInstance.transform.localPosition = Vector3.zero;
            }
            if (portMarkerInstance != null)
            {
                portMarkerInstance.SetActive(IsVisible && HasBeenExplored);
            }
        }
        else
        {
            if (portMarkerInstance != null)
            {
                Destroy(portMarkerInstance);
                portMarkerInstance = null;
            }
        }

        if (IsVisible)
        {
            // Päätellään, näytetäänkö strategiset resurssit tässä näkymässä.
            bool displayResources = true;
            if (GameManager.Instance.CurrentMapView == MapView.Diplomacy ||
                GameManager.Instance.CurrentMapView == MapView.Climatic ||
                GameManager.Instance.CurrentMapView == MapView.Religion ||
                GameManager.Instance.CurrentMapView == MapView.Technology ||
                GameManager.Instance.CurrentMapView == MapView.Alliance)
            {
                displayResources = GameManager.Instance.showStrategicResources;
            }

            if (HasIron)
            {
                if (GameManager.Instance.playerNation.HasIronCard)
                {
                    if (ironMarkerInstance != null)
                        ironMarkerInstance.SetActive(displayResources);
                    else
                        spriteRenderer.color = displayResources ? Color.gray : spriteRenderer.color;
                }
                else
                {
                    if (ironMarkerInstance != null)
                        ironMarkerInstance.SetActive(false);
                }
            }

            // Päivitetään ruudun värit nykyisen karttanäkymän mukaisesti.
            switch (GameManager.Instance.CurrentMapView)
            {
                case MapView.Alliance:
                    if (Terrain == TerrainType.Merivesi)
                        spriteRenderer.color = new Color(0.0f, 0.0f, 0.5f);
                    
                    else if (IsControlled && controllingNation != null)
                    {
                        // Tarkistetaan onko valtio liitossa DiplomacyManagerin avulla
                        Alliance alliance = DiplomacyManager.Instance.GetAllianceForNation(controllingNation);
                        // Jos liitto löytyy, käytetään liiton väriä, muuten harmaa
                        spriteRenderer.color = alliance != null ? alliance.AllianceColor : Color.gray;
                    }
                    else
                    {
                        // Jos ruutua ei hallita, asetetaan se harmaaksi
                        spriteRenderer.color = Color.gray;
                    }
                    break;

                case MapView.Religion:
                    if (Terrain == TerrainType.Merivesi)
                        spriteRenderer.color = new Color(0.0f, 0.0f, 0.5f);
                    else if (Terrain == TerrainType.MakeaVesi)
                        spriteRenderer.color = new Color(0.5f, 0.7f, 1.0f);
                    else
                        spriteRenderer.color = (Religion != null) ? Religion.Color : Color.gray;
                    break;
                case MapView.Climatic:
                    AssignClimaticColor();
                    break;
                case MapView.Technology:
                    if (IsControlled && controllingNation != null)
                    {
                        switch (controllingNation.Technology)
                        {
                            case 0:
                                spriteRenderer.color = Color.red;
                                break;
                            case 1:
                                // Oranssi: voidaan määritellä RGB-arvoilla, esim. 1, 0.5, 0
                                spriteRenderer.color = new Color(1f, 0.5f, 0f);
                                break;
                            case 2:
                                spriteRenderer.color = Color.green;
                                break;
                            default:
                                spriteRenderer.color = Color.gray;
                                break;
                        }
                    }
                    else
                    {
                        AssignTerrainEffects();
                    }
                    break;
                case MapView.Diplomacy:
                default:
                    if (IsControlled && controllingNation != null)
                        spriteRenderer.color = controllingNation.EmpireColor;
                    else
                        AssignTerrainEffects();
                    break;
            }

            if (fogOverlay != null)
                fogOverlay.enabled = false;
        }
        else
        {
            spriteRenderer.color = Color.black;
            if (fogOverlay != null)
                fogOverlay.enabled = true;
            if (ironMarkerInstance != null)
                ironMarkerInstance.SetActive(false);
            if (castleMarkerInstance != null)
                castleMarkerInstance.SetActive(false);
        }
    }

    // Metodi Climatic-näkymälle
    private void AssignClimaticColor()
    {
        switch (Terrain)
        {
            case TerrainType.Grassland:
                spriteRenderer.color = Color.green;
                break;
            case TerrainType.Desert:
                spriteRenderer.color = Color.yellow;
                break;
            case TerrainType.Forest:
                spriteRenderer.color = new Color(0.2f, 0.5f, 0.2f);
                break;
            case TerrainType.Mountain:
                spriteRenderer.color = Color.gray;
                break;
            case TerrainType.Tundra:
                spriteRenderer.color = new Color(0.5f, 0.0f, 0.5f);
                break;
            case TerrainType.Snow:
                spriteRenderer.color = Color.white;
                break;
            case TerrainType.Savannah:
                spriteRenderer.color = new Color(0.76f, 0.7f, 0.4f);
                break;
            case TerrainType.Merivesi:
                spriteRenderer.color = new Color(0.0f, 0.0f, 0.5f);
                break;
            case TerrainType.MakeaVesi:
                spriteRenderer.color = new Color(0.5f, 0.7f, 1.0f);
                break;
        }
    }
    //Tämä versio on diplomacy karttaa varten:
    public void AssignTerrainEffects()
    {
        if (spriteRenderer == null) return;
        if (!IsControlled)
        {
            // Aseta väriksi kunkin maastotyypin väri
            switch (Terrain)
            {
                case TerrainType.Grassland:
                    spriteRenderer.color = Color.green;
                    break;
                case TerrainType.Desert:
                    spriteRenderer.color = Color.yellow;
                    break;
                case TerrainType.Forest:
                    spriteRenderer.color = new Color(0.2f, 0.5f, 0.2f);
                    break;
                case TerrainType.Mountain:
                    spriteRenderer.color = Color.gray;
                    break;
                case TerrainType.Tundra:
                    spriteRenderer.color = new Color(0.5f, 0.0f, 0.5f);
                    break;
                case TerrainType.Snow:
                    spriteRenderer.color = new Color(1.0f, 1.0f, 1.0f);
                    break;
                case TerrainType.Savannah:
                    spriteRenderer.color = new Color(0.76f, 0.7f, 0.4f);
                    break;
                case TerrainType.Merivesi:
                    spriteRenderer.color = new Color(0.0f, 0.0f, 0.5f);
                    break;
                case TerrainType.MakeaVesi:
                    spriteRenderer.color = new Color(0.5f, 0.7f, 1.0f);
                    break;
            }
        }
        else
        {
            // Jos ruutu on hallittu, käytetään hallitsevan valtakunnan väriä
            if (controllingNation != null)
                spriteRenderer.color = controllingNation.EmpireColor;
            else
                spriteRenderer.color = Color.red;
        }
    }

    public void AssignResourceProduction()
    {
        switch (Terrain)
        {
            case TerrainType.Grassland:
                FoodProduction = 10;
                MineralProduction = 4;
                break;
            case TerrainType.Desert:
                FoodProduction = 1;
                MineralProduction = 1;
                break;
            case TerrainType.Forest:
                FoodProduction = 3;
                MineralProduction = 3;
                break;
            case TerrainType.Mountain:
                FoodProduction = 1;
                MineralProduction = 7;
                break;
            case TerrainType.Tundra:
                FoodProduction = 1;
                MineralProduction = 3;
                break;
            case TerrainType.Snow:
                FoodProduction = 1;
                MineralProduction = 1;
                break;
            case TerrainType.Savannah:
                FoodProduction = 4;
                MineralProduction = 2;
                break;
            case TerrainType.Merivesi:
                FoodProduction = 0;
                MineralProduction = 0;
                break;
            case TerrainType.MakeaVesi:
                FoodProduction = 2;
                MineralProduction = 0;
                break;
        }
    }

    //alkuperänen:
      public void SetControlled(bool controlled, Nation nation, bool revealAdjacents = true)
      {

          if (controlled && !IsControlled)
          {
              IsControlled = true;
              controllingNation = nation;

              if (HasIron && nation != GameManager.Instance.playerNation)
              {
                  nation.HasIronCard = true;
              }
              GameManager gameManager = FindObjectOfType<GameManager>();
              // Paljasta naapurit vain, jos kyseessä on pelaajan valtakunta
              if (revealAdjacents && nation == gameManager.playerNation)
              {
            //    Debug.Log("Kutsutaan RevealAdjacentTiles ruudulle: (" + this.X + ", " + this.Y + ")");
                SquareGrid grid = FindObjectOfType<SquareGrid>();
                  if (grid != null)
                  {
                      grid.RevealAdjacentTiles(this);
                  }
                  else
                  {
                      //Debug.LogError("SquareGrid not found!");
                  }
              }

              // Ensure the tile is visible and marked as explored if controlled by the player

              if (gameManager != null && nation == gameManager.playerNation)
              {
                  SetVisibility(true, true);
              }
          }
          else if (!controlled && IsControlled)
          {
              IsControlled = false;
              controllingNation = null;
          }
          else
          {
              IsControlled = controlled;
              controllingNation = controlled ? nation : null;
          }

          AssignTerrainEffects();
          SetVisibility(IsVisible, HasBeenExplored);

          // Update material properties
          var tileRenderer = GetComponent<SpriteRenderer>();
          if (tileRenderer != null)
          {
              MaterialPropertyBlock mpb = new MaterialPropertyBlock();
              tileRenderer.GetPropertyBlock(mpb);
              mpb.SetFloat("_IsControlled", IsControlled ? 1.0f : 0.0f);
              tileRenderer.SetPropertyBlock(mpb);
          }

          // Update borders for this tile and neighbors
          SquareGrid gridUpdate = FindObjectOfType<SquareGrid>();
          if (gridUpdate != null)
          {
              gridUpdate.UpdateBordersForTileAndNeighbors(this);
          }
      }
 
   

    // Merivesi nostaa viereisen ruudun FoodProductionia.
    public void IncreaseFoodProduction(int amount)
    {
        FoodProduction += amount;
    }

    public bool IsControllable
    {
        get
        {
            return Terrain != TerrainType.Merivesi;
        }
    }

    private bool hasCity = false;
    public bool HasCity
    {
        get => hasCity;
        set
        {
            hasCity = value;
            UpdateCityMarker();
        }
    }

    public void SetVisibility(bool visible, bool explored)
    {
        IsVisible = visible;
        if (visible)
        {
            if (!HasBeenExplored)
                HasBeenExplored = explored;
            // Kutsutaan päivitys TÄHÄN jälkeen
            UpdateVisuals();
            if (fogOverlay != null)
            {
                fogOverlay.enabled = false;
                fogOverlay.gameObject.SetActive(false);
            }
        }
        else
        {
            spriteRenderer.color = Color.black;
            if (fogOverlay != null)
            {
                fogOverlay.enabled = true;
                fogOverlay.gameObject.SetActive(true);
            }
        }
    }


    // Viite kaupungin merkkijoonelle (esim. musta piste)
    public GameObject cityMarkerPrefab;
    private GameObject cityMarkerInstance;

    // Metodi kaupungin merkin päivittämiseen
    private void UpdateCityMarker()
    {
        if (hasCity)
        {
            // Luodaan marker, jos sitä ei vielä ole
            if (cityMarkerInstance == null && cityMarkerPrefab != null)
            {
                cityMarkerInstance = Instantiate(cityMarkerPrefab, transform.position, Quaternion.identity, transform);
                cityMarkerInstance.transform.localPosition = Vector3.zero;
            }
            // Jos ruutu kuuluu tekoälylle ja sitä ei ole tutkittu, pidetään marker piilossa
            if (controllingNation != null && controllingNation != GameManager.Instance.playerNation && !HasBeenExplored)
            {
                cityMarkerInstance.SetActive(false);
            }
            else
            {
                // Muussa tapauksessa näytetään marker vain, jos ruutu on näkyvissä
                cityMarkerInstance.SetActive(IsVisible);
            }
        }
        else
        {
            if (cityMarkerInstance != null)
            {
                Destroy(cityMarkerInstance);
            }
        }
    }


    /// <summary>
    /// Soveltaa kertaluonteiset vaikutukset, kun laatta valloitetaan.
    /// bonusMultiplier määrittää väkiluvubonuksen suuruuden: 1.0 = täysi bonus, 0.5 = puolet bonusista.
    /// </summary>
    public void ApplyOneTimeEffects(Nation nation, float bonusMultiplier = 1.0f)
    {
        // Väkiluvun kasvu FoodProductionista kerrottuna bonusMultiplierilla
        float foodPopulationIncrease = FoodProduction * 10000f * bonusMultiplier;
        nation.AddPopulationGrowth(foodPopulationIncrease);
        //Debug.Log($"Laattavalloitus: +{foodPopulationIncrease} väkilukua FoodProductionista.");
    }

   
    //Shader juttuja:
    public int BorderMask { get; private set; } = 0;

    public void UpdateBorderMask(Dictionary<Nation, HashSet<SquareTile>> controlledTilesByNation)
    {
        // Nollataan maski ensin
        BorderMask = 0;

        // Haetaan ruudun reunat, esim. SquareGridin GetNeighbors()-metodilla
        List<SquareTile> neighbors = FindObjectOfType<SquareGrid>().GetNeighbors(this);
        foreach (SquareTile neighbor in neighbors)
        {
            // Jos naapuri ei kuulu samaan valtioon, merkitään kyseinen reuna rajaksi
            if (neighbor.controllingNation != this.controllingNation)
            {
                // Lasketaan erotus: 
                // tile.X vastaa pystysuuntaista sijaintia ja tile.Y vaakasuuntaista sijaintia.
                int dX = neighbor.X - this.X; // Pystysuuntainen erotus
                int dY = neighbor.Y - this.Y; // Vaakasuuntainen erotus

                if (dX == 1)
                    BorderMask |= 1;  // Yläreuna (naapuri ylempänä)
                if (dY == 1)
                    BorderMask |= 2;  // Oikea reuna (naapuri oikealla)
                if (dX == -1)
                    BorderMask |= 4;  // Alareuna (naapuri alempana)
                if (dY == -1)
                    BorderMask |= 8;  // Vasen reuna (naapuri vasemmalla)
            }
        }
    }


    public void MarkAsCapturedThisTurn()
    {
        capturedThisTurn = true;
        // visuaalisen merkin aktivointi. Ehkä turha atm
    }
   
    //alkuperäinen:
    void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return; // Hiiri on UI:n päällä, joten ohitetaan klikkaus

        //Debug.Log($"Tile clicked: ({X}, {Y}), Terrain: {Terrain}, IsControllable: {IsControllable}, " +
         //     $"IsControlled: {IsControlled}, IsVisible: {IsVisible}, HasBeenExplored: {HasBeenExplored}");
        // Jatka normaalisti, kun hiiri ei ole UI-elementin päällä:
        if (!IsVisible && !HasBeenExplored)
            return; 

        Nation playerNation = FindObjectOfType<GameManager>().playerNation;

        if (IsControlled && controllingNation != null && controllingNation != playerNation)
        {
            NationInfoPanel.Instance.ShowNationInfo(controllingNation, this);
        }
        else if (IsControlled && controllingNation != null && controllingNation == playerNation)
        {
            NationInfoPanel.Instance.ShowNationInfo(controllingNation, this);
        }
    }


    void OnMouseOver()
    {
       
        // Tarkistetaan, että ruutu on näkyvä ja sillä on valtio
        if (IsVisible && controllingNation != null)
        {
            // Jos hiiren oikeaa painiketta pidetään alhaalla, näytetään minimipaneeli
            if (Input.GetMouseButton(1))
            {
                MinimalNationInfoPanel.Instance.ShowMinimalInfo(controllingNation);
            }
        }
    }

    void OnMouseUp()
    {
        // Kun oikea painike vapautetaan, piilotetaan minimipaneeli
        if (Input.GetMouseButtonUp(1))
        {
            MinimalNationInfoPanel.Instance.Hide();
        }
    }

    public void TrySpawnIron()
    {
        // Raudan esiintyminen ei saa tapahtua veden ruuduissa
        if (Terrain == TerrainType.Merivesi || Terrain == TerrainType.MakeaVesi)
            return;

        // Perusmaassa 3% todennäköisyys, vuorissa 30%
        float chance = (Terrain == TerrainType.Mountain) ? 0.30f : 0.03f;

        if (Random.value < chance)
        {
            HasIron = true;
            //Debug.Log($"Rautaa ilmestyi ruutuun ({X}, {Y})");

            // Jos prefab on määritelty ja markeria ei vielä ole instanssoitu
            if (ironMarkerPrefab != null && ironMarkerInstance == null)
            {
                ironMarkerInstance = Instantiate(ironMarkerPrefab, transform.position, Quaternion.identity, transform);
                // Varmistetaan, että marker on pois päältä, jos ruutua ei vielä ole tutkittu tai se ei ole näkyvissä
                if (!IsVisible || !HasBeenExplored)
                    ironMarkerInstance.SetActive(false);
            }
        }
    }

    public float GetTileIncome()
    {
        switch (Terrain)
        {
            case TerrainType.Grassland: return 0.25f;
            case TerrainType.Forest: return 0.1f;
            case TerrainType.Tundra: return 0.03f; 
            case TerrainType.Snow: return 0.01f;
            case TerrainType.MakeaVesi: return 0.05f;
            case TerrainType.Desert: return 0.01f;
            case TerrainType.Mountain: return 0.05f;
            case TerrainType.Savannah: return 0.05f;
            default: return 0f;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(1))
        {
            MinimalNationInfoPanel.Instance.Hide();
        }
    }

    public bool HasCastle { get; set; } = false;

    public GameObject castleMarkerPrefab; 
    private GameObject castleMarkerInstance;

    public bool HasPort { get; set; } = false;
    public GameObject portMarkerPrefab;  

    // Sisäinen viite portin markeriin
    private GameObject portMarkerInstance;

    private void UpdateCastleMarker()
    {
        if (HasCastle)
        {
            // Jos markeria ei vielä ole, luodaan se
            if (castleMarkerInstance == null && castleMarkerPrefab != null)
            {
                castleMarkerInstance = Instantiate(castleMarkerPrefab, transform.position, Quaternion.identity, transform);
                castleMarkerInstance.transform.localPosition = Vector3.zero;
            }
            // Näytetään marker, jos ruutu on näkyvissä ja on tutkittu
            if (castleMarkerInstance != null)
                castleMarkerInstance.SetActive(IsVisible && HasBeenExplored);
        }
        else
        {
            // Jos linnaa ei ole, poistetaan marker jos se on olemassa
            if (castleMarkerInstance != null)
            {
                Destroy(castleMarkerInstance);
                castleMarkerInstance = null;
            }
        }
    }


    public GameObject conqueredCityMarkerPrefab; // Uusi prefab, asetetaan Inspectorissa

    // Päivitetään kaupunkimerkin ulkonäköä vallanvaihdon jälkeen
    public void SetConqueredCityAppearance()
    {
        if (cityMarkerInstance != null)
        {
            Destroy(cityMarkerInstance);
            cityMarkerInstance = null;
        }

        if (conqueredCityMarkerPrefab != null)
        {
            cityMarkerInstance = Instantiate(
                conqueredCityMarkerPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );
            cityMarkerInstance.transform.localPosition = Vector3.zero;
        }
    }

    public void ResetExploration()
    {
        // Nollataan ruudun tutkimustila:
        // Ruudun tutkimusmerkintä poistuu ja visuaaliset asetukset päivitetään.
        // Tämän seurauksena ruutu tulee mustaksi ja mahdolliset markerit piilotetuiksi.
        HasBeenExplored = false;
        SetVisibility(false, false);
    }

}




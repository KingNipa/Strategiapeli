using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  
using TMPro;         
using System.Linq;
using System.Collections;

public class StatisticsPanel : MonoBehaviour
{

    public Button statisticsButton;
    public Button populationCategoryButton;
    public Button territoryCategoryButton;
    public Button militaryCategoryButton;
    public Transform listContent;
    public GameObject listItemPrefab;
    public GameObject panel;

    // Enum, jolla m‰‰ritell‰‰n aktiivinen kategoria
    private enum StatCategory
    {
        Population,
        Territory,
        Military
    }
    private StatCategory currentCategory = StatCategory.Population;

    void Start()
    {
        // Piilotetaan paneeli alussa
        panel.SetActive(false);

        // Kytket‰‰n napit kuuntelijoihin
        statisticsButton.onClick.AddListener(TogglePanel);
        populationCategoryButton.onClick.AddListener(() =>
        {
            currentCategory = StatCategory.Population;
            UpdateList();
        });
        territoryCategoryButton.onClick.AddListener(() =>
        {
            currentCategory = StatCategory.Territory;
            UpdateList();
        });
        militaryCategoryButton.onClick.AddListener(() =>
        {
            currentCategory = StatCategory.Military;
            UpdateList();
        });
    }

    // N‰ytet‰‰n/piilotetaan tilastopaneeli
    void TogglePanel()
    {
        bool isActive = panel.activeSelf;
        panel.SetActive(!isActive);
        if (!isActive)
        {
            UpdateList();
        }
    }

    // P‰ivitt‰‰ listan sis‰llˆn
    void UpdateList()
    {
        // Tyhjennet‰‰n aiemmat rivit
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid == null) return;

        List<Nation> visibleNations = grid.GetAllTiles()
            .Where(t => t.IsVisible && t.controllingNation != null)
            .Select(t => t.controllingNation)
            .Distinct()
            .ToList();

        if (currentCategory == StatCategory.Population)
        {
            visibleNations = visibleNations.OrderByDescending(n => n.Population).ToList();
        }
        else if (currentCategory == StatCategory.Territory)
        {
            visibleNations = visibleNations.OrderByDescending(n =>
                grid.GetAllTiles().Count(t => t.IsVisible && t.controllingNation == n)
            ).ToList();
        }
        else if (currentCategory == StatCategory.Military)
        {
            visibleNations = visibleNations.OrderByDescending(n => n.MilitaryPower).ToList();
        }

        int rank = 1;
        foreach (Nation nation in visibleNations)
        {
            GameObject listItem = Instantiate(listItemPrefab, listContent);
            Text[] texts = listItem.GetComponentsInChildren<Text>();
            texts[0].text = rank + ".";
            texts[1].text = nation.Name;
            texts[2].text = currentCategory switch
            {
                StatCategory.Population => nation.Population.ToString("F0"),
                StatCategory.Territory => grid.GetAllTiles().Count(t => t.IsVisible && t.controllingNation == nation).ToString(),
                StatCategory.Military => nation.MilitaryPower.ToString("F0"),
                _ => ""
            };

            // Button & background
            Button btn = listItem.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnNationSelected(nation));

            Image bg = listItem.GetComponent<Image>();
            bg.color = (rank % 2 == 0)
                ? new Color(1f, 1f, 1f, 0.05f)
                : new Color(1f, 1f, 1f, 0f);

            rank++;
        }


        void OnNationSelected(Nation nation)
        {
            panel.SetActive(false);
            ZoomToNation(nation);

            // Haetaan ensimm‰inen n‰kyv‰ ruutu, jonka valtio on kyseinen nation
            SquareGrid grid = FindObjectOfType<SquareGrid>();
            SquareTile nationTile = grid.GetAllTiles()
                                        .FirstOrDefault(t => t.controllingNation == nation && t.IsVisible);

            // Avataan valtion infopaneeli
            NationInfoPanel.Instance.ShowNationInfo(nation, nationTile);
            // K‰ynnistet‰‰n hohto coroutinen GameManagerista
            GameManager.Instance.StartCoroutine(HighlightNationCoroutine(nation, 4f));

        }
    }

    private IEnumerator HighlightNationCoroutine(Nation nation, float duration)
    {
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid == null)
            yield break;

        // Haetaan kaikki ruudut, joita kyseinen valtio hallitsee ja jotka ovat n‰kyviss‰
        List<SquareTile> nationTiles = grid.GetAllTiles()
            .Where(t => t.controllingNation == nation && t.IsVisible)
            .ToList();

        // Tallennetaan ruutujen alkuper‰iset v‰rit
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
                    // K‰ytet‰‰n vahvempaa korostusv‰ri‰: blendataan alkuper‰inen v‰ri 80 % valkoiseksi
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

        // Palautetaan alkuper‰iset v‰rit
        foreach (SquareTile tile in nationTiles)
        {
            tile.spriteRenderer.color = originalColors[tile];
        }
    }

    // Zoomaa pelimaailmassa sen kansan alueen kohdalle
    void ZoomToNation(Nation nation)
    {
        SquareGrid grid = FindObjectOfType<SquareGrid>();
        if (grid == null) return;

        // Hakee kaikki ruudut, jotka kansa hallitsee
        List<SquareTile> nationTiles = grid.GetAllTiles().Where(t => t.controllingNation == nation).ToList();
        if (nationTiles.Count == 0)
        {
            //Debug.Log("Kansalla ei ole hallittuja ruutuja zoomausta varten.");
            return;
        }

        // Lasketaan hallittujen ruutujen keskipiste
        Vector3 sum = Vector3.zero;
        foreach (SquareTile tile in nationTiles)
        {
            sum += tile.transform.position;
        }
        Vector3 center = sum / nationTiles.Count;

        // Siirret‰‰n p‰‰kamera keskipisteen kohdalle. T‰ss‰ esimerkiss‰ oletetaan,
        // ett‰ k‰ytet‰‰n ortografista kameraa ja ett‰ z-koordinaali s‰ilyy ennallaan.
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(center.x, center.y, mainCam.transform.position.z);
            // Voi myˆs s‰‰t‰‰ ortografista kokoa, mik‰li haluu tehd‰ zoomaamisesta tarkemman:
            // mainCam.orthographicSize = 10; // Esimerkiksi
        }
        else
        {
            //Debug.LogWarning("P‰‰kameraa ei lˆytynyt.");
        }
    }
}
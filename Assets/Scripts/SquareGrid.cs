using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SquareGrid : MonoBehaviour
{
    [Header("Tile Asetukset")]
    public GameObject tilePrefab;      // SquareTile Prefab
    public int height = 100;            // Kartan leveys neliˆin‰
    public int width = 100;             // Kartan korkeus neliˆin‰
    public float tileSpacing = 1.0f;   // Et‰isyys neliˆiden v‰lill‰

    [Header("Kartageneroinnin asetukset")]
    [Tooltip("Satunnainen seed kartan generointiin. Sama seed tuottaa saman kartan.")]
    public int seed = 0; 

    [Header("Vuorinoise Parametrit")]
    public float mountainScale = 30f;
    public float mountainThreshold = 0.72f;

    [Header("Perlin Noise Parametrit")]
    public float landNoiseScale = 20f;
    public float seaThreshold = 0.4f;  // Jos Perlin-arvo alle t‰m‰n => meri, muuten maa

    [Header("J‰rvi Parametrit")]
    [SerializeField] private float lakeScale = 15f;       // Mit‰ suurempi, sit‰ harvemmin j‰rvi‰
    [SerializeField] private float lakeThreshold = 0.8f;  // Jos Perlin-arvo ylitt‰‰ t‰m‰n, ruudusta tulee MakeaVesi

    private Dictionary<Nation, HashSet<SquareTile>> controlledTilesByNation = new Dictionary<Nation, HashSet<SquareTile>>();
    private Dictionary<Nation, HashSet<SquareTile>> edgeTilesByNation = new Dictionary<Nation, HashSet<SquareTile>>();



    private List<SquareTile> tiles = new List<SquareTile>();
    private HashSet<SquareTile> controlledTiles = new HashSet<SquareTile>();
    private HashSet<SquareTile> edgeTiles = new HashSet<SquareTile>();

    public Nation nation;

    // Mahdolliset alustusterrainit pelaajan aloitusalueelle
    private readonly List<SquareTile.TerrainType> allowedStartingTerrains = new List<SquareTile.TerrainType>
    {
        SquareTile.TerrainType.Grassland,
        SquareTile.TerrainType.Forest,
        SquareTile.TerrainType.Savannah
    };


    public int initialRevealRadius = 5; // 5

    void Start()
    {
        // Generoi ruudukko
      //  GenerateSquareGrid();

        // Aseta kaikki ruudut aluksi n‰kym‰ttˆmiksi
        foreach (var tile in tiles)
        {
            tile.SetVisibility(false, false);
        }

        // Poistetaan starting-alueen ja fog-of-war -alustuksen kutsut t‰st‰ Start-metodista.
        // N‰m‰ kutsutaan myˆhemmin GameManagerin OnSceneLoaded:ssa,
        // jolloin playerNation on varmasti asetettu.
    }



   public void InitializeFogOfWar()
    {
        // Paljasta vain pelaajan hallitsemat ruudut
        if (controlledTilesByNation.ContainsKey(nation))
        {
            foreach (var controlledTile in controlledTilesByNation[nation])
            {
                RevealTileAndSurroundings(controlledTile, initialRevealRadius);
            }
        }
    }

    // New method to reveal a specific tile and its surroundings within a given radius
    public void RevealTileAndSurroundings(SquareTile centerTile, int radius)
    {
        foreach (var tile in tiles)
        {
            // Calculate Manhattan distance
            int distance = Mathf.Abs(tile.X - centerTile.X) + Mathf.Abs(tile.Y - centerTile.Y);
            if (distance <= radius)
            {
                tile.SetVisibility(true, true);
            }
        }
    }

    // New method to reveal adjacent tiles when a new tile is conquered
    public void RevealAdjacentTiles(SquareTile newlyControlledTile)
    {
     //   Debug.Log("RevealAdjacentTiles k‰ynnistyi ruudulle: (" + newlyControlledTile.X + ", " + newlyControlledTile.Y + ")");
        // Optionally, define a smaller radius for adjacent revelation
        int revealRadius = 5; // 10

        RevealTileAndSurroundings(newlyControlledTile, revealRadius);
    }

    public List<SquareTile> GetDiscoveredTiles()
    {
        return tiles.Where(t => t.IsVisible || t.HasBeenExplored).ToList();
    }

    public List<SquareTile> GetControlledTiles()
    {
        return tiles.Where(t => t.IsControlled).ToList();
    }
   
    private bool gridGenerated = false;
    public void GenerateSquareGrid()
    {
        if (gridGenerated) return;
        gridGenerated = true;
      //  Debug.Log("GenerateSquareGrid alkaa.");
        tiles.Clear();

        Random.InitState(seed);

        // 1) Luo ruudukko & p‰‰t‰ meri/maa
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Luodaan tile-objekti
                Vector3 tilePosition = new Vector3(y * tileSpacing, x * tileSpacing, 0);
                GameObject tileGO = Instantiate(tilePrefab, tilePosition, Quaternion.identity, transform);
                SquareTile tile = tileGO.GetComponent<SquareTile>();

                // Aseta X ja Y
                tile.X = x;
                tile.Y = y;

                // Reunoille pakotettu meri (valinnaisesti esim. 2 ruudun leveydelt‰):
                if (x < 2 || x >= width - 2 || y < 2 || y >= height - 2)
                {
                    tile.Terrain = SquareTile.TerrainType.Merivesi;
                }
                else
                {
                    int octaves = 4;             
                    float persistence = 0.5f;      // Typillinen arvo, joka pienent‰‰ amplitudia joka oktaavilla
                    float lacunarity = 2.0f;       // Taajuuden kertaantumisnopeus
                    float perlinValue = FBm((x + seed) / landNoiseScale, (y + seed) / landNoiseScale, octaves, persistence, lacunarity);
                    if (perlinValue < seaThreshold)
                        tile.Terrain = SquareTile.TerrainType.Merivesi;
                    else
                        tile.Terrain = SquareTile.TerrainType.Grassland;  // Alustavasti Grassland
                }

                tiles.Add(tile);
                ////Debug.Log($"Tile generated at ({x}, {y}) with Terrain: {tile.Terrain} and IsControllable: {tile.IsControllable}");
            }
        
        }

        // 2) Aseta ilmastovyˆhykkeet latitude-bandeilla (vain maalle)
        for (int x = 2; x < width - 2; x++)
        {
            for (int y = 2; y < height - 2; y++)
            {
                SquareTile tile = tiles[x * height + y];  // Korjattu indeksointi
                if (tile.Terrain == SquareTile.TerrainType.Merivesi) continue;

                float latitude = (float)x / (width - 1);

                // Lis‰‰ Perlin-noise-pohjainen modifikaattori latitude-arvoon
                int reducedSeed = seed % 10000; // rajoitetaan seed tiettyyn arvoon
                float noise = Mathf.PerlinNoise((x + reducedSeed) / 10f, (y + reducedSeed) / 10f);
                float modifiedLatitude = latitude + (noise - 0.5f) * 0.25f; // S‰‰t‰‰ vaikutuksen voimakkuutta 0.1

                // Hyvin simppeli malli: pohjoiseen ja etel‰‰n lunta, keskelle ruohoa
                if (modifiedLatitude > 0.97f)
                {
                    tile.Terrain = SquareTile.TerrainType.Snow;
                }
                else if (modifiedLatitude > 0.92f)
                {
                    tile.Terrain = SquareTile.TerrainType.Tundra;
                }
                else if (modifiedLatitude > 0.80f)
                {
                    tile.Terrain = SquareTile.TerrainType.Forest;
                }
                else if (modifiedLatitude > 0.45f)
                {
                    tile.Terrain = SquareTile.TerrainType.Grassland;
                }
                else if (modifiedLatitude > 0.30f)
                {
                    tile.Terrain = SquareTile.TerrainType.Desert;
                }
                else if (modifiedLatitude > 0.22f)
                {
                    tile.Terrain = SquareTile.TerrainType.Forest;
                }
                else if (modifiedLatitude > 0.03f)
                {
                    tile.Terrain = SquareTile.TerrainType.Savannah;
                }
                else
                {
                    tile.Terrain = SquareTile.TerrainType.Tundra;  // etel‰n "napa-alue"
                }
            }
        }


        // 3) Vuorten generointi toisella Perlin-noisella
        for (int x = 2; x < width - 2; x++)
        {
            for (int y = 2; y < height - 2; y++)
            {
                SquareTile tile = tiles[x * height + y];  // Korjattu indeksointi
                if (tile.Terrain == SquareTile.TerrainType.Merivesi) continue;

                float mValue = Mathf.PerlinNoise((x + seed) / mountainScale, (y + seed) / mountainScale);
                if (mValue > mountainThreshold)
                {
                    tile.Terrain = SquareTile.TerrainType.Mountain;
                }
            }
        }

        // J‰rvet
        GenerateFreshwaterLakes();

        // 4) Lopuksi aseta v‰rit ja resurssit
        foreach (var tile in tiles)
        {
            tile.AssignTerrainEffects();
            tile.AssignResourceProduction();
            tile.TrySpawnIron();
        }

        // 5) Valinnainen: merivesi boostaa viereisten laattojen ruoan tuotantoa
        ApplySeaBoosts();
     //   Debug.Log("GenerateSquareGrid p‰‰ttynyt.");
    }

    // Merivesi nostaa viereisten ruutujen FoodProductionia
    void ApplySeaBoosts()
    {
        foreach (var seaTile in tiles.Where(t => t.Terrain == SquareTile.TerrainType.Merivesi))
        {
            foreach (var neighbor in GetNeighbors(seaTile))
            {
                if (neighbor.Terrain != SquareTile.TerrainType.Merivesi)
                {
                    neighbor.IncreaseFoodProduction(1);
                }
            }
        }
    }

    void GenerateFreshwaterLakes()
    {
        for (int x = 2; x < width - 2; x++)
        {
            for (int y = 2; y < height - 2; y++)
            {
                // K‰yt‰ Perlin-noisea j‰rvien sijainnin m‰‰ritt‰miseen
                float noiseValue = Mathf.PerlinNoise((x + seed) / lakeScale, (y + seed) / lakeScale);

                if (noiseValue > lakeThreshold)
                {
                    SquareTile tile = tiles[x * height + y];
                    if (tile.Terrain != SquareTile.TerrainType.Merivesi && !HasAdjacentSea(tile))
                    {
                        tile.Terrain = SquareTile.TerrainType.MakeaVesi;
                    }
                }
            }
        }

        // Smoothing-vaihe j‰rvien luonnollisuuden lis‰‰miseksi
        SmoothLakes();
    }

    void SmoothLakes()
    {
        List<SquareTile> lakeTiles = new List<SquareTile>(tiles.Where(t => t.Terrain == SquareTile.TerrainType.MakeaVesi));

        foreach (var tile in lakeTiles)
        {
            foreach (var neighbor in GetNeighbors(tile))
            {
                if (neighbor.Terrain != SquareTile.TerrainType.MakeaVesi && !HasAdjacentSea(neighbor))
                {
                    if (Random.value > 0.7f) // esim. 30% todenn‰kˆisyys
                    {
                        neighbor.Terrain = SquareTile.TerrainType.MakeaVesi;
                    }
                }
            }
        }
    }
    // Joka kerta kun uusi ruutu hallitaan, p‰ivitet‰‰n reuna-alueet
    void UpdateEdgeTiles(SquareTile newControlledTile)
    {
        foreach (var neighbor in GetNeighbors(newControlledTile))
        {
            if (!neighbor.IsControlled)
            {
                edgeTiles.Add(neighbor);
            }
        }
    }
    /*
    // Laajentaa satunnaisesti kontrolloitua aluetta reunoilta
    //vanha
    public SquareTile ExpandTerritoryForNation(Nation nation)
    {
        if (!edgeTilesByNation.ContainsKey(nation) || edgeTilesByNation[nation].Count == 0)
        {
            //Debug.Log("No edge tiles available for expansion for this nation.");
            return null;
        }

        var validEdgeTiles = edgeTilesByNation[nation]
               .Where(tile => tile.Terrain != SquareTile.TerrainType.Merivesi)
               .ToList();

        if (validEdgeTiles.Count == 0)
        {
            //Debug.Log("No valid edge tiles available (sea tiles are not allowed for expansion).");
            return null;
        }
        // Jos kaikkien validien ruutujen FoodProduction on 0 (esim. vain hiekkaruutuja)
        if (validEdgeTiles.Sum(t => t.FoodProduction) == 0)
        {
            // Ker‰t‰‰n kaikki viel‰ valloittamattomat ruudut
            List<SquareTile> candidates = validEdgeTiles.Where(tile => !tile.IsControlled).ToList();
            if (candidates.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                SquareTile randomTile = candidates[randomIndex];
                bool isPlayer = (nation == FindObjectOfType<GameManager>().playerNation);
                randomTile.SetControlled(true, nation, isPlayer);
                controlledTilesByNation[nation].Add(randomTile);
                edgeTilesByNation[nation].Remove(randomTile);
                UpdateEdgeTilesForNation(randomTile, nation);
                return randomTile;
            }
            else
            {
                //Debug.Log("Kaikki validit edge-ruudut ovat jo vallattuja.");
                return null;
            }
        }


        float totalFoodProduction = validEdgeTiles.Sum(t => t.FoodProduction);

        if (totalFoodProduction == 0)
        {
            SquareTile randomTile = validEdgeTiles[Random.Range(0, validEdgeTiles.Count)];
            if (randomTile != null && !randomTile.IsControlled)
            {
                bool isPlayer = (nation == FindObjectOfType<GameManager>().playerNation);
                randomTile.SetControlled(true, nation, isPlayer);
                controlledTilesByNation[nation].Add(randomTile);
                edgeTilesByNation[nation].Remove(randomTile);
                UpdateEdgeTilesForNation(randomTile, nation);
                return randomTile;
            }
            return null;
        }

        float randomValue = Random.Range(0, totalFoodProduction);
        float cumulative = 0f;
        foreach (var tile in validEdgeTiles)
        {
            cumulative += tile.FoodProduction;
            if (randomValue <= cumulative)
            {
                if (!tile.IsControlled)
                {
                    bool isPlayer = (nation == FindObjectOfType<GameManager>().playerNation);
                    tile.SetControlled(true, nation, isPlayer);
                    tile.ApplyOneTimeEffects(nation);
                    controlledTilesByNation[nation].Add(tile);
                    edgeTilesByNation[nation].Remove(tile);
                    UpdateEdgeTilesForNation(tile, nation);
                    return tile;
                }
            }
        }
        return null;
    }
    */


    public List<SquareTile> GetAllTiles()
    {
        return tiles;
    }


    // Palauttaa ruudun naapurit (vain vasen, oikea, ylˆs ja alas)
    public List<SquareTile> GetNeighbors(SquareTile tile)
    {
        List<SquareTile> neighbors = new List<SquareTile>();

        // M‰‰ritell‰‰n vain nelj‰ suoraan vierekk‰ist‰ suuntaa
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int neighborX = tile.X + dx[i];
            int neighborY = tile.Y + dy[i];

            if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
            {
                SquareTile neighbor = tiles[neighborX * height + neighborY];
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public void UpdateMapView()
    {
        //Debug.Log("SquareGrid.UpdateMapView() k‰ynnistyy.");
        foreach (var tile in tiles)
        {
            tile.UpdateVisuals();
        }
    }

    private void UpdateEdgeTilesForNation(SquareTile controlledTile, Nation nation)
    {
        // Varmista, ett‰ dictionaryiss‰ on entry kyseiselle valtakunnalle
        if (!edgeTilesByNation.ContainsKey(nation))
        {
            edgeTilesByNation[nation] = new HashSet<SquareTile>();
        }
        foreach (SquareTile neighbor in GetNeighbors(controlledTile))
        {
            if (!neighbor.IsControlled)
            {
                edgeTilesByNation[nation].Add(neighbor);
            }
            // P‰ivitet‰‰n naapurin border mask, koska tilanne on muuttunut
            neighbor.UpdateBorderMask(controlledTilesByNation);
            // P‰ivitet‰‰n naapurin materiaalin property _TileMask
            var neighborRenderer = neighbor.GetComponent<SpriteRenderer>();
            if (neighborRenderer != null)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                neighborRenderer.GetPropertyBlock(mpb);
                mpb.SetFloat("_TileMask", neighbor.BorderMask);
                neighborRenderer.SetPropertyBlock(mpb);
            }
        }
        // Myˆs itse hallitun ruudun maski kannattaa p‰ivitt‰‰
        controlledTile.UpdateBorderMask(controlledTilesByNation);
        // P‰ivitet‰‰n hallitun ruudun materiaalin property _TileMask
        var tileRenderer = controlledTile.GetComponent<SpriteRenderer>();
        if (tileRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            tileRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_TileMask", controlledTile.BorderMask);
            tileRenderer.SetPropertyBlock(mpb);
        }
    }


    public void InitializeStartingAreaForNation(Nation nation, bool reveal = true)
    {
        List<SquareTile.TerrainType> allowedStartingTerrains = new List<SquareTile.TerrainType>
    {
        SquareTile.TerrainType.Grassland,
        SquareTile.TerrainType.Forest,
        SquareTile.TerrainType.Savannah,
        SquareTile.TerrainType.Desert,
        SquareTile.TerrainType.Mountain,
        SquareTile.TerrainType.Tundra,
        SquareTile.TerrainType.Snow
    };

        List<SquareTile> validTiles = tiles
            .Where(tile => allowedStartingTerrains.Contains(tile.Terrain) && tile.IsControllable && !tile.IsControlled)
            .ToList();

      //  Debug.Log("Valid starting tiles count (All): " + validTiles.Count);

        if (validTiles.Count == 0)
        {
            // Mahdollisesti voi lis‰t‰ t‰h‰n myˆs virheilmoituksen tai fallback-logiikkaa
            return;
        }

        SquareTile startingTile = validTiles[Random.Range(0, validTiles.Count)];
        startingTile.SetControlled(true, nation, reveal);

        if (!nation.isCivilWarNation)
        {
            // K‰ytet‰‰n uutta metodia alustusarvojen asettamiseen
            switch (startingTile.Terrain)
            {
                case SquareTile.TerrainType.Grassland:
                    nation.InitializeStartingValues(325000f, 2000);
                    break;
                case SquareTile.TerrainType.Desert:
                    nation.InitializeStartingValues(50000f, 250);
                    break;
                case SquareTile.TerrainType.Forest:
                    nation.InitializeStartingValues(150000f, 1000);
                    break;
                case SquareTile.TerrainType.Mountain:
                    nation.InitializeStartingValues(70000f, 500);
                    break;
                case SquareTile.TerrainType.Tundra:
                    nation.InitializeStartingValues(50000f, 250);
                    break;
                case SquareTile.TerrainType.Snow:
                    nation.InitializeStartingValues(60000f, 250);
                    break;
                case SquareTile.TerrainType.Savannah:
                    nation.InitializeStartingValues(120000f, 750);
                    break;
                default:
                    nation.InitializeStartingValues(100000f, 3000);
                    break;
            }
        }

        // Lis‰t‰‰n ruutu kansakunnan hallittujen ruutujen listaan
        if (!controlledTilesByNation.ContainsKey(nation))
        {
            controlledTilesByNation[nation] = new HashSet<SquareTile>();
            edgeTilesByNation[nation] = new HashSet<SquareTile>();
        }
        controlledTilesByNation[nation].Add(startingTile);
        UpdateEdgeTilesForNation(startingTile, nation);

        if (reveal)
        {
            startingTile.SetVisibility(true, true);
            
        }
    }

    public SquareTile GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return tiles[x * height + y];
    }


    // Metodi, joka p‰ivitt‰‰ sek‰ annetun ruudun ett‰ sen naapureiden rajamaskit!
    public void UpdateBordersForTileAndNeighbors(SquareTile tile)
    {
        // P‰ivitet‰‰n itse ruutu
        tile.UpdateBorderMask(controlledTilesByNation);
        UpdateTileMPB(tile);

        // P‰ivitet‰‰n ruudun naapurit
        List<SquareTile> neighbors = GetNeighbors(tile);
        foreach (SquareTile neighbor in neighbors)
        {
            neighbor.UpdateBorderMask(controlledTilesByNation);
            UpdateTileMPB(neighbor);
        }
    }

    // Apumetodi materiaalin propertyjen p‰ivitt‰miseen
    private void UpdateTileMPB(SquareTile tile)
    {
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_TileMask", tile.BorderMask);
            renderer.SetPropertyBlock(mpb);
        }
    }

  
    public void TransferTileOwnership(SquareTile tile, Nation newNation)
    {
        // Tallenna vanhan valtakunnan tieto ja tieto siit‰, oliko ruudussa kaupunki.
        Nation oldNation = tile.controllingNation;
        bool hadCity = tile.HasCity;

        // Jos ruudussa on kaupunki, poista bonus vanhasta valtakunnasta.
        if (tile.HasCity && oldNation != null)
        {
            oldNation.IncomeBonusPerTurn -= 5;
        }

        // Poista ruutu vanhan omistajan hallitsemien ruutujen listalta.
        if (oldNation != null && controlledTilesByNation.ContainsKey(oldNation))
        {
            controlledTilesByNation[oldNation].Remove(tile);
        }

        // Poista vanha kaupunkimerkint‰, mik‰li sellainen on.
        if (tile.HasCity)
        {
            tile.HasCity = false;
        }

        // P‰ivitet‰‰n ruudun tila kutsumalla SetControlled-metodia.
        // T‰m‰ metodi huolehtii ruudun IsControlled-, IsVisible- ja HasBeenExplored-asetuksista.
        tile.SetControlled(true, newNation, false);

        // Lis‰‰ ruutu uuden omistajan hallitsemien ruutujen listaan.
        if (!controlledTilesByNation.ContainsKey(newNation))
        {
            controlledTilesByNation[newNation] = new HashSet<SquareTile>();
        }
        controlledTilesByNation[newNation].Add(tile);

        // P‰ivitet‰‰n uuden omistajan reuna-alueet.
        UpdateEdgeTilesForNation(tile, newNation);

        // P‰ivitet‰‰n kaupunkispriten ulkon‰kˆ.
        if (hadCity)
        {
            // Jos uusi valtio ei ole pelaajan valtakunta, p‰ivitet‰‰n kaupunkisprite vain,
            // jos ruutu on jo tutkittu.
            if (newNation != GameManager.Instance.playerNation)
            {
                if (tile.HasBeenExplored)
                {
                    tile.SetConqueredCityAppearance();
                }
                // Muussa tapauksessa (jos ruutu on viel‰ tutkimaton)
                // ei p‰ivitet‰ kaupunkisprite‰ ñ se pysyy piilotettuna fogin takana.
            }
            else
            {
                // Jos pelaaja itse vallaa ruudun, n‰ytet‰‰n kaupunkisprite normaalisti.
                tile.SetConqueredCityAppearance();
            }
        }
    }




    private bool HasAdjacentSea(SquareTile tile)
    {
        foreach (SquareTile neighbor in GetNeighbors(tile))
        {
            if (neighbor.Terrain == SquareTile.TerrainType.Merivesi)
                return true;
        }
        return false;
    }


    /// <summary>
    /// T‰t‰ k‰ytet‰‰n uskonnon levitt‰misess‰:
    /// Palauttaa kaikki 8 ruutua, jotka ymp‰rˆiv‰t annettua ruutua (diagonaaliset mukaan lukien).
    /// </summary>
    public List<SquareTile> GetNeighbors8(SquareTile tile)
    {
        List<SquareTile> neighbors = new List<SquareTile>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // Ohitetaan keskell‰ oleva ruutu
                if (dx == 0 && dy == 0)
                    continue;
                int newX = tile.X + dx;
                int newY = tile.Y + dy;
                if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                {
                    neighbors.Add(GetTile(newX, newY));
                }
            }
        }
        return neighbors;
    }

    /// <summary>
    /// T‰t‰ k‰ytet‰‰n uskonnon levitt‰misess‰: 
    /// Palauttaa kaikki ruudut, jotka ovat annettua ruutua ymp‰rˆiv‰ll‰ alueella, jonka et‰isyys on enint‰‰n radius
    /// (k‰ytet‰‰n Manhattan-et‰isyytt‰ t‰ss‰ esimerkiss‰).
    /// </summary>
    public List<SquareTile> GetTilesInRadius(SquareTile centerTile, int radius)
    {
        List<SquareTile> result = new List<SquareTile>();
        foreach (SquareTile tile in GetAllTiles())
        {
            int distance = Mathf.Abs(tile.X - centerTile.X) + Mathf.Abs(tile.Y - centerTile.Y);
            if (distance <= radius)
            {
                result.Add(tile);
            }
        }
        return result;
    }

    public List<SquareTile> GetDynamicEdgeTiles(Nation nation)
    {
        // Hae kaikki pelaajan hallitsemat ruudut
        var controlled = GetControlledTiles().Where(t => t.controllingNation == nation);
        // Ker‰‰ niiden naapurit, jotka eiv‰t ole viel‰ hallittuja ja eiv‰t ole meriruutuja
        var edgeTiles = controlled
            .SelectMany(tile => GetNeighbors(tile))
            .Where(neighbor => !neighbor.IsControlled && neighbor.Terrain != SquareTile.TerrainType.Merivesi)
            .Distinct()
            .ToList();
        return edgeTiles;
    }

    //uus:
    public SquareTile ExpandTerritoryForNation(Nation nation)
    {
        // K‰ytet‰‰n dynaamista metodia reuna-ruutujen hakemiseen
        List<SquareTile> validEdgeTiles = GetDynamicEdgeTiles(nation);

        if (validEdgeTiles.Count == 0)
        {
            //Debug.Log("Ei en‰‰ laajennettavia reuna-alueita!");
            return null;
        }

        // Jos kaikissa edge-ruuduissa FoodProduction on 0, valitaan satunnainen ruutu
        if (validEdgeTiles.Sum(t => t.FoodProduction) == 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validEdgeTiles.Count);
            SquareTile randomTile = validEdgeTiles[randomIndex];
            if (!randomTile.IsControlled)
            {
                bool isPlayer = (nation == FindObjectOfType<GameManager>().playerNation);
                randomTile.SetControlled(true, nation, isPlayer);
                return randomTile;
            }
            return null;
        }

        // Valitaan ruutu painotetusti FoodProductionin mukaan
        float totalFoodProduction = validEdgeTiles.Sum(t => t.FoodProduction);
        float randomValue = UnityEngine.Random.Range(0, totalFoodProduction);
        float cumulative = 0f;
        foreach (var tile in validEdgeTiles)
        {
            cumulative += tile.FoodProduction;
            if (randomValue <= cumulative)
            {
                if (!tile.IsControlled)
                {
                    bool isPlayer = (nation == FindObjectOfType<GameManager>().playerNation);
                    tile.SetControlled(true, nation, isPlayer);
                    tile.ApplyOneTimeEffects(nation);
                    return tile;
                }
            }
        }
        return null;
    }

    // Palauttaa normalisoidun fBM-arvon pisteelle (x, y)
    public float FBm(float x, float y, int octaves, float persistence, float lacunarity)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f; // Normalisoinnin apu

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;   // Amplitudin pienentyminen
            frequency *= lacunarity;    // Taajuuden kasvu
        }

        return total / maxValue;
    }
    public void InitializeStartingAreaForNationGrassland(Nation nation, bool reveal = true)
    {
        // Suodatetaan vain ruudut, joissa maastotyyppi on Grassland,
        // ovat hallittavissa ja eiv‰t ole viel‰ hallittuja
        List<SquareTile> validTiles = tiles
        .Where(tile => tile.Terrain == SquareTile.TerrainType.Grassland && tile.IsControllable && !tile.IsControlled)
        .ToList();
       // Debug.Log("Valid starting tiles count (Grassland): " + validTiles.Count);

        if (validTiles.Count == 0)
        {
            InitializeStartingAreaForNation(nation, reveal);
            return;
        }

        SquareTile startingTile = validTiles[Random.Range(0, validTiles.Count)];
        startingTile.SetControlled(true, nation, reveal);

        // Aseta alkuarvot grassland-laatan mukaisesti
        nation.InitializeStartingValues(325000f, 2000);

        if (!controlledTilesByNation.ContainsKey(nation))
        {
            controlledTilesByNation[nation] = new HashSet<SquareTile>();
            edgeTilesByNation[nation] = new HashSet<SquareTile>();
        }
        controlledTilesByNation[nation].Add(startingTile);
        UpdateEdgeTilesForNation(startingTile, nation);

        if (reveal)
        {
            startingTile.SetVisibility(true, true);
            
        }
      //  Debug.Log("Aloitusruutu valittu: (" + startingTile.X + ", " + startingTile.Y + "), controllingNation: " + (startingTile.controllingNation != null ? startingTile.controllingNation.Name : "null"));

    }




}

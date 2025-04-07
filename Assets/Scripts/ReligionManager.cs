using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public static class ReligionManager
{
    /// <summary>
    /// Levittää pelaajan kontrolloimalta alueelta valitun uskonnon naapureihin.
    /// Levitys tapahtuu vain ruutuihin, jotka:
    ///   - Ovat vierekkäin (naapureita) jossain ruudussa, jossa uskonto on jo olemassa.
    ///   - Joissakin ruuduissa uskontoa ei ole vielä.
    ///   - Eivät ole vesialueita (Merivesi tai MakeaVesi).
    /// Levityksen määrä määritellään parametrilla maxTilesToSpread.
    /// </summary>
    public static void SpreadReligionDigging(SquareGrid grid, Nation nation, int maxTilesToSpread, int spreadRadius = 1)
    {
        if (nation.Religion == null)
        {
            //Debug.LogWarning("SpreadReligionDigging: Nationilla ei ole uskontoa.");
            return;
        }

        // Aloituspisteenä otetaan kaikki ruudut, joissa uskonto on jo läsnä.
        List<SquareTile> seeds = grid.GetAllTiles()
            .Where(tile => tile.Religion == nation.Religion)
            .ToList();

        if (seeds.Count == 0)
        {
            //Debug.LogWarning("SpreadReligionDigging: Ei löydy ruutuja, joissa uskonto olisi jo.");
            return;
        }

        Queue<SquareTile> queue = new Queue<SquareTile>();
        HashSet<SquareTile> visited = new HashSet<SquareTile>();

        foreach (SquareTile tile in seeds)
        {
            queue.Enqueue(tile);
            visited.Add(tile);
        }

        int tilesUpdated = 0;

        // Käydään läpi ruutuja niin kauan, kunnes on päivitetty maxTilesToSpread uutta ruutua tai ruutuja ei enää löydy.
        while (queue.Count > 0 && tilesUpdated < maxTilesToSpread)
        {
            SquareTile current = queue.Dequeue();
            // Haetaan kaikki ruudut, jotka ovat current-ruudusta enintään 'spreadRadius' etäisyydellä.
            List<SquareTile> neighbors = grid.GetTilesInRadius(current, spreadRadius);

            foreach (SquareTile neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);

                    // Ohitetaan vesialueet
                    if (neighbor.Terrain == SquareTile.TerrainType.Merivesi ||
                        neighbor.Terrain == SquareTile.TerrainType.MakeaVesi)
                    {
                        continue;
                    }

                    // Jos ruudussa ei vielä ole tätä uskontoa:
                    // Jos ruudussa on jo uskontoa, vain 20 % todennäköisyys korvata se.
                    if (neighbor.Religion != nation.Religion)
                    {
                        if (neighbor.Religion == null || Random.value < 0.2f)
                        {
                            neighbor.Religion = nation.Religion;
                            tilesUpdated++;
                            //Debug.Log($"Uskonto {nation.Religion.Name} levitetty ruutuun ({neighbor.X}, {neighbor.Y}).");
                            if (tilesUpdated >= maxTilesToSpread)
                                break;
                        }
                    }

                    queue.Enqueue(neighbor);
                }
            }
        }

        //Debug.Log($"SpreadReligionDigging: Päivitetty {tilesUpdated} uutta ruutua uskonnalla {nation.Religion.Name}.");
    }


}
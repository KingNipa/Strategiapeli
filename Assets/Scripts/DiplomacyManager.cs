using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// DiplomacyManager huolehtii valtioiden v�lisist� suhteista: se tallentaa ja p�ivitt�� suhteiden pisteet,
/// laskee niiden vuorokausittaisen "neutralisaation" ja tarkastelee liittouman muodostamisen ehtoja.
/// </summary>
public class DiplomacyManager : MonoBehaviour
{
    public static DiplomacyManager Instance;
    private Dictionary<Nation, Dictionary<Nation, int>> relationships;
    public int allianceThreshold = 10;
    public int neutralPoint = 0;
    public int minRelationship = -30;
    public int maxRelationship = 30;


    // Uusi rakenne hy�kk�yksiin
    public List<Alliance> Alliances { get; private set; } = new List<Alliance>();
    private HashSet<(Nation, Nation)> attackedPairs = new HashSet<(Nation, Nation)>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            relationships = new Dictionary<Nation, Dictionary<Nation, int>>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<Nation> GetAllRegisteredNations()
    {
        return relationships.Keys.ToList();
    }

    // Apumetodi, joka j�rjest�� kahden valtion parin johdonmukaisesti
    private (Nation, Nation) OrderPair(Nation a, Nation b)
    {
        return (a.Name.CompareTo(b.Name) <= 0) ? (a, b) : (b, a);
    }

    public void RegisterNation(Nation nation)
    {
        if (!relationships.ContainsKey(nation))
        {
            relationships[nation] = new Dictionary<Nation, int>();
            foreach (var other in relationships.Keys)
            {
                if (other != nation)
                {
                    relationships[nation][other] = neutralPoint;
                    if (!relationships[other].ContainsKey(nation))
                        relationships[other][nation] = neutralPoint;
                }
            }
        }
    }

    public void AdjustRelationship(Nation nationA, Nation nationB, int delta)
    {
        if (nationA == nationB) return;
        if (!relationships.ContainsKey(nationA)) RegisterNation(nationA);
        if (!relationships.ContainsKey(nationB)) RegisterNation(nationB);

        relationships[nationA][nationB] = Mathf.Clamp(relationships[nationA][nationB] + delta, minRelationship, maxRelationship);
        relationships[nationB][nationA] = Mathf.Clamp(relationships[nationB][nationA] + delta, minRelationship, maxRelationship);


        //Debug.Log($"Diplomatia: {nationA.Name} ja {nationB.Name} suhdetta s��dettiin {delta} pisteell�. Uusi arvo: {relationships[nationA][nationB]}");

        if (relationships[nationA][nationB] >= allianceThreshold)
        {
            //Debug.Log($"Diplomatia: {nationA.Name} ja {nationB.Name} ovat potentiaalisessa liittoumassa!");
        }
    }

    // Muokattu metodi hy�kk�ysten rekister�intiin
    public void RegisterAttack(Nation attacker, Nation defender)
    {
        // Suora hy�kk�yssanktio kohteeseen.
        AdjustRelationship(attacker, defender, -30);
        attackedPairs.Add(OrderPair(attacker, defender));

        //  heikennet��n suhteita muihin valtioihin -1, 50 %:n mahdollisuudella.
        foreach (Nation other in GetAllRegisteredNations())
        {
            if (other != attacker && other != defender)
            {
                if (UnityEngine.Random.value < 0.50f)
                {
                    AdjustRelationship(attacker, other, -1);
                }
            }
        }

        // Resetoi hy�kk�ysten laskuri hy�kk��j�lle
        attacker.TurnsSinceAttack = 0;

        // Peruutetaan mahdolliset iron-kauppasopimukset, jos hy�kk�ys tapahtuu osapuolten v�lill�.
        TradeManager.CancelTradeBetween(attacker, defender);
    }

    // P�ivitet��n vain sotilaalliset (hy�kk�ysten aiheuttamat) suhdearvot
    public void DecayRelationships()
    {
        var pairsToRemove = new List<(Nation, Nation)>();
        foreach (var pair in attackedPairs)
        {
            Nation nationA = pair.Item1;
            Nation nationB = pair.Item2;
            int currentScore = relationships[nationA][nationB];
            if (currentScore < neutralPoint)
                relationships[nationA][nationB] = currentScore + 1;
            else if (currentScore > neutralPoint)
                relationships[nationA][nationB] = currentScore - 1;
            // P�ivitet��n symmetrinen suhde
            relationships[nationB][nationA] = relationships[nationA][nationB];
            if (relationships[nationA][nationB] == neutralPoint)
                pairsToRemove.Add(pair);
        }
        // Poistetaan ne parit, joiden suhde on neutralisoitunut
        foreach (var pair in pairsToRemove)
            attackedPairs.Remove(pair);
    }

    public void UpdateReligionRelationshipsForNation(Nation nation)
    {
        if (nation == null || nation.Religion == null)
            return;

        foreach (var otherNation in relationships.Keys.ToList())
        {
            if (otherNation != nation && otherNation.Religion != null)
            {
                // Tarkistetaan, onko uskonnon vaikutus jo sovellettu t�h�n kansaparin suhteeseen
                if (!nation.ReligiousEffectApplied.ContainsKey(otherNation) || !nation.ReligiousEffectApplied[otherNation])
                {
                    if (nation.Religion.Name == otherNation.Religion.Name)
                    {
                        AdjustRelationship(nation, otherNation, 1);
                    }
                    else
                    {
                        AdjustRelationship(nation, otherNation, -1);
                    }
                    // Merkit��n, ett� vaikutus on sovellettu t�h�n pariin
                    nation.ReligiousEffectApplied[otherNation] = true;
                }
            }
        }
    }

    public int GetRelationship(Nation nationA, Nation nationB)
    {
        if (relationships.ContainsKey(nationA) && relationships[nationA].ContainsKey(nationB))
            return relationships[nationA][nationB];
        return neutralPoint;
    }

    public void ApplyRandomRelationshipAdjustment()
    {
        // Kopioidaan dictionaryn avaimet listaksi
        foreach (var nationA in relationships.Keys.ToList())
        {
            foreach (var nationB in relationships[nationA].Keys.ToList())
            {
                if (nationA.Name.CompareTo(nationB.Name) < 0)
                {
                    float roll = UnityEngine.Random.value; // arvo v�lilt� 0.0 - 1.0
                    if (roll < 0.33f)
                    {
                        AdjustRelationship(nationA, nationB, +1);
                    }
                    else if (roll < 0.66f)
                    {
                        AdjustRelationship(nationA, nationB, -1);
                    }
                    // Muussa tapauksessa suhdetta ei muuteta.
                }
            }
        }
    }

    // Esimerkki: Yritet��n muodostaa liitto kahden valtion v�lill�, jos niiden suhde on yli threshold-arvon
    public bool TryFormAlliance(Nation a, Nation b)
    {

        // Tarkistetaan, ettei jommallakummalla osapuolella ole aktiivista kylm�aikaa.
        if (a.allianceJoinCooldown > 0 || b.allianceJoinCooldown > 0)
        {
            
            return false;
        }

        // Tarkistetaan, ett� molemmat valtiot ovat aktiivisia. Eli tippuneen kanssa ei tee liittoja
        if (!a.IsActive || !b.IsActive)
        {
            return false;
        }

        int rel = GetRelationship(a, b);
        if (rel >= allianceThreshold && !IsInAlliance(a) && !IsInAlliance(b))
        {
            Alliance newAlliance = new Alliance(a, b);
            Alliances.Add(newAlliance);
            // Liiton solmimisen yhteydess� nostetaan suhteita 3 pistett�
            AdjustRelationship(a, b, 3);
         
            return true;
        }
        return false;
    }

    public bool IsInAlliance(Nation nation)
    {
        foreach (var alliance in Alliances)
        {
            if (alliance.Members.Contains(nation))
                return true;
        }
        return false;
    }


    // Lis�t��n uusi j�sen olemassa olevaan liittoon, mik�li kutsuva on liiton alkuper�inen sopija
    public bool AddNationToAlliance(Nation newNation, Alliance alliance, Nation inviter)
    {

        // Estet��n liittoon liittyminen, jos newNationilla on viel� kylm�aika voimassa.
        if (newNation.allianceJoinCooldown > 0)
        {
            
            return false;
        }


        if (alliance.CanInviteNewMember(inviter) && !IsInAlliance(newNation))
        {
            alliance.AddMember(newNation);
            // Liiton solmimisen yhteydess� nostetaan suhteita my�s muiden j�senten v�lill�
            foreach (var member in alliance.Members)
            {
                if (member != newNation)
                    AdjustRelationship(newNation, member, 3);
            }
            //Debug.Log($"{newNation.Name} liitty liittoon {alliance.Name}");
            return true;
        }
        return false;
    }
    public void ProcessAlliancesTurn()
    {
        List<Alliance> expiredAlliances = new List<Alliance>();
        foreach (var alliance in Alliances)
        {
            alliance.ProcessTurn();
            if (alliance.IsExpired())
            {
                expiredAlliances.Add(alliance);
                // Luonnollisen liiton p��ttymisen yhteydess� v�hennet��n suhteita yhdell�
                for (int i = 0; i < alliance.Members.Count; i++)
                {
                    for (int j = i + 1; j < alliance.Members.Count; j++)
                    {
                        AdjustRelationship(alliance.Members[i], alliance.Members[j], -1);
                    }
                }
                //Debug.Log($"Alliance expired: {alliance.Name}");
            }
        }
        foreach (var expired in expiredAlliances)
        {
            Alliances.Remove(expired);
        }
    }

    // Palauttaa liiton, johon valtio kuuluu (jos sellaista on)
    public Alliance GetAllianceForNation(Nation nation)
    {
        foreach (var alliance in Alliances)
        {
            if (alliance.Members.Contains(nation))
                return alliance;
        }
        return null;
    }

    public bool ProposeAlliance(Nation proposer, Nation target)
    {
        // Tarkistetaan, ettei kummallakaan osapuolella ole aktiivista kylm�aikaa.
        if (proposer.allianceJoinCooldown > 0 || target.allianceJoinCooldown > 0)
        {
           
            return false;
        }


        // Tarkistetaan, ett� molemmat valtiot ovat aktiivisia.
        if (!proposer.IsActive || !target.IsActive)
        {
            return false;
        }

        int rel = GetRelationship(proposer, target);
        if (rel < allianceThreshold)
        {
           
            return false;
        }

        // Jos kohde on pelaaja, k�ytet��n interaktiivista p��t�st�.
        if (target == GameManager.Instance.playerNation)
        {
            string message = $"{proposer.Name} is requesting an alliance. Do you accept?";
            GameManager.Instance.allianceConfirmationPanel.ShowConfirmation(true, message, decision =>
            {
                if (decision)
                {
                    AdjustRelationship(proposer, target, +3);
                    if (!IsInAlliance(proposer) && !IsInAlliance(target))
                    {
                        TryFormAlliance(proposer, target);
                    }
                    else if (IsInAlliance(proposer) && !IsInAlliance(target))
                    {
                        Alliance alliance = GetAllianceForNation(proposer);
                        if (alliance != null)
                        {
                            AddNationToAlliance(target, alliance, proposer);
                        }
                    }
                   
                }
                else
                {
                    AdjustRelationship(proposer, target, -5);
                    
                }
            });
            return true; // Pyynt� on k�ynnistetty, p��t�s tehd��n callbackissa.
        }
        // Jos pelaaja on ehdottajana, mutta kohde on teko�ly
        else if (proposer == GameManager.Instance.playerNation)
        {
            // T�ss� yksinkertaistettu logiikka, oletetaan automaattinen hyv�ksynt�.
            bool accepted = true;

            if (accepted)
            {
                AdjustRelationship(proposer, target, +3);
                if (!IsInAlliance(proposer) && !IsInAlliance(target))
                {
                    TryFormAlliance(proposer, target);
                }
                else if (IsInAlliance(proposer) && !IsInAlliance(target))
                {
                    Alliance alliance = GetAllianceForNation(proposer);
                    if (alliance != null)
                    {
                        AddNationToAlliance(target, alliance, proposer);
                    }
                }
              
                GameManager.Instance.allianceConfirmationPanel.ShowResult($"The state {target.Name} accepted your alliance request.");
            }
            else
            {
                AdjustRelationship(proposer, target, -5);
                
                GameManager.Instance.allianceConfirmationPanel.ShowResult($"The state {target.Name} rejected your alliance request.");
            }
            return true;
        }
        else
        {
            // AI:lt� AI:lle tapahtuva ehdotus tehd��n automaattisesti ilman UI-paneelia, joten:
            float baseProbability = 0.55f;
            float extra = (rel - allianceThreshold) * 0.05f;
            float decisionProbability = Mathf.Clamp(baseProbability + extra, 0.5f, 0.9f);
            bool accepted = UnityEngine.Random.value < decisionProbability;

            if (accepted)
            {
                AdjustRelationship(proposer, target, +3);
                if (!IsInAlliance(proposer) && !IsInAlliance(target))
                {
                    TryFormAlliance(proposer, target);
                }
                else if (IsInAlliance(proposer) && !IsInAlliance(target))
                {
                    Alliance alliance = GetAllianceForNation(proposer);
                    if (alliance != null)
                    {
                        AddNationToAlliance(target, alliance, proposer);
                    }
                }
                //Debug.Log($"testi");
                return true;
            }
            else
            {
                AdjustRelationship(proposer, target, -6);
                //Debug.Log($"testi2");
                return false;
            }
        }
    }


}

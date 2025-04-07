using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// DiplomacyManager huolehtii valtioiden välisistä suhteista: se tallentaa ja päivittää suhteiden pisteet,
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


    // Uusi rakenne hyökkäyksiin
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

    // Apumetodi, joka järjestää kahden valtion parin johdonmukaisesti
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


        //Debug.Log($"Diplomatia: {nationA.Name} ja {nationB.Name} suhdetta säädettiin {delta} pisteellä. Uusi arvo: {relationships[nationA][nationB]}");

        if (relationships[nationA][nationB] >= allianceThreshold)
        {
            //Debug.Log($"Diplomatia: {nationA.Name} ja {nationB.Name} ovat potentiaalisessa liittoumassa!");
        }
    }

    // Muokattu metodi hyökkäysten rekisteröintiin
    public void RegisterAttack(Nation attacker, Nation defender)
    {
        // Suora hyökkäyssanktio kohteeseen.
        AdjustRelationship(attacker, defender, -30);
        attackedPairs.Add(OrderPair(attacker, defender));

        //  heikennetään suhteita muihin valtioihin -1, 50 %:n mahdollisuudella.
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

        // Resetoi hyökkäysten laskuri hyökkääjälle
        attacker.TurnsSinceAttack = 0;

        // Peruutetaan mahdolliset iron-kauppasopimukset, jos hyökkäys tapahtuu osapuolten välillä.
        TradeManager.CancelTradeBetween(attacker, defender);
    }

    // Päivitetään vain sotilaalliset (hyökkäysten aiheuttamat) suhdearvot
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
            // Päivitetään symmetrinen suhde
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
                // Tarkistetaan, onko uskonnon vaikutus jo sovellettu tähän kansaparin suhteeseen
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
                    // Merkitään, että vaikutus on sovellettu tähän pariin
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
                    float roll = UnityEngine.Random.value; // arvo väliltä 0.0 - 1.0
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

    // Esimerkki: Yritetään muodostaa liitto kahden valtion välillä, jos niiden suhde on yli threshold-arvon
    public bool TryFormAlliance(Nation a, Nation b)
    {

        // Tarkistetaan, ettei jommallakummalla osapuolella ole aktiivista kylmäaikaa.
        if (a.allianceJoinCooldown > 0 || b.allianceJoinCooldown > 0)
        {
            
            return false;
        }

        // Tarkistetaan, että molemmat valtiot ovat aktiivisia. Eli tippuneen kanssa ei tee liittoja
        if (!a.IsActive || !b.IsActive)
        {
            return false;
        }

        int rel = GetRelationship(a, b);
        if (rel >= allianceThreshold && !IsInAlliance(a) && !IsInAlliance(b))
        {
            Alliance newAlliance = new Alliance(a, b);
            Alliances.Add(newAlliance);
            // Liiton solmimisen yhteydessä nostetaan suhteita 3 pistettä
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


    // Lisätään uusi jäsen olemassa olevaan liittoon, mikäli kutsuva on liiton alkuperäinen sopija
    public bool AddNationToAlliance(Nation newNation, Alliance alliance, Nation inviter)
    {

        // Estetään liittoon liittyminen, jos newNationilla on vielä kylmäaika voimassa.
        if (newNation.allianceJoinCooldown > 0)
        {
            
            return false;
        }


        if (alliance.CanInviteNewMember(inviter) && !IsInAlliance(newNation))
        {
            alliance.AddMember(newNation);
            // Liiton solmimisen yhteydessä nostetaan suhteita myös muiden jäsenten välillä
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
                // Luonnollisen liiton päättymisen yhteydessä vähennetään suhteita yhdellä
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
        // Tarkistetaan, ettei kummallakaan osapuolella ole aktiivista kylmäaikaa.
        if (proposer.allianceJoinCooldown > 0 || target.allianceJoinCooldown > 0)
        {
           
            return false;
        }


        // Tarkistetaan, että molemmat valtiot ovat aktiivisia.
        if (!proposer.IsActive || !target.IsActive)
        {
            return false;
        }

        int rel = GetRelationship(proposer, target);
        if (rel < allianceThreshold)
        {
           
            return false;
        }

        // Jos kohde on pelaaja, käytetään interaktiivista päätöstä.
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
            return true; // Pyyntö on käynnistetty, päätös tehdään callbackissa.
        }
        // Jos pelaaja on ehdottajana, mutta kohde on tekoäly
        else if (proposer == GameManager.Instance.playerNation)
        {
            // Tässä yksinkertaistettu logiikka, oletetaan automaattinen hyväksyntä.
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
            // AI:ltä AI:lle tapahtuva ehdotus tehdään automaattisesti ilman UI-paneelia, joten:
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

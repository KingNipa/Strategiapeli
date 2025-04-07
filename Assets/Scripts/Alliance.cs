using System.Collections.Generic;
using UnityEngine;

public class Alliance
{
    // Liiton nimi, muodostuu alkuper�isten sopijien nimien ensimm�isist� kahdesta kirjaimesta.
    public string Name { get; private set; }
    // Liiton j�senet
    public List<Nation> Members { get; private set; }
    // Alkuper�iset sopijat
    public Nation OriginalNationA { get; private set; }
    public Nation OriginalNationB { get; private set; }
    
    public int RemainingTurns { get; private set; }

    // liiton v�ri� varten
    public Color AllianceColor { get; private set; }

    // Konstruktorissa asetetaan alkuarvot ja muodostetaan liiton nimi.
    public Alliance(Nation nationA, Nation nationB)
    {
        OriginalNationA = nationA;
        OriginalNationB = nationB;
        Members = new List<Nation> { nationA, nationB };
        RemainingTurns = 30;
        Name = GenerateAllianceName(nationA, nationB);

        AllianceColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.7f, 1f);
    }

    // Liiton nimen muodostus: otetaan kunkin alkuper�isen valtion nimest� kaksi ensimm�ist� kirjainta.
    private string GenerateAllianceName(Nation a, Nation b)
    {
        string partA = a.Name.Length >= 2 ? a.Name.Substring(0, 2) : a.Name;
        string partB = b.Name.Length >= 2 ? b.Name.Substring(0, 2) : b.Name;
        return partA + partB;
    }

    // Tarkistetaan, onko kutsuva valtio yksi liiton alkuper�isist� sopijoista.
    public bool CanInviteNewMember(Nation inviter)
    {
        return Members.Contains(inviter);
    }

    // Lis�t��n uusi j�sen liittoon, mik�li sit� ei viel� ole.
    public void AddMember(Nation newNation)
    {
        if (!Members.Contains(newNation))
        {
            Members.Add(newNation);
            // Liiton solmimisen yhteydess� suhteet voisi my�s nousta
        }
    }

    public void RemoveMember(Nation leavingNation)
    {
        if (Members.Contains(leavingNation))
        {
            Members.Remove(leavingNation);
         
            foreach (Nation member in Members)
            {
                DiplomacyManager.Instance.AdjustRelationship(leavingNation, member, -10);
            }
          
            // jos pelaaja on liitossa:
            if (Members.Contains(GameManager.Instance.playerNation))
            {
                GameManager.Instance.ShowNotificationWindow($"{leavingNation.Name} has left the alliance {Name}.");
            }
        }
        else
        {
        //
        }
    }

    // P�ivitet��n liiton kestoa joka vuorolla.
    public void ProcessTurn()
    {
        RemainingTurns--;
    }

    // Tarkistetaan, onko liitto vanhentunut (30 vuoron j�lkeen).
    public bool IsExpired()
    {
        return RemainingTurns <= 0;
    }
}


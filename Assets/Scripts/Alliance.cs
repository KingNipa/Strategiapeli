using System.Collections.Generic;
using UnityEngine;

public class Alliance
{
    // Liiton nimi, muodostuu alkuperäisten sopijien nimien ensimmäisistä kahdesta kirjaimesta.
    public string Name { get; private set; }
    // Liiton jäsenet
    public List<Nation> Members { get; private set; }
    // Alkuperäiset sopijat
    public Nation OriginalNationA { get; private set; }
    public Nation OriginalNationB { get; private set; }
    
    public int RemainingTurns { get; private set; }

    // liiton väriä varten
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

    // Liiton nimen muodostus: otetaan kunkin alkuperäisen valtion nimestä kaksi ensimmäistä kirjainta.
    private string GenerateAllianceName(Nation a, Nation b)
    {
        string partA = a.Name.Length >= 2 ? a.Name.Substring(0, 2) : a.Name;
        string partB = b.Name.Length >= 2 ? b.Name.Substring(0, 2) : b.Name;
        return partA + partB;
    }

    // Tarkistetaan, onko kutsuva valtio yksi liiton alkuperäisistä sopijoista.
    public bool CanInviteNewMember(Nation inviter)
    {
        return Members.Contains(inviter);
    }

    // Lisätään uusi jäsen liittoon, mikäli sitä ei vielä ole.
    public void AddMember(Nation newNation)
    {
        if (!Members.Contains(newNation))
        {
            Members.Add(newNation);
            // Liiton solmimisen yhteydessä suhteet voisi myös nousta
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

    // Päivitetään liiton kestoa joka vuorolla.
    public void ProcessTurn()
    {
        RemainingTurns--;
    }

    // Tarkistetaan, onko liitto vanhentunut (30 vuoron jälkeen).
    public bool IsExpired()
    {
        return RemainingTurns <= 0;
    }
}


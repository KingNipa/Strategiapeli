using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class TradeManager : MonoBehaviour
{
  
    public static bool ExecuteTrade(Nation seller, Nation buyer)
    {
        if (!IsTradePossible(seller, buyer))
        {
            //Debug.Log("Kauppa ei ole mahdollinen.");
            return false;
        }

        // Rahansiirto: ostaja maksaa 2, myyj‰ saa 2.
        buyer.GDP -= 2;
        seller.GDP += 2;

        // P‰ivitet‰‰n kaupasta saatava tulo kansakunnille
        seller.TradeIncome += 2;
        buyer.TradeIncome -= 2;

        // P‰ivitet‰‰n diplomaattisia suhteita
        DiplomacyManager.Instance.AdjustRelationship(seller, buyer, +5);

        // Asetetaan kauppa-efekti: ostaja saa raudan k‰yttˆoikeuden 20 vuoron ajaksi.
        buyer.IronTradeTurnsRemaining = 20;
        buyer.IronTradeSeller = seller;

        // Lis‰t‰‰n debug-logit kaupan suunnan selkeytt‰miseksi
        if (seller == GameManager.Instance.playerNation)
        {
            //Debug.Log("Teko‰ly osti rautaa pelaajalta!");
        }
        else if (buyer == GameManager.Instance.playerNation)
        {
            //Debug.Log("Pelaaja osti rautaa teko‰lylt‰!");
        }
        else
        {
            //Debug.Log("Kauppa teko‰lyjen v‰lill‰ onnistui!");
        }

        //Debug.Log("Rautakauppa onnistui!");
        return true;
    }

    // Esimerkkimetodi tarkistamaan, ovatko valtiot samalla mantereella
    public static bool IsOnSameContinent(Nation nationA, Nation nationB)
    {
        // Toteuta tarkistus esimerkiksi vertailemalla, ovatko maiden hallitsemat ruudut yhteydess‰ ilman merialueita.
        return true; // Palautetaan true t‰ss‰ prototyyppivaiheessa
    }
    private void ProcessIronTrade()
    {
        TradeManager.ProcessIronTradeForNation(GameManager.Instance.playerNation);
    }

    public static void ProcessIronTradeForNation(Nation nation)
    {
        if (nation.HasActiveIronTrade)
        {
            // Tarkistetaan, ett‰ myyj‰ on edelleen voimassa (myyj‰ll‰ on v‰hint‰‰n yksi ruutu, jossa on ironiaa)
            if (nation.IronTradeSeller == null || !NationHasIron(nation.IronTradeSeller))
            {
                //Debug.Log("Iron-kauppa peruutetaan, koska myyj‰ ei en‰‰ hallitse ironiaa sis‰lt‰vi‰ ruutuja.");
                nation.IronTradeTurnsRemaining = 0;
                nation.IronTradeSeller = null;
                return;
            }

            // Kaupan normaalin k‰sittelyn jatkaminen
            nation.GDP -= 2;
            if (nation.IronTradeSeller != null)
            {
                nation.IronTradeSeller.GDP += 2;
            }
            nation.IronTradeTurnsRemaining--;
            if (nation.IronTradeTurnsRemaining <= 0)
            {
                nation.IronTradeSeller = null;
            }
        }
    }

    public static bool NationHasIron(Nation nation)
    {
        SquareGrid grid = Object.FindObjectOfType<SquareGrid>();
        if (grid == null) return false;

        bool hasIron = grid.GetControlledTiles().Any(tile => {
            bool result = tile.controllingNation == nation && tile.HasIron;
            if (nation == GameManager.Instance.playerNation)
            {
                //Debug.Log($"Tile ({tile.X},{tile.Y}) HasIron: {tile.HasIron}");
            }
            return result;
        });
        //Debug.Log($"Nation {nation.Name} has iron: {hasIron}");
        return hasIron;
    }

    public static bool PlayerHasIron()
    {
        SquareGrid grid = Object.FindObjectOfType<SquareGrid>();
        if (grid == null) return false;

        Nation player = GameManager.Instance.playerNation;
        // K‰yd‰‰n l‰pi kaikki pelaajan hallitsemat ruudut
        return grid.GetControlledTiles().Any(tile => tile.controllingNation == player && tile.HasIron);
    }

    private static bool AreNeighbors(Nation seller, Nation buyer)
    {
        SquareGrid grid = Object.FindObjectOfType<SquareGrid>();
        if (grid == null) return false;

        var sellerTiles = grid.GetControlledTiles().Where(t => t.controllingNation == seller);
        foreach (var tile in sellerTiles)
        {
            if (grid.GetNeighbors(tile).Any(n => n.controllingNation == buyer))
                return true;
        }
        return false;
    }

    public static bool IsTradePossible(Nation seller, Nation buyer)
    {
        int relation = DiplomacyManager.Instance.GetRelationship(seller, buyer);
        if (relation < -10)
            return false;

        if (!AreNeighbors(seller, buyer))
            return false;

        if (!IsOnSameContinent(seller, buyer))
            return false;

        if (seller == GameManager.Instance.playerNation && !seller.HasIronCard)
            return false;

        // Jos pelaaja toimii myyj‰n‰, varmista, ettei h‰nell‰ ole raudan puutetta
        if (seller == GameManager.Instance.playerNation && !NationHasIron(seller))
            return false;

        // Jos ostajana on teko‰ly, varmista, ett‰ sill‰ itsell‰‰n ei ole jo rautaa
        if (buyer != GameManager.Instance.playerNation && NationHasIron(buyer))
            return false;

        // Jos ostajana on pelaaja, tarkista, ett‰ pelaaja on aktivoinut iron-kortin.
        if (buyer == GameManager.Instance.playerNation && !buyer.HasIronCard)
            return false;

        return true;
    }

    public static int playerTradeDeclineCooldown = 0;

    public static void CancelTradeBetween(Nation nationA, Nation nationB)
    {
        // Jos nationA on ostajana ja sen myyj‰ on nationB, peruutetaan kauppa
        if (nationA.HasActiveIronTrade && nationA.IronTradeSeller == nationB)
        {
            //Debug.Log($"Kauppasopimus keskeytyy hyˆkk‰yksen seurauksena: {nationA.Name} (ostaja) ja {nationB.Name} (myyj‰).");
            nationA.IronTradeTurnsRemaining = 0;
            nationA.IronTradeSeller = null;
        }
        // Jos nationB on ostajana ja sen myyj‰ on nationA, peruutetaan kauppa
        if (nationB.HasActiveIronTrade && nationB.IronTradeSeller == nationA)
        {
            //Debug.Log($"Kauppasopimus keskeytyy hyˆkk‰yksen seurauksena: {nationB.Name} (ostaja) ja {nationA.Name} (myyj‰).");
            nationB.IronTradeTurnsRemaining = 0;
            nationB.IronTradeSeller = null;
        }
    }

    public void OnCancelButtonClicked()
    {
        TradeManager.playerTradeDeclineCooldown = 20;
        gameObject.SetActive(false);
    }

}

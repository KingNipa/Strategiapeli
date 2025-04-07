using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TradePanel : MonoBehaviour
{
    public Text infoText;
    public Button tradeButton;
    public Button cancelButton;

    private Nation sellerNation;
    private Nation buyerNation;

    public void InitializeTrade(Nation seller, Nation buyer)
    {
        sellerNation = seller;
        buyerNation = buyer;

        // Tarkistetaan, kumpi osapuoli on pelaaja.
        if (seller == GameManager.Instance.playerNation)
        {
            // Pelaaja myy raudasta ñ teko‰ly ostaa
            infoText.text = $"{buyerNation.Name} wants to buy iron.\n" +
                            "Price: 2 money, iron usage for 20 turns\n" +
                            "Do you accept the trade?";
            tradeButton.interactable = CheckTradeConditionsForPlayerSelling();
        }
        else if (buyer == GameManager.Instance.playerNation)
        {
            // Pelaaja ostaa raudasta ñ teko‰ly tarjoaa
            infoText.text = $"{sellerNation.Name} offers iron.\n" +
                            $"Price: 2 money, iron usage for 20 turns";
            tradeButton.interactable = CheckTradeConditionsForPlayerBuying();
        }
        else
        {
            // Jos molemmat ovat teko‰lyj‰, k‰ytet‰‰n yleist‰ tarkistusta
            infoText.text = $"{sellerNation.Name} and {buyerNation.Name} are trading iron.";
            tradeButton.interactable = CheckTradeConditions();
        }

        // Varmistetaan, ett‰ cancel-painike on aina k‰ytett‰viss‰
        cancelButton.interactable = true;
        tradeButton.onClick.RemoveAllListeners();
        tradeButton.onClick.AddListener(OnTradeButtonClicked);
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    // Yleinen tarkistus (AI-AI tai muu)
    private bool CheckTradeConditions()
    {
        int relation = DiplomacyManager.Instance.GetRelationship(sellerNation, buyerNation);
        if (relation < -10)
            return false;
        if (!sellerNation.HasIronCard)
            return false;
        if (!TradeManager.IsOnSameContinent(sellerNation, buyerNation))
            return false;
        return true;
    }

    // Tarkistus pelaajan myyntitilanteelle (ei vaadita rautakorttia pelaajalta)
    private bool CheckTradeConditionsForPlayerSelling()
    {
        int relation = DiplomacyManager.Instance.GetRelationship(sellerNation, buyerNation);
        if (relation < -10)
            return false;
        if (!TradeManager.IsOnSameContinent(sellerNation, buyerNation))
            return false;
        return true;
    }

    // Tarkistus pelaajan ostotilanteelle (vaaditaan, ett‰ myyj‰ll‰ on rautakortti)
    private bool CheckTradeConditionsForPlayerBuying()
    {
        int relation = DiplomacyManager.Instance.GetRelationship(sellerNation, buyerNation);
        if (relation < -10)
            return false;
        if (!sellerNation.HasIronCard)
            return false;
        if (!TradeManager.IsOnSameContinent(sellerNation, buyerNation))
            return false;
        return true;
    }

    public void OnTradeButtonClicked()
    {
        bool success = TradeManager.ExecuteTrade(sellerNation, buyerNation);
        if (success)
        {
            if (GameManager.Instance.playerNation == sellerNation)
                infoText.text = $"The state {buyerNation.Name} accepted your trade request.";
            else if (GameManager.Instance.playerNation == buyerNation)
                infoText.text = $"The state {sellerNation.Name} accepted your trade request.";
        }
        else
        {
            if (GameManager.Instance.playerNation == sellerNation)
                infoText.text = $"The state {buyerNation.Name} rejected your trade request.";
            else if (GameManager.Instance.playerNation == buyerNation)
                infoText.text = $"The state {sellerNation.Name} rejected your trade request.";
        }
        gameObject.SetActive(false);
    }

    public void OnCancelButtonClicked()
    {
        // Jos pelaaja myy raudasta (eli on myyj‰), hylk‰ys heikent‰‰ suhteita
        if (GameManager.Instance.playerNation == sellerNation)
        {
            DiplomacyManager.Instance.AdjustRelationship(sellerNation, buyerNation, -3);
            infoText.text = $"You rejected the trade. Relationship with {buyerNation.Name} decreased.";
        }
        // Jos pelaaja ostaa raudasta, cancel peruuttaa kaupan ilman suhteiden muuttamista
        else if (GameManager.Instance.playerNation == buyerNation)
        {
            infoText.text = "Trade cancelled.";
        }
        TradeManager.playerTradeDeclineCooldown = 10;
        gameObject.SetActive(false);
    }

}

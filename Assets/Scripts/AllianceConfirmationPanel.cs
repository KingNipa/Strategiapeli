using UnityEngine;
using UnityEngine.UI;
using System;

public class AllianceConfirmationPanel : MonoBehaviour
{
    public Text messageText;          // Näyttää viestin
    public Button confirmButton;      // Hyväksymispainike
    public Button rejectButton;       // Hylkäyspainike

    // Callback, joka palauttaa pelaajan päätöksen
    private Action<bool> decisionCallback;

    void Start()
    {
        gameObject.SetActive(false);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(() => OnDecision(true));
        if (rejectButton != null)
            rejectButton.onClick.AddListener(() => OnDecision(false));
    }

    /// <summary>
    /// Näyttää liittoehdotus-paneelin.
    /// </summary>
    /// <param name="interactive">
    /// Jos true, kyseessä on tilanne, jossa pelaajan on annettava päätös (molemmat painikkeet näkyvissä).
    /// Jos false, kyseessä on non-interaktiivinen tilanne – vain confirm-painike näkyy.
    /// </param>
    /// <param name="message">Näytettävä viesti</param>
    /// <param name="callback">
    /// Callback, joka palauttaa päätöksen
    /// </param>
    public void ShowConfirmation(bool interactive, string message, Action<bool> callback = null)
    {
        messageText.text = message;
        decisionCallback = callback;
        if (interactive)
        {
            if (confirmButton != null) confirmButton.gameObject.SetActive(true);
            if (rejectButton != null) rejectButton.gameObject.SetActive(true);
        }
        else
        {
            if (confirmButton != null) confirmButton.gameObject.SetActive(true);
            if (rejectButton != null) rejectButton.gameObject.SetActive(false);
        }
        gameObject.SetActive(true);
    }

    private void OnDecision(bool decision)
    {
        decisionCallback?.Invoke(decision);
        HidePanel();
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    public void ShowResult(string message)
    {
        messageText.text = message;
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
        if (rejectButton != null)
            rejectButton.gameObject.SetActive(false);
        gameObject.SetActive(true);
    }


}

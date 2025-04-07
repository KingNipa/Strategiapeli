using UnityEngine;
using UnityEngine.UI;
using System;

public class AllianceConfirmationPanel : MonoBehaviour
{
    public Text messageText;          // N�ytt�� viestin
    public Button confirmButton;      // Hyv�ksymispainike
    public Button rejectButton;       // Hylk�yspainike

    // Callback, joka palauttaa pelaajan p��t�ksen
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
    /// N�ytt�� liittoehdotus-paneelin.
    /// </summary>
    /// <param name="interactive">
    /// Jos true, kyseess� on tilanne, jossa pelaajan on annettava p��t�s (molemmat painikkeet n�kyviss�).
    /// Jos false, kyseess� on non-interaktiivinen tilanne � vain confirm-painike n�kyy.
    /// </param>
    /// <param name="message">N�ytett�v� viesti</param>
    /// <param name="callback">
    /// Callback, joka palauttaa p��t�ksen
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

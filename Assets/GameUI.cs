using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public Transform InteractPanel;
    public Text InteractText;

    public Text HelpText;

    public Transform HoldingFishingRodPanel;
    public Text HoldingFishingRodText;
    public Slider HoldingFishingRodSlider;

    public Transform FightingPanel;
    public Text FightingText;
    public Slider FightingSlider;

    public Transform EndFightPanel;
    public Text EndFightText;
    public Text EndFightButtonText;

    public Text LineDistanceText;

    public Transform ProgressPanel;
    public Text ProgressText;

    public Transform LetterPanel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CanInteractWith(string msg)
    {
        InteractPanel.gameObject.SetActive(true);
        InteractText.text = msg;
    }
    public void CannotInteract()
    {
        InteractPanel.gameObject.SetActive(false);
        InteractText.text = "";
    }

    public void ShowHelp(string helpFor)
    {
        HelpText.text = "";

        if (helpFor == "HoldingRod")
        {
            HelpText.text = "Charge by holding the LMB\nRelease LMB to throw line\n\nPress 'E' to put Fishing Rod back";
        }
        else if (helpFor == "Fishing")
        {
            HelpText.text = "Reel in by holding LMB\nCancel with RMB";
        }
    }

    public void HideHelp()
    {
        HelpText.text = "";
    }

    public void UpdateHoldingFishingRodPanel(bool enabled, float perc = 0f, string text = "")
    {
        HoldingFishingRodPanel.gameObject.SetActive(enabled);
        HoldingFishingRodSlider.value = perc;
        HoldingFishingRodText.text = text;
    }

    public void UpdateFightingPanel(bool enabled, float perc = 0f, string text = "")
    {
        FightingPanel.gameObject.SetActive(enabled);
        FightingSlider.value = perc;
        FightingText.text = text;
    }

    public void UpdateEndFightPanel(bool enabled, string text = "", string buttonText = "Continue")
    {
        EndFightPanel.gameObject.SetActive(enabled);
        EndFightText.text = text;
        EndFightButtonText.text = buttonText;
    }

    public void UpdateLineDistance(bool enabled, float meters = 0f)
    {
        LineDistanceText.enabled = enabled;
        LineDistanceText.text = $"Distance: {meters.ToString("0.##")} m";
    }

    public void UpdateProgressText(bool enabled, int friendlyCount = 0, int shyCount = 0, float force = 0)
    {
        ProgressPanel.gameObject.SetActive(enabled);
        string txt = $"Friendly: {friendlyCount}\nShy: {shyCount}\n\nStrength: {force.ToString("0.##")}";
        ProgressText.text = txt;
    }

    public void UpdateLetterPanel(bool enabled)
    {
        LetterPanel.gameObject.SetActive(enabled);
    }

}

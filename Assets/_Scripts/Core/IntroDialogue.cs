using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroDialogue : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text dialogueText;
    [SerializeField] Button continueButton;

    string[] lines = {
        "Hey! I'm Cath and welcome to HackPrinceton.",
        "Workers: stand at desks to fill the progress bar. Finish before time runs out!",
        "Saboteurs: disrupt the Workers. Use Q to unplug the router and stun them.",
        "Watch out for the Judge, he patrols the office and stuns anyone slacking.",
        "At 2:00, the Tattletale Terminal appears. Race to it and accuse someone.",
        "The Judge will decide on the truth.",
        "Lie well and your target gets stunned. Get caught lying... and YOU get stunned.",
        "Good luck and DON'T DISTURB OTHER PLAYERS. 😈"
    };

    int _index = 0;

    void Start()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
        dialogueText.text = lines[0];
        continueButton.onClick.AddListener(NextLine);
    }

    void NextLine()
    {
        _index++;
        if (_index >= lines.Length)
        {
            panel.SetActive(false);
            Time.timeScale = 1f;
            return;
        }
        dialogueText.text = lines[_index];
    }
}
using UnityEngine;
using TMPro;

public class IntroDialogue : MonoBehaviour
{
    [SerializeField] TMP_Text dialogueText;

    string[] lines = {
        "Hey! I'm Cath, your friendly neighborhood HackPrinceton guide.",
        "Workers: stand at desks to fill the progress bar.",
        "Saboteurs: press Q to unplug the router and stun Workers.",
        "Watch out for the Judge — he stuns everyone.",
        "At 2:00, the Tattletale Terminal appears. Press E to use it.",
        "Claude decides: guilty = they get stunned. Lie = YOU get stunned.",
        "Good luck. DON'T DISTURB OTHERS!"
    };

    int _index = 0;

    void Start()
    {
        dialogueText.text = lines[0];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            _index++;
            if (_index >= lines.Length)
            {
                gameObject.SetActive(false);
                return;
            }
            dialogueText.text = lines[_index];
        }
    }
}
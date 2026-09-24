using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Dialogue : MonoBehaviour
{
    private UI ui;

    [SerializeField] private Image speakerPortrait;
    [SerializeField] private TextMeshProUGUI speakerName;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI[] dialogueChoices;

    [Space]
    [SerializeField] private float typingSpeed = 0.05f; // Time delay between each letter
    private string fullTextToShow; // The full text to display
    private Coroutine typingCoroutine;

    private Dialogue_LineSO currentLine;
    private Dialogue_LineSO[] currentChoices;
    private Dialogue_LineSO selectedChoice;
    private int selectedChoiceIndex;

    private bool waitingToConfirm;

    void Awake()
    {
        ui = GetComponentInParent<UI>();
    }

    public void PlayDialogueLine(Dialogue_LineSO line)
    {
        currentLine = line;
        currentChoices = line.choiceLines;

        speakerPortrait.sprite = line.speaker.speakerPortrait;
        speakerName.text = line.speaker.speakerName;

        fullTextToShow = line.GetRandomLine();
        typingCoroutine = StartCoroutine(TypeTextCo(fullTextToShow)); 
    }

    private void HandleNextAction()
    {
        switch (currentLine.actionType)
        {
            case DialogueActionType.OpenShop:
                // Open merchant UI
                ui.SwitchToInGameUI();
                ui.OpenMerchantUI(true);
                break;
        }
    }

    public void DialogueInteraction()
    {
        if(typingCoroutine != null)
        {
            CompleteTyping();
            waitingToConfirm = true;
            return;
        }

        if(waitingToConfirm)
        {
            waitingToConfirm = false;
            HandleNextAction();
        }
    }

    private void CompleteTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = fullTextToShow; // Show the full text immediately
            typingCoroutine = null; // Reset the coroutine reference
        }
    }

    private IEnumerator TypeTextCo(string text)
    {
        dialogueText.text = "";

        foreach (char letter in text)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        waitingToConfirm = true; // Now waiting for player confirmation to proceed

        typingCoroutine = null; // Reset the coroutine reference
    }
}

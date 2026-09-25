using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Dialogue : MonoBehaviour
{
    private UI ui;
    private DialogueNpcData currentNpcData;
    private Player_QuestManager questManager;

    [SerializeField] private Image speakerPortrait;
    [SerializeField] private TextMeshProUGUI speakerName;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI[] dialogueChoicesText;

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
        questManager = Player.Instance.questManager;

        for (int i = 0; i < dialogueChoicesText.Length; i++)
        {
            DialogueChoiceHandler handler = dialogueChoicesText[i].GetComponent<DialogueChoiceHandler>();
            handler.Setup(i);
            handler.OnHover += selectedIndex => {
                selectedChoiceIndex = selectedIndex;
                ShowChoices(); // Refresh the choices display to reflect the new selection
            };
            handler.OnClick += choiceIndex => {
                selectedChoiceIndex = choiceIndex;
                selectedChoice = currentChoices[selectedChoiceIndex];
                PlayDialogueLine(selectedChoice);
            };
        }
    }

    public void SetupNpcData(DialogueNpcData npcData) => currentNpcData = npcData;

    public void PlayDialogueLine(Dialogue_LineSO line)
    {
        currentLine = line;
        currentChoices = line.choiceLines;
        selectedChoice = null; // Reset any previous choice

        HideAllChoices(); // Hide all choices initially

        speakerPortrait.sprite = line.speaker.speakerPortrait;
        speakerName.text = line.speaker.speakerName;

        fullTextToShow = line.actionType == DialogueActionType.None || line.actionType == DialogueActionType.PlayerMakeChoice ? line.GetRandomLine() : line.actionLine;
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
            case DialogueActionType.PlayerMakeChoice:
                if(selectedChoice == null)
                {
                    selectedChoiceIndex = 0; // Default to the first choice
                    ShowChoices();
                }
                else
                {
                    PlayDialogueLine(currentChoices[selectedChoiceIndex]);
                }
                break;
            case DialogueActionType.OpenQuest:
                // Open quest UI
                ui.SwitchToInGameUI();
                ui.OpenQuestUI(currentNpcData.quests);
                break;
            case DialogueActionType.GetQuestReward:
                ui.SwitchToInGameUI();
                questManager.TryGiveRewardFrom(currentNpcData.rewardGiver);
                break;
            case DialogueActionType.OpenCraft:
                // Open craft UI
                ui.SwitchToInGameUI();
                ui.OpenCraftUI(true);
                break;
            case DialogueActionType.OpenStorage:
                // Open storage UI
                ui.SwitchToInGameUI();
                ui.OpenStorageUI(true);
                break;
            case DialogueActionType.CloseDialogue:
                ui.SwitchToInGameUI();
                break;
        }
    }

    public void DialogueInteraction()
    {
        if(typingCoroutine != null)
        {
            CompleteTyping();
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
            OnTypingFinished();
        }
    }

    private void OnTypingFinished()
    {
        waitingToConfirm = true; // Now waiting for player confirmation to proceed

        // Show choices right away instead of waiting for another input
        if (currentLine.actionType == DialogueActionType.PlayerMakeChoice)
        {
            selectedChoiceIndex = 0;
            ShowChoices();
        }
        // Open the shop right after its action line finishes
        else if (currentLine.actionType == DialogueActionType.OpenShop)
        {
            waitingToConfirm = false;
            HandleNextAction();
        }
    }

    private void ShowChoices()
    {
        HideAllChoices();

        for(int i = 0; i < dialogueChoicesText.Length; i++)
        {
            if(i < currentChoices.Length)
            {
                Dialogue_LineSO choice = currentChoices[i];
                string choiceText = choice.playerChoiceAnswer;

                dialogueChoicesText[i].gameObject.SetActive(true);
                dialogueChoicesText[i].text = selectedChoiceIndex == i ? $"<color=yellow> {i + 1}) {choiceText}" : $"{i + 1}) {choiceText}";


                if(choice.actionType == DialogueActionType.GetQuestReward && !questManager.HasCompletedQuest())
                {
                    dialogueChoicesText[i].gameObject.SetActive(false); // Hide the choice if the player hasn't completed the quest
                }
            }
            // else
            // {
            //     dialogueChoicesText[i].gameObject.SetActive(false);
            // }
        }

        selectedChoice = currentChoices[selectedChoiceIndex];
    }

    private void HideAllChoices(){
        foreach(var obj in dialogueChoicesText){
            obj.gameObject.SetActive(false);
        }
    }

    public void NavigateChoices(int direction)
    {
        if(currentChoices == null || currentChoices.Length <= 1) return;

        selectedChoiceIndex += direction;
        selectedChoiceIndex = Mathf.Clamp(selectedChoiceIndex, 0, currentChoices.Length - 1);

        ShowChoices(); // Refresh the choices display to reflect the new selection
    }

    private IEnumerator TypeTextCo(string text)
    {
        dialogueText.text = "";

        foreach (char letter in text)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null; // Reset the coroutine reference
        OnTypingFinished();
    }

    private void SelectChoice(int index)
    {
        selectedChoiceIndex = index;
        ShowChoices();
    }
 
    private void ConfirmChoice(int index)
    {
        SelectChoice(index);
        DialogueInteraction();
    }
}

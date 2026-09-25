using UnityEngine;

public class UI : MonoBehaviour
{
    public static UI Instance;
    [SerializeField] private GameObject[] uiElements;
    public bool alternativeInput { get; private set; }
    private PlayerInputSet input;

    #region UI Components
    public UI_SkillToolTip skillToolTip { get; private set; }
    public UI_ItemToolTip itemToolTip { get; private set; }
    public UI_StatToolTip statToolTip { get; private set; }
    public UI_SkillTree skillTreeUI { get; private set; }
    public UI_Inventory inventoryUI { get; private set; }
    public UI_Storage storageUI { get; private set; }
    public UI_Craft craftUI { get; private set; }
    public UI_Merchant merchantUI { get; private set; }
    public UI_InGame inGameUI { get; private set; }
    public UI_Options optionsUI { get; private set; }
    public UI_DeathScreen deathScreenUI { get; private set; }
    public UI_FadeScreen fadeUI { get; private set; }
    public UI_Quest questUI { get; private set; }
    public UI_ActiveQuest activeQuestUI { get; private set; }
    public UI_Dialogue dialogueUI { get; private set; }
    #endregion
    private bool skillTreeEnabled;
    private bool inventoryEnabled;

    private void Awake()
    {
        Instance = this;

        skillToolTip = GetComponentInChildren<UI_SkillToolTip>();
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>();
        statToolTip = GetComponentInChildren<UI_StatToolTip>();

        skillTreeUI = GetComponentInChildren<UI_SkillTree>(true); // This line is can find skill tree if it is inactive
        inventoryUI = GetComponentInChildren<UI_Inventory>(true); // This line is can find inventory if it is inactive
        storageUI = GetComponentInChildren<UI_Storage>(true); // This line is can find storage if it is inactive
        craftUI = GetComponentInChildren<UI_Craft>(true); // This line is can find craft if it is inactive
        merchantUI = GetComponentInChildren<UI_Merchant>(true); // This line is can find merchant if it is inactive
        inGameUI = GetComponentInChildren<UI_InGame>(true); // This line is can find in game UI if it is inactive
        optionsUI = GetComponentInChildren<UI_Options>(true); // This line is can find options if it is inactive
        deathScreenUI = GetComponentInChildren<UI_DeathScreen>(true); // This line is can find death screen if it is inactive
        fadeUI = GetComponentInChildren<UI_FadeScreen>(true); // This line is can find fade screen if it is inactive
        questUI = GetComponentInChildren<UI_Quest>(true); // This line is can find quest UI if it is inactive
        activeQuestUI = GetComponentInChildren<UI_ActiveQuest>(true); // This line is can find active quest UI if it is inactive
        dialogueUI = GetComponentInChildren<UI_Dialogue>(true); // This line is can find dialogue UI if it is inactive

        skillTreeEnabled = skillTreeUI.gameObject.activeSelf;
        inventoryEnabled = inventoryUI.gameObject.activeSelf;
    }

    private void Start()
    {
        skillTreeUI.UnlockDefaultSkills();
    }

    public void SetUpControlsUI(PlayerInputSet inputSet)
    {
        input = inputSet;

        input.UI.SkillTree.performed += ctx => ToggleSkillTreeUI();
        input.UI.Inventory.performed += ctx => ToggleInventoryUI();
        input.UI.Journal.performed += ctx => ToggleQuestUI();

        input.UI.AlternativeInput.performed += ctx => alternativeInput = true;
        input.UI.AlternativeInput.canceled += ctx => alternativeInput = false;

        input.UI.Options.performed += ctx => 
        {
            foreach (var element in uiElements)
            {
                if(element.activeSelf)
                {
                    // Time.timeScale = 1f; // Resume the game when options menu is closed 
                    SwitchToInGameUI();
                    return;
                }
            }
            
            // Time.timeScale = 0f; // Pause the game when options menu is opened

            OpenOptionsUI();
        };

        input.UI.Dialogue.performed += ctx => 
        {
            if(dialogueUI.gameObject.activeInHierarchy)
            {
                dialogueUI.DialogueInteraction();
            }
        };

        input.UI.DialogueNavigation.performed += ctx => 
        {
            int direction = Mathf.RoundToInt(ctx.ReadValue<float>());

            if(dialogueUI.gameObject.activeInHierarchy)
            {
                dialogueUI.NavigateChoices(direction);
            }
        };
    }

    private void StopPlayerControls(bool stopControls)
    {
        if (stopControls)
        {
            input.Player.Disable();
        }
        else
        {
            input.Player.Enable();
        }
    }

    private void StopPlayerControlsIfNeeded()
    {
        foreach (var element in uiElements)
        {
            if(element.activeSelf)
            {
                StopPlayerControls(true);
                return;
            }
        }
        StopPlayerControls(false);
    }

    public void HideToolTips()
    {
        skillToolTip.ShowToolTip(false, null);
        itemToolTip.ShowToolTip(false, null);
        statToolTip.ShowToolTip(false, null);
    }

    private void SetToolTipsAboveUIElements()
    {
        skillToolTip.transform.SetAsLastSibling();
        itemToolTip.transform.SetAsLastSibling();
        statToolTip.transform.SetAsLastSibling();
    }

    public void ToggleSkillTreeUI()
    {
        skillTreeUI.transform.SetAsLastSibling();

        SetToolTipsAboveUIElements();
        fadeUI.transform.SetAsLastSibling();

        skillTreeEnabled = !skillTreeEnabled;
        skillTreeUI.gameObject.SetActive(skillTreeEnabled);
        HideToolTips();

        StopPlayerControlsIfNeeded();
    }

    public void ToggleInventoryUI()
    {
        inventoryUI.transform.SetAsLastSibling();

        SetToolTipsAboveUIElements();
        fadeUI.transform.SetAsLastSibling();

        inventoryEnabled = !inventoryEnabled;
        inventoryUI.gameObject.SetActive(inventoryEnabled);
        HideToolTips();

        StopPlayerControlsIfNeeded();
    }

    public void ToggleQuestUI()
    {
        activeQuestUI.transform.SetAsLastSibling();

        SetToolTipsAboveUIElements();
        fadeUI.transform.SetAsLastSibling();

        activeQuestUI.gameObject.SetActive(!activeQuestUI.gameObject.activeSelf);
        HideToolTips();

        StopPlayerControlsIfNeeded();
    }

    public void OpenDialogueUI(Dialogue_LineSO firstLine, DialogueNpcData npcData)
    {
        StopPlayerControls(true);
        HideToolTips();

        dialogueUI.gameObject.SetActive(true);
        dialogueUI.SetupNpcData(npcData);
        dialogueUI.PlayDialogueLine(firstLine);
    }

    public void OpenQuestUI(Quest_DataSO[] questsToShow)
    {
        StopPlayerControls(true);
        HideToolTips();

        questUI.gameObject.SetActive(true);
        questUI.SetUpQuestUI(questsToShow);
    }

    public void OpenStorageUI(bool openStorageUI)
    {
        storageUI.gameObject.SetActive(openStorageUI);
        StopPlayerControls(openStorageUI);

        if(!openStorageUI)
        {
            craftUI.gameObject.SetActive(false);
            HideToolTips();
        }
    }

    public void OpenCraftUI(bool openCraftUI)
    {
        craftUI.gameObject.SetActive(openCraftUI);
        StopPlayerControls(openCraftUI);

        if(!openCraftUI)
        {
            storageUI.gameObject.SetActive(false);
            HideToolTips();
        }
    }

    public void OpenMerchantUI(bool openMerchantUI)
    {
        merchantUI.gameObject.SetActive(openMerchantUI);
        StopPlayerControls(openMerchantUI);

        if(!openMerchantUI)
        {
            HideToolTips();
        }
    }

    public void OpenOptionsUI()
    {
        HideToolTips();
        StopPlayerControls(true);
        SwitchTo(optionsUI.gameObject);
    }

    public void CloseOptionsUI()
    {
        HideToolTips();
        SwitchToInGameUI();
        StopPlayerControls(false);
        optionsUI.gameObject.SetActive(false);
    }

    public void SwitchToInGameUI()
    {
        HideToolTips();
        StopPlayerControls(false);
        SwitchTo(inGameUI.gameObject);
        skillTreeEnabled = false;
        inventoryEnabled = false;
    }

    public void OpenDeathScreenUI()
    {
        // HideToolTips();
        // StopPlayerControls(true);
        SwitchTo(deathScreenUI.gameObject);
        input.Disable(); // Pay attention to this line if we import gamepad
    }

    private void SwitchTo(GameObject objectToSwitchOn)
    {
        foreach (var element in uiElements)
        {
            element.gameObject.SetActive(false);
        }

        objectToSwitchOn.SetActive(true);
    }
}

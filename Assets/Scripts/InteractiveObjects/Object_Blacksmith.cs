using UnityEngine;

public class Object_Blacksmith : Object_NPC, IInteractable
{
    [Header("Quest & Dialogue")]
    [SerializeField] private Dialogue_LineSO firstDialogueLine;
    [SerializeField] private Quest_DataSO[] questsToOffer;

    private Animator anim;
    private Inventory_Player playerInventory;
    private Inventory_Storage storage;

    protected override void Awake()
    {
        base.Awake();
        storage = GetComponent<Inventory_Storage>();
        anim = GetComponentInChildren<Animator>();
        anim.SetBool("isBlackSmith", true);
    }

    public override void Interact()
    {
        base.Interact();
        if (!EnsurePlayerInventory()) return;

        ui.storageUI.SetUpStorageUI(storage);
        ui.craftUI.SetUpCraftUI(storage);

        ui.OpenDialogueUI(firstDialogueLine, new DialogueNpcData(npcType, questsToOffer));
    }

    private bool EnsurePlayerInventory()
    {
        if (playerInventory == null)
        {
            Transform playerTransform = player != null ? player : FindFirstObjectByType<Player>()?.transform;
            playerInventory = playerTransform != null ? playerTransform.GetComponent<Inventory_Player>() : null;

            if (playerInventory != null)
                storage.SetInventory(playerInventory);
        }

        return playerInventory != null;
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        playerInventory = player.GetComponent<Inventory_Player>();

        storage.SetInventory(playerInventory);
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);

        if (ui == null) return;

        ui.HideToolTips();

        ui.OpenStorageUI(false);
    }
}

using UnityEngine;

public class Object_Blacksmith : Object_NPC, IInteractable
{
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

    public void Interact()
    {
        if (!EnsurePlayerInventory()) return;

        ui.storageUI.SetUpStorageUI(storage);
        ui.craftUI.SetUpCraftUI(storage);

        ui.storageUI.gameObject.SetActive(true);
        // ui.craftUI.gameObject.SetActive(true);
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

        ui.SwitchOffAllToolTips();

        if (ui.storageUI != null)
            ui.storageUI.gameObject.SetActive(false);

        if (ui.craftUI != null)
            ui.craftUI.gameObject.SetActive(false);
    }
}

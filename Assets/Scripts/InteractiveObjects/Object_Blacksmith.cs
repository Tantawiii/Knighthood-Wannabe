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
        ui.storageUI.SetUpStorage(storage, playerInventory);
        ui.storageUI.gameObject.SetActive(true);
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

        ui.SwitchOffAllToolTips();
        ui.storageUI.gameObject.SetActive(false);
    }
}

using UnityEngine;

public class Object_Merchant : Object_NPC, IInteractable
{
    private Inventory_Player inventory;
    private Inventory_Merchant merchant;

    protected override void Awake()
    {
        base.Awake();
        merchant = GetComponent<Inventory_Merchant>();
    }

    protected override void Update()
    {
        base.Update();

        if(Input.GetKeyDown(KeyCode.Z))
        {
            merchant.FillShopList();
        }
    }

    public void Interact()
    {
        if (!EnsurePlayerInventory()) return;

        ui.merchantUI.SetUpMerchantUI(merchant, inventory);

        ui.merchantUI.gameObject.SetActive(true);
    }

    private bool EnsurePlayerInventory()
    {
        if (inventory == null)
        {
            Transform playerTransform = player != null ? player : FindFirstObjectByType<Player>()?.transform;
            inventory = playerTransform != null ? playerTransform.GetComponent<Inventory_Player>() : null;

            if (inventory != null)
                merchant.SetInventory(inventory);
        }

        return inventory != null;
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        inventory = player.GetComponent<Inventory_Player>();
        merchant.SetInventory(inventory);
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);

        if (ui == null) return;

        ui.SwitchOffAllToolTips();

        ui.merchantUI.gameObject.SetActive(false);
    }
}

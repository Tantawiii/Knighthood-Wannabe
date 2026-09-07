using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    private Player player;
    private Inventory_Player inventory;
    private UI_SkillSlot[] skillSlots;
    
    [SerializeField] private RectTransform healthRect;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [Header("Quick Item Slots")]
    [SerializeField] private float yOffsetQuickItemParent = 150f;
    [SerializeField] private Transform quickItemOptionsParent;
    private UI_QuickItemSlotOption[] quickItemOptions;
    private UI_QuickItemSlot[] quickItemSlots;

    private void Awake()
    {
        skillSlots = GetComponentsInChildren<UI_SkillSlot>(true);
    }

    private void Start()
    {
        quickItemSlots = GetComponentsInChildren<UI_QuickItemSlot>();

        player = FindFirstObjectByType<Player>();
        player.health.OnHealthUpdate += UpdateHealthBar;

        inventory = player.inventory;
        inventory.OnQuickSlotUsed += UpdateQuickSlotsUI;
    }

    public void UpdateQuickSlotsUI(int slotNumber, Inventory_Item itemInSlot)
    {
        quickItemSlots[slotNumber].UpdateQuickSlotUI(itemInSlot);
    }

    public void OpenQuickItemOptions(UI_QuickItemSlot quickItemSlot, RectTransform targetRect)
    {
        if(quickItemOptions == null)
        {
            quickItemOptions = quickItemOptionsParent.GetComponentsInChildren<UI_QuickItemSlotOption>(true);
        }

        List<Inventory_Item> consumables = inventory.itemList.FindAll(item => item.itemData.itemType == ItemType.Consumable);

        for(int i = 0; i < quickItemOptions.Length; i++)
        {
            if(i < consumables.Count)
            {
                quickItemOptions[i].gameObject.SetActive(true);
                quickItemOptions[i].SetupOption(quickItemSlot, consumables[i]);
            }
            else
            {
                quickItemOptions[i].gameObject.SetActive(false);
            }
        }

        quickItemOptionsParent.position = targetRect.position + Vector3.up * yOffsetQuickItemParent;
    }

    public void HideQuickItemOptions() => quickItemOptionsParent.position = new Vector3(9999, 9999);

    public UI_SkillSlot GetSkillSlot(SkillType skillType)
    {
        foreach (var slot in skillSlots)
        {
            if (slot.skillType == skillType)
            {
                slot.gameObject.SetActive(true);
                return slot;
            }
        }
        return null;
    }

    private void UpdateHealthBar()
    {
        float currentHealth = Mathf.Min(Mathf.RoundToInt(player.health.GetCurrentHealthValue()), player.entityStats.GetMaxHealth());
        float maxHealth = player.entityStats.GetMaxHealth();
        float sizeDifference = Mathf.Abs(maxHealth - healthRect.sizeDelta.x);

        if(sizeDifference > 0.1f)
        {
            healthRect.sizeDelta = new Vector2(maxHealth * .2f, healthRect.sizeDelta.y);
        }

        healthText.text = currentHealth + " / " + maxHealth;
        healthSlider.value = player.health.GetHealthPercent();
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CraftSlot : MonoBehaviour
{
    private Item_DataSO itemToCraft;
    [SerializeField] private UI_CraftPreview craftPreview;

    [SerializeField] private Image craftItemIcon;
    [SerializeField] private TextMeshProUGUI cractItemName;


    public void SetUpButton(Item_DataSO craftData)
    {
        itemToCraft = craftData;
        craftItemIcon.sprite = craftData.itemIcon;
        cractItemName.text = craftData.itemName;
    }

    public void UpdateCraftPreview() => craftPreview.UpdateCraftPreview(itemToCraft);
}

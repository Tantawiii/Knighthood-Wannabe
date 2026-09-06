using System.Collections.Generic;
using UnityEngine;

public class UI_EquipSlotParent : MonoBehaviour
{
    private UI_EquipmentSlot[] equipSlots;

    public void UpdateEquipSlots(List<Inventory_EquipmentSlot> equipList)
    {
        if (equipSlots == null)
        {
            equipSlots = GetComponentsInChildren<UI_EquipmentSlot>();
        }

        for(int i = 0; i < equipSlots.Length; i++)
        {
            var playerEquipSlot = equipList[i];

            if (!playerEquipSlot.HasItem())
            {
                equipSlots[i].UpdateSlot(null);
            }
            else
            {
                equipSlots[i].UpdateSlot(playerEquipSlot.equippedItem);
            }
        }
    }
}

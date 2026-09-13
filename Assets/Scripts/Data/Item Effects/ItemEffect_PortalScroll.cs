using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item Effect/Portal Scroll", fileName = "Item effect data - portal scroll")]
public class ItemEffect_PortalScroll : ItemEffect_DataSO
{
    public override void ExecuteEffect()
    {
        if(SceneManager.GetActiveScene().name == "Level_0")
        {
            Debug.Log("You are already in town.");
            return;
        }

        Player player = Player.Instance;
        Vector3 portalPosition = player.transform.position + new Vector3(player.facingDir * 1.5f, 0);

        Object_Portal.Instance.ActivatePortal(portalPosition, player.facingDir);
    }
}

using UnityEngine;

public class Player_Health : Entity_Health
{
    private Player player;
    protected override void Awake()
    {
        base.Awake();

        player = GetComponent<Player>();
    }

    protected override void Die()
    {
        base.Die();

        // GameManager.Instance.SetLastPlayerPosition(transform.position);
        // GameManager.Instance.RestartScene();

        player.ui.OpenDeathScreenUI();
    }
}

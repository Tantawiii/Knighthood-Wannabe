using UnityEngine;

public class Player_Health : Entity_Health
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            Die();
        }
    }

    protected override void Die()
    {
        base.Die();

        GameManager.Instance.SetLastDeathPosition(transform.position);
        // UI
        GameManager.Instance.RestartScene();
    }
}

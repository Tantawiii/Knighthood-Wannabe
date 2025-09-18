using UnityEngine;

public class Enemy_AnimationTriggers : Entity_AnimationTriggers
{
    Enemy enemy;
    Enemy_VFX enemy_VFX;

    protected override void Awake()
    {
        base.Awake();

        enemy = GetComponentInParent<Enemy>();
        enemy_VFX = GetComponentInParent<Enemy_VFX>();
    }

    private void EnableCounterWindow() 
    {
        enemy_VFX.EnableAttackAlert(true);
        enemy.EnableCounterWindow(true);
    }

    private void DisableCounterWindow()
    {
        enemy_VFX.EnableAttackAlert(false);
        enemy.EnableCounterWindow(false);
    }
}

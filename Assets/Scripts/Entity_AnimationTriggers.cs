using UnityEngine;

public class Entity_AnimationTriggers : MonoBehaviour
{
    Entity entity;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }

    private void CurrentStateTrigger()
    {
        entity.CallAnimationTrigger();
    }
}

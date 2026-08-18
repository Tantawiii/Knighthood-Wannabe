using UnityEngine;

public class Object_Blacksmith : Object_NPC, IInteractable
{
    private Animator anim;

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponentInChildren<Animator>();
        anim.SetBool("isBlackSmith", true);
    }

    public void Interact()
    {
        Debug.Log("Interacting with Blacksmith");
    }
}

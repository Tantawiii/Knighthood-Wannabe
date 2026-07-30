using UnityEngine;

public class SkillObject_TimeEcho : SkillObject_Base
{
    private void Update()
    {
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }
}

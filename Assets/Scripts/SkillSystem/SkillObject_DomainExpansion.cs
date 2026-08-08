using UnityEngine;

public class SkillObject_DomainExpansion : SkillObject_Base
{
    private Skill_DomainExpansion domainManager;

    private float maxSize = 10;
    private float expandSpeed = 2;
    private float duration = 5;
    private float slowPercent = 0.9f;


    private Vector3 targetScale;
    private bool isShrinking;

    public void SetUpDomain(Skill_DomainExpansion domainManager)
    {
        this.domainManager = domainManager;

        targetScale = Vector3.one * maxSize;
        Invoke(nameof(ShrinkDomain), duration);
    }

    private void Update()
    {
        HandleScaling();
    }

    private void HandleScaling()
    {
        float sizeDifference = Mathf.Abs(transform.localScale.x - targetScale.x);

        bool shouldChangeScale = sizeDifference > 0.1f;

        if(shouldChangeScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, expandSpeed * Time.deltaTime);
        }

        if (isShrinking && sizeDifference <= 0.1f)
        {
            Destroy(gameObject);
        }
    }

    private void ShrinkDomain()
    {
        targetScale = Vector3.zero;
        isShrinking = true;
    }
}

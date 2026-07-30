using UnityEngine;

public class Skill_TimeEcho : Skill_Base
{
    [SerializeField] private GameObject timeEchoPrefab;
    [SerializeField] private float timeEchoDuration;

    public float GetTimeEchoDuration() => timeEchoDuration;

    public override void TryUseSkill()
    {
        if(!CanUseSkill())
        {
            return;
        }

        CreateTimeEcho();
    }

    public void CreateTimeEcho()
    {
        GameObject timeEchoInstance = Instantiate(timeEchoPrefab, transform.position, Quaternion.identity);
        timeEchoInstance.GetComponent<SkillObject_TimeEcho>().SetUpEcho(this);
    }
}
using UnityEngine;

public class Skill_TimeEcho : Skill_Base
{
    [SerializeField] private GameObject timeEchoPrefab;
    [SerializeField] private float timeEchoDuration;

    public void CreateTimeEcho()
    {
        GameObject timeEchoInstance = Instantiate(timeEchoPrefab, transform.position, Quaternion.identity);
    }
}
using UnityEngine;

public class UI_SkillTree : MonoBehaviour
{
    [SerializeField] private int skillPoints;
    [SerializeField] private UI_TreeConnectHandler[] parentNodes;
    private UI_TreeNode[] allTreeNodes; 
    public Player_SkillManager skillManager { get; private set; }

    public bool EnoughSkillPoints(int cost) => skillPoints >= cost;

    public void RemoveSkillPoints(int cost) => skillPoints -= cost;
    public void AddSkillPoints(int amount) => skillPoints += amount;

    private void Start()
    {
        UpdateAllConnections();
    }

    public void UnlockDefaultSkills()
    {
        allTreeNodes = GetComponentsInChildren<UI_TreeNode>(true);
        skillManager = FindFirstObjectByType<Player_SkillManager>();

        foreach (var node in allTreeNodes)
        {
            node.UnlockDefaultSkill();
        }
    }

    [ContextMenu("Reset Skill Tree")]
    public void ResetSkillTree()
    {
        UI_TreeNode[] allNodes = GetComponentsInChildren<UI_TreeNode>();

        foreach (var node in allNodes)
        {
            node.Refund();
        }
    }

    [ContextMenu("Update All Connections")]
    public void UpdateAllConnections()
    {
        foreach (var node in parentNodes)
        {
            node.UpdateAllConnections();
        }
    }
}

using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Quest Data/Quest Database", fileName = "Quest Database")]
public class QuestDatabase_DataSO : ScriptableObject
{
    public Quest_DataSO[] allQuests;

    public Quest_DataSO GetQuestDataByID(string id)
    {
        return allQuests.FirstOrDefault(quest => quest.questSaveID == id && quest != null);
    }

    #if UNITY_EDITOR
    [ContextMenu("Auto-Fill Quests Data")]
    public void CollectQuestsData()
    {
        string[] guids = AssetDatabase.FindAssets("t:Quest_DataSO");
        allQuests = guids.Select(guid => AssetDatabase.LoadAssetAtPath<Quest_DataSO>(AssetDatabase.GUIDToAssetPath(guid))).Where(quest => quest != null).ToArray();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
    #endif
}

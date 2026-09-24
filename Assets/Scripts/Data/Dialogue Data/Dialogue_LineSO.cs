using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Dialogue Data/New Line Data", fileName = "Line - ")]
public class Dialogue_LineSO : ScriptableObject
{
    [Header("Dialogue Info")]
    public string dialogueGroupName;
    public Dialogue_SpeakerSO speaker;

    [Header("Dialogue Text")]
    [TextArea] public string[] textLine;

    [Header("Dialogue Action")]
    [TextArea] public string actionLine;
    public DialogueActionType actionType;
    public Dialogue_LineSO[] choiceLines; 

    public string GetRandomLine()
    {
        return textLine[Random.Range(0, textLine.Length)];
    }
}

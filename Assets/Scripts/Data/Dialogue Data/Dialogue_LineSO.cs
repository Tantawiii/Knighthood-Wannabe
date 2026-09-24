using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Dialogue Data/New Line Data", fileName = "Line - ")]
public class Dialogue_LineSO : ScriptableObject
{
    [Header("Dialogue Info")]
    public string dialogueGroupName;
    public Dialogue_SpeakerSO speaker;

    [Header("Dialogue Text")]
    [TextArea] public string[] textLine;

    [Header("Answer Setup")]
    public bool playerCanAnswer; // If true, the player can answer this line with a response
    public Dialogue_LineSO[] playerResponses; // The lines that the player can respond with


    public string GetRandomLine()
    {
        return textLine[Random.Range(0, textLine.Length)];
    }
}

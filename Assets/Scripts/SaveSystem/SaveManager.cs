using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private FileDataHandler dataHandler;
    private GameData gameData;
    private List<ISaveable> allSaveables;
    [SerializeField] private string fileName = "Knighthood Wannabe.json";
    [SerializeField] private bool encryptData = true;

    private void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        Debug.Log(Application.persistentDataPath);
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData );
        allSaveables = FindISaveables();

        yield return null; // Wait for one frame to ensure all saveables are initialized before loading data
        LoadGame();
    }

    public GameData GetGameData() => gameData;
    public void SaveGame()
    {
        foreach (var saveable in allSaveables)
        {
            saveable.SaveData(ref gameData);
        }

        dataHandler.SaveData(gameData);
    }

    public void LoadGame()
    {
        allSaveables ??= FindISaveables();

        gameData = dataHandler.LoadData();

        if (gameData == null)
        {
            Debug.Log("No data was found. A new game will be started.");
            gameData = new GameData();
        }

        foreach (var saveable in allSaveables)
        {
            saveable.LoadData(gameData);
        }
    }

    [ContextMenu("Delete Save Data")]
    public void DeleteSaveData()
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        dataHandler.DeleteData();

        LoadGame(); // Load the game after deleting the save data to reset the game state
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private List<ISaveable> FindISaveables()
    {
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ISaveable>().ToList();
    }
}

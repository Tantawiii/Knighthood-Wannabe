using System;
using System.IO;
using UnityEngine;

public class FileDataHandler
{
    private string fullPath;
    private bool encrtyptData;
    private string encryptionCodeWord = "UATAIAM"; 

    public FileDataHandler(string dataDirPath, string dataFileName, bool encrtyptData)
    {
        this.fullPath = Path.Combine(dataDirPath, dataFileName);
        this.encrtyptData = encrtyptData;
    }
    
    // Save data to a file
    public void SaveData(GameData gameData)
    {
        try
        {
            // 1. Create the directory where the file will be saved if it doesn't already exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // 2. Serialize the C# game data object into a JSON string
            string dataToSave = JsonUtility.ToJson(gameData, true);

            // 2.5. Encrypt the data if encryption is enabled
            if (encrtyptData)
            {
                dataToSave = EncryptDecrypt(dataToSave);
            }

            // 3. Write the serialized data to a file
            using(FileStream stream = new FileStream(fullPath, FileMode.Create))
            {

                // 4. Write the serialized data to the file
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToSave);
                }
            }
        }
        catch (Exception e)
        {
            // Log any errors that might occur during the save process
            Debug.LogError("Error occured when trying to save data to file: " + fullPath + "\n" + e);
        }
    }


    // Load data from a file and return a GameData object
    public GameData LoadData()
    {
        GameData loadData = null;

        // 1. Check if the file exists
        if (File.Exists(fullPath))
        {
            try
            {   
                string dataToLoad = "";
                // 2. Open the file and read the serialized data from it
                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    // 3. Read the serialized data from the file
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                // 3.5. Decrypt the data if encryption is enabled
                if (encrtyptData)
                {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }

                // 4. Deserialize the JSON string back into a C# game data object
                loadData = JsonUtility.FromJson<GameData>(dataToLoad);
            }
            catch (Exception e)
            {
                Debug.LogError("Error occurred when trying to load data from file: " + fullPath + "\n" + e);
            }
        }

        return loadData;
    }

    public void DeleteData()
    {
        try
        {
            // 1. Check if the file exists before attempting to delete it
            if (File.Exists(fullPath))
            {
                // 2. Delete the file
                File.Delete(fullPath);
                Debug.Log("Data file deleted successfully: " + fullPath);
            }
            else
            {
                Debug.LogWarning("Data file not found for deletion: " + fullPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error occurred when trying to delete data file: " + fullPath + "\n" + e);
        }
    }

    private string EncryptDecrypt(string data)
    {
        string modifiedData = "";
        for (int i = 0; i < data.Length; i++)
        {
            modifiedData += (char)(data[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);
        }
        return modifiedData;
    }
}

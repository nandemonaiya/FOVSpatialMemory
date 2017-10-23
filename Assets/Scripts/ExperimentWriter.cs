using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using System.Collections.Generic;

using System;
using System.IO;

#if !UNITY_EDITOR && UNITY_METRO
using System.Threading.Tasks;
using Windows.Storage;
#endif

public class ExperimentWriter : MonoBehaviour
{
    private string m_dataFolderPath;
    private string m_dataFileName = "SpatialMemoryData.csv";

    private string m_experimentData;
    public string ExperimentData
    {
        get
        {
            return m_experimentData;
        }
    }

    public void SaveExperimentData(string data)
    {
#if !UNITY_EDITOR && UNITY_METRO
        Task<Task> task = Task<Task>.Factory.StartNew(
            async () =>
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(m_dataFolderPath);
                StorageFile file = await folder.CreateFileAsync(m_dataFileName, CreationCollisionOption.OpenIfExists);

                List<string> temp = new List<string>();
                temp.Add(data);

                await FileIO.AppendLinesAsync(file, temp);
            }
        );

        task.Wait();
        task.Result.Wait();
#else
        Stream stream = new FileStream(Path.Combine(m_dataFolderPath, m_dataFileName), FileMode.Append, FileAccess.Write);
        using (StreamWriter streamWriter = new StreamWriter(stream))
        {
            streamWriter.WriteLine(data);
        }

        stream.Dispose();
#endif

        Debug.Log("Saved data: " + data);
    }


    private void Start()
    {
#if !UNITY_EDITOR && UNITY_METRO
        m_dataFolderPath = ApplicationData.Current.RoamingFolder.Path;
#else
        m_dataFolderPath = Application.persistentDataPath;
#endif
        Debug.Log("Data folder path: " + m_dataFolderPath);

        DontDestroyOnLoad(gameObject);
    }
}

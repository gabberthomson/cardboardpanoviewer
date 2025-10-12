using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class FolderPickerReceiver : MonoBehaviour
{
    public static FolderPickerReceiver Instance;

    private string SelectedUri;
    private string uriFilePath;

    void Awake()
    {
        UnityEngine.Debug.Log("FolderPicker awaken");
        if (Instance == null) Instance = this;
        uriFilePath = Path.Combine(Application.persistentDataPath, "saf_uri.txt");
    }

    /// <summary>
    /// Ritorna true se l'URI è già disponibile (caricato da file).
    /// Se non lo è, apre il picker e ritorna false.
    /// </summary>
    public bool LoadOrRequestUri()
    {
        UnityEngine.Debug.Log("LoadOrRequestURI()");
        if (File.Exists(uriFilePath))
        {
            SelectedUri = File.ReadAllText(uriFilePath);
            UnityEngine.Debug.Log("[SAF] Uri salvato trovato: " + SelectedUri);
            return true;
        }
        else
        {
            OpenFolderPicker();
            return false;
        }
    }


    public void OpenFolderPicker()
    {
        UnityEngine.Debug.Log("OpenFolderPicker()");
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            UnityEngine.Debug.Log("using java");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using (var jc = new AndroidJavaClass("com.cardboardpanoviewer.saplugin.SAFManager"))
            {
                UnityEngine.Debug.Log("before java method");
                jc.CallStatic("openFolderPicker", activity, uriFilePath);
                UnityEngine.Debug.Log("after java method");
            }
        }
    }

    /// <summary>
    /// Elenco sincrono dei file .jpg/.jpeg nella cartella selezionata (senza estensione).
    /// </summary>
    public List<string> ListFilesSync()
    {
        if (string.IsNullOrEmpty(SelectedUri))
            throw new Exception("[SAF] Nessun URI selezionato.");

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using (var jc = new AndroidJavaClass("com.cardboardpanoviewer.saplugin.SAFManager"))
            {
                string joined = jc.CallStatic<string>("listFilesInDirectorySync", activity, SelectedUri);
                var list = new List<string>();
                if (!string.IsNullOrEmpty(joined))
                {
                    foreach (var n in joined.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (n.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            n.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(Path.GetFileNameWithoutExtension(n));
                        }
                    }
                }
                return list;
            }
        }
    }


    public string CopyFileToCache(string fileName)
    {
        if (string.IsNullOrEmpty(SelectedUri))
            throw new Exception("[SAF] Nessun URI selezionato.");

        // Dove vuoi il file copiato (cache dell’app)
        string destDir = Path.Combine(Application.temporaryCachePath, "safcache");
        Directory.CreateDirectory(destDir);
        string destPath = Path.Combine(destDir, fileName);

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using (var jc = new AndroidJavaClass("com.cardboardpanoviewer.saplugin.SAFManager"))
            {
                string res = jc.CallStatic<string>("copyFileToPath", activity, SelectedUri, fileName, destPath);
                if (string.IsNullOrEmpty(res) || !res.StartsWith("OK"))
                    throw new Exception("[SAF] copyFileToPath failed: " + res);
            }
        }
        return destPath; // ora è un vero path leggibile dall’app
    }

    /// <summary>
    /// Lettura sincrona: ritorna i byte del file richiesto.
    /// </summary>
    public byte[] ReadFileAsBytes(string fileName)
    {
        string fullPath = CopyFileToCache(fileName);
        return File.ReadAllBytes(fullPath);
    }

    public string GetSelectedUri() => SelectedUri;
}

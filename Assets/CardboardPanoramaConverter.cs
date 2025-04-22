using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.Android;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;
using TMPro;
using System.Collections.Specialized;

public class CardboardPanoramaConverter : MonoBehaviour
{
    public Renderer panoramaRenderer; // Renderer per mostrare il panorama
    private AudioPlayer audioPlayer;
    public bool leftEyeOnly;
    private Texture2D currentTexture;
    public Transform sphereTransform; // La sfera con la texture panoramica
    public Transform cameraRig;       // XR Origin o la camera principale
    public string BasePanoPath;
    public string ConvertedPanoPath;
    private Texture defaultTexture;
    public TextMeshProUGUI menuText;
    public Canvas helpCanvas;

    void setBanoPaths()
    {
        try
        {
            using (AndroidJavaClass environmentClass = new AndroidJavaClass("android.os.Environment"))
            using (AndroidJavaObject externalStorageDirectory = environmentClass.CallStatic<AndroidJavaObject>("getExternalStorageDirectory"))
            {
                BasePanoPath = externalStorageDirectory.Call<string>("getAbsolutePath");
                BasePanoPath = Path.Combine(BasePanoPath, "Pano");
                if (!Directory.Exists(BasePanoPath))
                    Directory.CreateDirectory(BasePanoPath);
                ConvertedPanoPath = Path.Combine(Application.persistentDataPath, "Converted");
                if (!Directory.Exists(ConvertedPanoPath))
                    Directory.CreateDirectory(ConvertedPanoPath);
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Errore nell'API Android: " + e.Message);
        }
    }

    IEnumerator WaitUntilCameraReady()
    {
        while (Camera.main == null || Camera.main.transform.position == Vector3.zero)
        {
            yield return null; // aspetta il prossimo frame
        }
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : GameObject.Find("CenterEyeAnchor").transform;
        if (cameraTransform != null)
        {
            menuText.transform.position = cameraTransform.position + cameraTransform.forward * 4f; // 2 metri davanti all'utente
            menuText.transform.rotation = Quaternion.LookRotation(cameraTransform.forward); // Ruota il menu per guardare l'utente
        }
    }


        void Start()
    {
        // Verifica e richiedi permessi
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        setBanoPaths();
        defaultTexture = panoramaRenderer.material.mainTexture;
        leftEyeOnly = false;
        audioPlayer = new AudioPlayer();
        StartCoroutine(WaitUntilCameraReady());
    }

    // Funzione per caricare una panoramica
    public void LoadPanorama(string item)
    {
        audioPlayer.StopAudio();
        // Step 1: Rilascia la texture corrente prima di caricarne una nuova
        ReleaseCurrentTexture();

        // Step 2: Assicurati che il renderer non punti a nulla durante il caricamento
        panoramaRenderer.material.mainTexture = null;
        bool meta = true;
        string audioPath = "";
        // Step 3: Carica l'immagine
        try
        {
            
            string imageName = item+ ".jpg";
            Texture2D newTexture;

            // First we check if it's already converted
            if (File.Exists(Path.Combine(ConvertedPanoPath, imageName))) {
                byte[] fileData = File.ReadAllBytes(Path.Combine(ConvertedPanoPath, imageName));
                meta = false;
                newTexture = new Texture2D(2, 2);
                newTexture.LoadImage(fileData);
                string audioName = Path.ChangeExtension(imageName, ".tmp");
                if (File.Exists(Path.Combine(ConvertedPanoPath, audioName)))
                    audioPath = Path.Combine(ConvertedPanoPath, audioName);
            }
            else {
                (newTexture, audioPath) = ConvertCardboardJpg(item);
            }

            if (newTexture != null)
            {
                // Step 4: Assegna la nuova texture
                currentTexture = newTexture;
                panoramaRenderer.material.mainTexture = currentTexture;

                // Step 4bis: ruota la sfera
                if (sphereTransform != null && cameraRig != null)
                {
                    Vector3 cameraForward = cameraRig.forward;
                    cameraForward.y = 0f; // Ignora altezza

                    // Calcola la rotazione verso la camera
                    Quaternion lookAtCamera = Quaternion.LookRotation(cameraForward);

                    // Inverti la rotazione per ruotare la sfera al contrario (immagine ruotata verso la camera)
                    sphereTransform.rotation = Quaternion.Inverse(lookAtCamera);
                }

                UnityEngine.Debug.Log($"Panorama caricato: {item}, " +
                    $"Dimensioni: {currentTexture.width}x{currentTexture.height}, " +
                    $"Memoria utilizzata: {Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MB");
            }
            else
            {
                panoramaRenderer.material.mainTexture = defaultTexture;
                menuText.text = "Not a panorama";
                menuText.alignment = TextAlignmentOptions.Center;
                Transform cameraTransform = Camera.main != null ? Camera.main.transform : GameObject.Find("CenterEyeAnchor").transform;
                if (cameraTransform != null)
                {
                    menuText.transform.position = cameraTransform.position + cameraTransform.forward * 4f + Vector3. down * 0.5f; // 2 metri davanti all'utente
                    menuText.transform.rotation = Quaternion.LookRotation(cameraTransform.forward); // Ruota il menu per guardare l'utente
                }
                helpCanvas.enabled = true;
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Errore nel caricamento dell'immagine: {e.Message}");
        }

        // Step 5: Gestione dell'audio
        if (audioPath != "")
        {
            if (File.Exists(audioPath))
            {
                audioPlayer.PlayAudio(audioPath);
            }
        }


        // Step 6: Forza la garbage collection
        ForceGarbageCollection();
    }

    // Metodo dedicato al rilascio delle risorse della texture corrente
    private void ReleaseCurrentTexture()
    {
        if (currentTexture != null)
        {
            UnityEngine.Debug.Log("Rilascio texture precedente...");

            // Rimuovi riferimento dal renderer
            if (panoramaRenderer.material.mainTexture == currentTexture)
            {
                panoramaRenderer.material.mainTexture = null;
            }

            // Destroy della texture
            Destroy(currentTexture);
            currentTexture = null;
        }
    }

    // Metodo per forzare la garbage collection
    private void ForceGarbageCollection()
    {
        // Forza la pulizia della memoria
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
    }


    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            UnityEngine.Debug.Log("L'utente ha lasciato l'app: metto in pausa l'audio.");
            audioPlayer.PauseAudio();
        }
        else
        {
            UnityEngine.Debug.Log("L'utente � tornato nell'app: riprendo l'audio.");
            audioPlayer.ResumeAudio();
        }
    }

    void OnDestroy()
    {
        // Rilascia la texture prima di distruggere l'oggetto
        ReleaseCurrentTexture();

        // Rilascia il player audio
        if (audioPlayer != null)
        {
            audioPlayer.Dispose();
            audioPlayer = null;
        }

        // Forza una pulizia finale
        ForceGarbageCollection();
    }

    public (Texture2D, string) ConvertCardboardJpg(string item)
    {
        string fileName = item + ".jpg";
        string path = Path.Combine(BasePanoPath, fileName);
        byte[] bytes = File.ReadAllBytes(path);
        List<JpegSection> sections = ParseJpeg(bytes);
        StringBuilder xml = new StringBuilder();
        bool visitedExtended = false;

        foreach (var section in sections)
        {
            bool isXmp = StartsWith(section.Data, "http://ns.adobe.com/xap/1.0/");
            bool isExt = StartsWith(section.Data, "http://ns.adobe.com/xmp/extension/");

            if (isXmp)
            {
                string str = Encoding.UTF8.GetString(section.Data);
                var match = Regex.Match(str, "<x:xmpmeta([\\s\\S]*?)</x:xmpmeta>");
                if (match.Success)
                    xml.Append(match.Value);
            }
            else if (isExt)
            {
                int len = 71 + (visitedExtended ? 4 : 0);
                visitedExtended = true;
                xml.Append(Encoding.UTF8.GetString(section.Data, len, section.Data.Length - len));
            }
        }

        var xmpBlocks = Regex.Matches(xml.ToString(), "<x:xmpmeta[\\s\\S]*?</x:xmpmeta>");
        string gImageBase64 = null;
        string gAudioBase64 = null;
        Dictionary<string, int> gPano = new Dictionary<string, int>();

        foreach (Match block in xmpBlocks)
        {
            XmlDocument doc = new XmlDocument();
            try { doc.LoadXml("<xml>" + SanitizeXml(block.Value) + "</xml>"); }
            catch { continue; }

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

            var descriptions = doc.SelectNodes("//rdf:Description", nsmgr);
            foreach (XmlNode node in descriptions)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    if (attr.Name == "GImage:Data")
                        gImageBase64 = attr.Value;
                    else if (attr.Name == "GAudio:Data")
                        gAudioBase64 = attr.Value;
                    else if (attr.Name.StartsWith("GPano:"))
                    {
                        string key = attr.Name.Substring(6);
                        if (int.TryParse(attr.Value, out int val))
                            gPano[key] = val;
                    }
                }
            }
        }
        UnityEngine.Debug.Log($"Pano count: {gPano.Count}");
        if (gPano.Count > 0)
        {
            string audioFileName = "";
            if (!string.IsNullOrEmpty(gAudioBase64))
            {
                try
                {
                    byte[] audioBytes = Convert.FromBase64String(PadBase64(gAudioBase64));
                    audioFileName = item +  ".tmp";
                    audioFileName = Path.Combine(ConvertedPanoPath, audioFileName);
                    UnityEngine.Debug.Log($"audiopath: {audioFileName}");
                    File.WriteAllBytes(audioFileName, audioBytes);
                    UnityEngine.Debug.Log("Audio saved");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log("Audio error: " + ex.Message);
                }
            }
            else
            {
                UnityEngine.Debug.Log("No Audio");
                audioFileName = "";
            }

            if (string.IsNullOrEmpty(gImageBase64))
            {
                UnityEngine.Debug.Log("No GImage:Data found.");
                return (null, "");
            }

            byte[] rightImageBytes = Convert.FromBase64String(PadBase64(gImageBase64));
            Texture2D rightTex = new Texture2D(2, 2);
            rightTex.LoadImage(rightImageBytes);

            Texture2D leftTex = new Texture2D(2, 2);
            leftTex.LoadImage(bytes);

            Texture2D final = BuildEquirectTexture(leftTex, rightTex, gPano);

            // UnityEngine.Debug.Log("Equirect image created: EquirectOutput.jpg");
            byte[] output = final.EncodeToJPG();
            UnityEngine.Debug.Log($"imagepath: {Path.Combine(ConvertedPanoPath, fileName)}");
            File.WriteAllBytes(Path.Combine(ConvertedPanoPath, fileName), output);
            return (final, audioFileName);

        }
        else
            return (null, "");
    }

    public Texture2D BuildEquirectTexture(Texture2D left, Texture2D right, Dictionary<string, int> pano)
    {
        int fullWidth = pano.GetValueOrDefault("FullPanoWidthPixels", left.width);
        int cropLeft = pano.GetValueOrDefault("CroppedAreaLeftPixels", 0);
        int cropTop = pano.GetValueOrDefault("CroppedAreaTopPixels", 0);
        int cropWidth = pano.GetValueOrDefault("CroppedAreaImageWidthPixels", left.width);
        UnityEngine.Debug.Log($"fullWidth: {fullWidth}");
        UnityEngine.Debug.Log($"cropTop: {cropTop}");
        UnityEngine.Debug.Log($"cropLeft: {cropLeft}");
        UnityEngine.Debug.Log($"cropWidth: {cropWidth}");

        int targetSize = fullWidth;
        float ratio = targetSize / (float)fullWidth;
        float scaleWidth = (cropWidth != fullWidth) ? cropWidth / (float)fullWidth : 1f;
        float imageWidth = targetSize * scaleWidth;
        float imageHeight = left.height * ratio;
        float offsetX = (targetSize - imageWidth) / 2f;
        float x = cropLeft * ratio + offsetX;
        // float y = cropTop * ratio;

        float y = targetSize - imageHeight - cropTop * ratio;
        float y2 = targetSize / 2 - imageHeight - cropTop * ratio;

        UnityEngine.Debug.Log($"x: {x}");
        UnityEngine.Debug.Log($"y: {y}");
        UnityEngine.Debug.Log($"y: {y2}");
        Texture2D canvas = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
        Color avg = GetAverageColor(left);
        Color[] fill = new Color[targetSize * targetSize];
        for (int i = 0; i < fill.Length; i++) fill[i] = avg;
        canvas.SetPixels(fill);

        CopyTextureRegion(left, canvas, x, y, imageWidth, imageHeight, 0);
        if (right != null)
            CopyTextureRegion(right, canvas, x, y2, imageWidth, imageHeight, 0);

        canvas.Apply();
        return canvas;
    }

    void CopyTextureRegion(Texture2D src, Texture2D dst, float dx, float dy, float dw, float dh, int mip)
    {
        Color[] srcPixels = src.GetPixels();
        int srcW = src.width;
        int srcH = src.height;
        int x0 = Mathf.RoundToInt(dx);
        int y0 = Mathf.RoundToInt(dy);
        int w = Mathf.RoundToInt(dw);
        int h = Mathf.RoundToInt(dh);

        for (int y = 0; y < h; y++)
        {
            int sy = y * srcH / h;
            for (int x = 0; x < w; x++)
            {
                int sx = x * srcW / w;
                Color c = srcPixels[sy * srcW + sx];
                dst.SetPixel(x0 + x, y0 + y, c);
            }
        }
    }

    Color GetAverageColor(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        long r = 0, g = 0, b = 0;
        int count = 0;
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                Color c = pixels[y * tex.width + x];
                r += (int)(c.r * 255);
                g += (int)(c.g * 255);
                b += (int)(c.b * 255);
                count++;
            }
        }
        return new Color(r / 255f / count, g / 255f / count, b / 255f / count);
    }

    string PadBase64(string base64)
    {
        int mod = base64.Length % 4;
        return mod == 0 ? base64 : base64.PadRight(base64.Length + (4 - mod), '=');
    }

    string SanitizeXml(string input)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            if (c >= 32 || c == 0x9 || c == 0xA || c == 0xD)
                sb.Append(c);
        }
        return sb.ToString();
    }

    class JpegSection
    {
        public byte Marker;
        public int Length;
        public byte[] Data;
    }

    List<JpegSection> ParseJpeg(byte[] bytes)
    {
        List<JpegSection> sections = new List<JpegSection>();
        int i = 0;
        if (bytes[i++] != 0xFF || bytes[i++] != 0xD8) return null;

        while (i < bytes.Length)
        {
            if (bytes[i++] != 0xFF) return null;
            while (i < bytes.Length && bytes[i] == 0xFF) i++;
            if (i >= bytes.Length) break;

            byte marker = bytes[i++];
            if (marker == 0xDA) break;
            int length = (bytes[i] << 8) | bytes[i + 1];
            i += 2;
            if (marker == 0xE1)
            {
                byte[] data = new byte[length - 2];
                Array.Copy(bytes, i, data, 0, length - 2);
                sections.Add(new JpegSection { Marker = marker, Length = length, Data = data });
            }
            i += length - 2;
        }
        return sections;
    }

    bool StartsWith(byte[] data, string sig)
    {
        var sigBytes = Encoding.ASCII.GetBytes(sig);
        if (data.Length < sigBytes.Length) return false;
        for (int i = 0; i < sigBytes.Length; i++)
            if (data[i] != sigBytes[i]) return false;
        return true;
    }

}


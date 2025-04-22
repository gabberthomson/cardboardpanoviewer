using UnityEngine;
using System.IO;
using System.Collections.Generic;
using TMPro;
using System.Diagnostics;
using SimpleFileBrowser;
using Meta.XR;
using System.Collections.Specialized;

// Corretto per i componenti OVR

public class FileSelector : MonoBehaviour
{
    public Transform menuContainer; // GameObject vuoto per i pulsanti
    public GameObject buttonPrefab; // Prefab del testo 3D per i file
    public CardboardPanoramaConverter panoramaConverter; // Riferimento allo script per caricare il file
    public GameObject menuPanel; // Il pannello del menu (per nasconderlo e mostrarlo)

    // Componenti per il raggio di selezione
    public OVRRaycaster raycaster;
    public LineRenderer rayRenderer;
    public float rayMaxLength = 10f;
    public Color rayColor = Color.blue;
    private GameObject _lastHoveredObject = null;


    private List<string> fileList = new List<string>();
    private Transform rayOrigin;

    private int currentPage = 0;
    private int filesPerPage = 7;
    private int totalPages = 0;

    void Start()
    {
        PositionMenuInFrontOfUser();
        //ShowFileBrowser(); // Mostra il menu all'avvio
        InitializeRayInteraction();

        // Debug per verificare l'inizializzazione
        UnityEngine.Debug.Log("FileSelector initialized");
    }

    //Not used
    public void ShowFileBrowser()
    {
        if (FindObjectOfType<FileBrowser>() == null)
        {
            UnityEngine.Debug.LogError("ERRORE: Nessun FileBrowser trovato nella scena! Assicurati di aver creato il GameObject.");
            return;
        }

        if (panoramaConverter == null)
        {
            UnityEngine.Debug.LogError("ERRORE: panoramaConverter non è stato assegnato in FileSelector!");
            return;
        }

        if (FileBrowser.CheckPermission() != FileBrowser.Permission.Granted)

        {
            FileBrowser.RequestPermission(); // Richiede il permesso per accedere ai file
        }



        // Abilita la UI del file browser
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg"));
        FileBrowser.SetDefaultFilter(".jpg");
        UnityEngine.Debug.Log("Menu avvio");
        // Mostra il file browser
        string initialPath = "/storage/emulated/0/Pano/";
        FileBrowser.ShowLoadDialog((paths) =>
        {
            if (paths.Length > 0)
            {
                string selectedFile = paths[0]; // Prende il primo file selezionato
                string selectedAudio = selectedFile.Replace(".jpg", ".m4a"); // Cerca il file audio con lo stesso nome

                UnityEngine.Debug.Log("File selezionato: " + selectedFile);
                selectedFile = Path.GetFileNameWithoutExtension(selectedFile);
                panoramaConverter.LoadPanorama(selectedFile); // Carica il panorama e l'audio
            }
        },
        () => { UnityEngine.Debug.Log("File selection cancelled"); },  // Annullato
        FileBrowser.PickMode.Files, false, initialPath, null, "Select a file", "Load");
    }


    void InitializeRayInteraction()
    {
        // Configura il punto di origine del raggio (solitamente il controller)
        GameObject rightHandAnchor = GameObject.Find("RightHandAnchor");
        if (rightHandAnchor != null)
        {
            rayOrigin = rightHandAnchor.transform;
            UnityEngine.Debug.Log("Ray origin set to RightHandAnchor");
        }
        else
        {
            UnityEngine.Debug.LogError("RightHandAnchor not found!");
        }

        // Configura il LineRenderer per visualizzare il raggio
        if (rayRenderer == null)
        {
            rayRenderer = gameObject.AddComponent<LineRenderer>();
            rayRenderer.startWidth = 0.005f;
            rayRenderer.endWidth = 0.001f;
            rayRenderer.material = new Material(Shader.Find("Sprites/Default"));
            rayRenderer.startColor = rayColor;
            rayRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.5f);
            rayRenderer.positionCount = 2;
        }
    }

    void Update()
    {
        // Assicurati che il rayOrigin sia valido
        if (rayOrigin == null)
        {
            UnityEngine.Debug.LogError("rayOrigin is null! Reinitializing ray interaction...");
            InitializeRayInteraction();
            return;
        }

        // Aggiorna la posizione del raggio
        UpdateRayVisualization();

        // Aggiungi debug per verificare che il raggio sia visibile
        UnityEngine.Debug.DrawRay(rayOrigin.position, rayOrigin.forward * rayMaxLength, Color.red);

        // Controlla l'hover per il feedback visivo
        CheckRayHover();

        // Verifica input solo per il controller destro
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            HandleRaycastSelection();
        }
    }


    void UpdateRayVisualization()
    {
        if (rayRenderer != null && rayOrigin != null)
        {
            rayRenderer.SetPosition(0, rayOrigin.position);
            rayRenderer.SetPosition(1, rayOrigin.position + rayOrigin.forward * rayMaxLength);
        }
    }

    void HandleRaycastSelection()
    {
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, rayMaxLength))
        {
            // Aggiungi debug per verificare se il raycast funziona
            UnityEngine.Debug.Log("Raycast hit: " + hit.collider.gameObject.name);

            // Verifica se il raggio ha colpito un pulsante
            ButtonTrigger buttonTrigger = hit.collider.GetComponent<ButtonTrigger>();
            if (buttonTrigger != null)
            {
                UnityEngine.Debug.Log("Button trigger found, executing click");
                buttonTrigger.OnClick();
            }
            else
            {
                UnityEngine.Debug.Log("No ButtonTrigger component found on hit object");
            }
        }
        else
        {
            UnityEngine.Debug.Log("Raycast did not hit anything");
        }
    }

    void CheckRayHover()
    {
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, rayMaxLength))
        {
            GameObject hitObject = hit.collider.gameObject;
            ButtonTrigger buttonTrigger = hitObject.GetComponent<ButtonTrigger>();

            // Se abbiamo colpito un nuovo oggetto
            if (buttonTrigger != null && (_lastHoveredObject == null || _lastHoveredObject != hitObject))
            {
                // Se abbiamo un oggetto precedente, disattiva l'hover
                if (_lastHoveredObject != null)
                {
                    ButtonTrigger lastTrigger = _lastHoveredObject.GetComponent<ButtonTrigger>();
                    if (lastTrigger != null)
                    {
                        lastTrigger.OnHoverExit();
                    }
                }

                // Attiva l'hover sul nuovo oggetto
                buttonTrigger.OnHoverEnter();
                _lastHoveredObject = hitObject;

                // Debug per verificare che il raycast funzioni
                UnityEngine.Debug.Log("Hovering over: " + hitObject.name);
            }
        }
        // Se non colpiamo nulla ma avevamo un oggetto precedente
        else if (_lastHoveredObject != null)
        {
            ButtonTrigger lastTrigger = _lastHoveredObject.GetComponent<ButtonTrigger>();
            if (lastTrigger != null)
            {
                lastTrigger.OnHoverExit();
            }
            _lastHoveredObject = null;
        }
    }




    void PositionMenuInFrontOfUser()
    {
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : GameObject.Find("CenterEyeAnchor").transform;
        if (cameraTransform != null)
        {
            menuPanel.transform.position = cameraTransform.position + cameraTransform.forward * 1.5f + Vector3.up * 0.5f;
            menuPanel.transform.rotation = Quaternion.LookRotation(cameraTransform.forward); // Ruota il menu per guardare l'utente
        }
    }

    public void ShowMenu()
    {
        PositionMenuInFrontOfUser();
        menuPanel.SetActive(true); // Attiva il menu
        LoadFiles(); // Carica i file quando il menu è visibile
    }

    public void LoadFiles()
    {
        string directoryPath;
        using (AndroidJavaClass environmentClass = new AndroidJavaClass("android.os.Environment"))
        using (AndroidJavaObject externalStorageDir = environmentClass.CallStatic<AndroidJavaObject>("getExternalStorageDirectory"))
        {
            string storagePath = externalStorageDir.Call<string>("getAbsolutePath");
            directoryPath = Path.Combine(storagePath, "Pano");
        }
        if (!Directory.Exists(directoryPath))
        {
            UnityEngine.Debug.LogError("Cartella non trovata: " + directoryPath);
            return;
        }

        fileList.Clear();
        foreach (string file in Directory.GetFiles(directoryPath, "*.jpg"))
        {
            fileList.Add(Path.GetFileNameWithoutExtension(file));
        }

        GenerateMenu();
    }

    void GenerateMenu()
    {
        // Pulisci il menu esistente
        foreach (Transform child in menuContainer)
        {
            Destroy(child.gameObject);
        }

        // Calcola il numero totale di pagine
        totalPages = Mathf.CeilToInt((float)fileList.Count / filesPerPage);

        // Limita la pagina corrente tra 0 e totalPages-1
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        // Calcola l'intervallo di file da visualizzare
        int startIndex = currentPage * filesPerPage;
        int endIndex = Mathf.Min(startIndex + filesPerPage, fileList.Count);

        float verticalSpacing = 6f;

        // Genera i pulsanti per i file della pagina corrente
        for (int i = startIndex; i < endIndex; i++)
        {
            int displayIndex = i - startIndex; // Indice relativo alla pagina corrente

            GameObject button = Instantiate(buttonPrefab, menuContainer);
            button.transform.localPosition = new Vector3(0, -(displayIndex + 1) * verticalSpacing, 0);
            button.name = "FileButton_" + fileList[i];

            TMP_Text buttonText = button.GetComponent<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = fileList[i];
                buttonText.color = Color.white;
            }

            int index = i;

            // Crea un GameObject figlio per il collider
            GameObject colliderObject = new GameObject("ButtonCollider");
            colliderObject.transform.SetParent(button.transform);
            colliderObject.transform.localPosition = Vector3.zero;

            // Aggiungi un collider molto più grande
            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.5f, 0.1f, 0.01f);  // Più largo e alto
            collider.center = Vector3.zero;

            // Trasferisci il ButtonTrigger sul GameObject del collider
            ButtonTrigger trigger = colliderObject.AddComponent<ButtonTrigger>();
            trigger.Setup(() => SelectFile(fileList[index]));

            // Collega il trigger al testo per il cambio colore
            trigger.SetTextComponent(buttonText);

            UnityEngine.Debug.Log("Created button: " + fileList[index] + " with large collider");
        }

        // Crea i pulsanti di navigazione solo se ci sono più pagine
        if (totalPages > 1)
        {
            // Crea il pulsante "Indietro" a fondo pagina
            GameObject prevButton = Instantiate(buttonPrefab, menuContainer);
            prevButton.transform.localPosition = new Vector3(-6f, -(filesPerPage + 1) * verticalSpacing, 0);
            prevButton.name = "PrevPageButton";

            TMP_Text prevButtonText = prevButton.GetComponent<TMP_Text>();
            if (prevButtonText != null)
            {
                prevButtonText.text = "Prev";
                prevButtonText.color = currentPage > 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grigio se inattivo
            }

            // Aggiungi collider e trigger al pulsante precedente
            GameObject prevColliderObj = new GameObject("PrevButtonCollider");
            prevColliderObj.transform.SetParent(prevButton.transform);
            prevColliderObj.transform.localPosition = Vector3.zero;

            BoxCollider prevCollider = prevColliderObj.AddComponent<BoxCollider>();
            prevCollider.size = new Vector3(0.5f, 0.1f, 0.01f);
            prevCollider.center = Vector3.zero;

            ButtonTrigger prevTrigger = prevColliderObj.AddComponent<ButtonTrigger>();
            prevTrigger.Setup(() => NavigateToPreviousPage());
            prevTrigger.SetTextComponent(prevButtonText);

            // Crea il pulsante "Avanti" a fondo pagina
            GameObject nextButton = Instantiate(buttonPrefab, menuContainer);
            nextButton.transform.localPosition = new Vector3(6f, -(filesPerPage + 1) * verticalSpacing, 0);
            nextButton.name = "NextPageButton";

            TMP_Text nextButtonText = nextButton.GetComponent<TMP_Text>();
            if (nextButtonText != null)
            {
                nextButtonText.text = "Next";
                nextButtonText.color = currentPage < totalPages - 1 ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grigio se inattivo
            }

            // Aggiungi collider e trigger al pulsante successivo
            GameObject nextColliderObj = new GameObject("NextButtonCollider");
            nextColliderObj.transform.SetParent(nextButton.transform);
            nextColliderObj.transform.localPosition = Vector3.zero;

            BoxCollider nextCollider = nextColliderObj.AddComponent<BoxCollider>();
            nextCollider.size = new Vector3(0.5f, 0.1f, 0.01f);
            nextCollider.center = Vector3.zero;

            ButtonTrigger nextTrigger = nextColliderObj.AddComponent<ButtonTrigger>();
            nextTrigger.Setup(() => NavigateToNextPage());
            nextTrigger.SetTextComponent(nextButtonText);

            // Aggiungi un indicatore della pagina attuale tra i pulsanti
            GameObject pageIndicator = Instantiate(buttonPrefab, menuContainer);
            pageIndicator.transform.localPosition = new Vector3(0f, -(filesPerPage + 1) * verticalSpacing, 0);
            pageIndicator.name = "PageIndicator";

            TMP_Text pageIndicatorText = pageIndicator.GetComponent<TMP_Text>();
            if (pageIndicatorText != null)
            {
                pageIndicatorText.text = $"{currentPage + 1}/{totalPages}";
                pageIndicatorText.fontSize *= 0.8f; // Riduce leggermente la dimensione del testo
                pageIndicatorText.color = Color.white;
            }
        }
    }

    // Metodi per gestire la navigazione tra le pagine
    public void NavigateToNextPage()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            UnityEngine.Debug.Log($"Navigando alla pagina successiva: {currentPage + 1}/{totalPages}");
            GenerateMenu();
        }
    }

    public void NavigateToPreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UnityEngine.Debug.Log($"Navigando alla pagina precedente: {currentPage + 1}/{totalPages}");
            GenerateMenu();
        }
    }
    void AddVisualFeedback(GameObject button)
    {
        // Aggiungi un componente per gestire l'evidenziazione quando il raggio passa sopra
        RaycastHitFeedback hitFeedback = button.AddComponent<RaycastHitFeedback>();
        hitFeedback.normalColor = Color.white;
        hitFeedback.hoverColor = Color.yellow;
        hitFeedback.selectColor = Color.green;

        // Se stai usando TextMeshPro, puoi controllare il colore del testo
        TMP_Text text = button.GetComponent<TMP_Text>();
        if (text != null)
        {
            hitFeedback.SetTargetText(text);
        }
    }

    void SelectFile(string fileName)
    {
        UnityEngine.Debug.Log("File selezionato: " + fileName);
        //string imgFile = "/storage/emulated/0/Pano/" + fileName;
        panoramaConverter.LoadPanorama(fileName);
        menuPanel.SetActive(false); // Chiudi il menu dopo la selezione
    }
}

// Classe per il feedback visivo quando il raggio colpisce un oggetto
public class RaycastHitFeedback : MonoBehaviour
{
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectColor = Color.green;

    private TMP_Text targetText;
    private Renderer targetRenderer;
    private Color originalColor;
    private bool isHovering = false;

    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        targetText = GetComponent<TMP_Text>();

        if (targetRenderer != null)
        {
            originalColor = targetRenderer.material.color;
        }
        else if (targetText != null)
        {
            originalColor = targetText.color;
        }
    }

    public void SetTargetText(TMP_Text text)
    {
        targetText = text;
        originalColor = text.color;
    }

    void OnEnable()
    {
        ResetVisuals();
    }

    public void OnRayEnter()
    {
        isHovering = true;
        if (targetRenderer != null)
        {
            targetRenderer.material.color = hoverColor;
        }
        else if (targetText != null)
        {
            targetText.color = hoverColor;
        }
    }

    public void OnRayExit()
    {
        isHovering = false;
        ResetVisuals();
    }

    public void OnRaySelect()
    {
        if (targetRenderer != null)
        {
            targetRenderer.material.color = selectColor;
        }
        else if (targetText != null)
        {
            targetText.color = selectColor;
        }

        // Ripristina dopo un breve ritardo
        Invoke("ResetVisuals", 0.2f);
    }

    void ResetVisuals()
    {
        if (isHovering) return;

        if (targetRenderer != null)
        {
            targetRenderer.material.color = normalColor;
        }
        else if (targetText != null)
        {
            targetText.color = normalColor;
        }
    }
}
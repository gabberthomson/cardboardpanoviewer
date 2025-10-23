using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Diagnostics;
using SimpleFileBrowser;
using Meta.XR;
using System.Collections.Specialized;
using System;
using System.Linq;
using System.Text.RegularExpressions;


// Corretto per i componenti OVR

public class FileSelector : MonoBehaviour
{
    public Transform menuContainer; // GameObject vuoto per i pulsanti
    public GameObject buttonPrefab; // Prefab del testo 3D per i file
    public CardboardPanoramaConverter panoramaConverter; // Riferimento allo script per caricare il file
    public GameObject menuPanel; // Il pannello del menu (per nasconderlo e mostrarlo)
    public Canvas helpCanvas; // Canvas con sfondo nero e testo guida
    public TextMeshProUGUI menuText;

    // Componenti per il raggio di selezione
    public OVRRaycaster raycaster;
    public LineRenderer rayRenderer;
    public float rayMaxLength = 10f;
    public Color rayColor = Color.blue;
    private GameObject _lastHoveredObject = null;
    private bool firstTime = true;


    private List<string> fileList = new List<string>();
    public Transform rayOrigin;

    private int currentPage = 0;
    private int filesPerPage = 6;
    private int totalPages = 0;

    void Start()
    {
        PositionMenuInFrontOfUser();

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

        // Debug per verificare l'inizializzazione
        UnityEngine.Debug.Log("FileSelector initialized");
    }


    void Update()
    {
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
        else
        {
            if (rayRenderer == null) UnityEngine.Debug.Log("rayRenderer = null");
            if (rayOrigin == null) UnityEngine.Debug.Log("rayOrigin = null");
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
                // UnityEngine.Debug.Log("Hovering over: " + hitObject.name);
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
            menuText.transform.position = cameraTransform.position + cameraTransform.forward * 4f + Vector3.down * 0.5f; // 2 metri davanti all'utente
            menuText.transform.rotation = Quaternion.LookRotation(cameraTransform.forward); // Ruota il menu per guardare l'utente
        }
    }

    private void BuildMenuRoutine()
    {
        // Pulisci vecchia pagina in un frame “vuoto”
        foreach (Transform child in menuContainer)
            Destroy(child.gameObject);

        // Ricrea i pulsanti/paginazione
        GenerateMenu();               // la tua funzione esistente

    }

    public bool ShowMenu()
    {
        bool status = false;
        try
        {
            UnityEngine.Debug.Log("Show menu");
            if (firstTime)
            {
                // Prova a caricare o apri il picker
                if (!FolderPickerReceiver.Instance.LoadOrRequestUri())
                {
                    UnityEngine.Debug.Log("LoadOrRequestUri ancora nn c'è il file");
                    return false; // Picker aperto: aspetti il prossimo giro
                }
                firstTime = false;
                // Elenco sincrono dei file
                fileList = FolderPickerReceiver.Instance.ListFilesSync();
                // dopo aver popolato la lista
                fileList.Sort(StringComparer.CurrentCultureIgnoreCase);

                UnityEngine.Debug.Log("Si va avanti nel menu");
            }

            PositionMenuInFrontOfUser();
            BuildMenuRoutine();
            status = true;
        }
        catch (Exception ex)
        {
            status = false;
        }
        return status;
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
        int displayIndex = 0;
        // Write sometething if empty dir
        if (fileList.Count == 0)
        {
            displayIndex++;
            GameObject button = Instantiate(buttonPrefab, menuContainer);
            button.transform.localPosition = new Vector3(0, -displayIndex * verticalSpacing, 0);
            button.name = "NoFile";

            TMP_Text buttonText = button.GetComponent<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = "Empty Diretory";
                buttonText.color = Color.white;
            }
        }
        else
        {
            // Genera i pulsanti per i file della pagina corrente
            for (int i = startIndex; i < endIndex; i++)
            {
                displayIndex++; // Indice relativo alla pagina corrente

                GameObject button = Instantiate(buttonPrefab, menuContainer);
                button.transform.localPosition = new Vector3(0, -displayIndex * verticalSpacing, 0);
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
                displayIndex++;
                // Crea il pulsante "Indietro" a fondo pagina
                GameObject prevButton = Instantiate(buttonPrefab, menuContainer);
                prevButton.transform.localPosition = new Vector3(-6f, -displayIndex * verticalSpacing, 0);
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
                nextButton.transform.localPosition = new Vector3(6f, -displayIndex * verticalSpacing, 0);
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
                pageIndicator.transform.localPosition = new Vector3(0f, -displayIndex * verticalSpacing, 0);
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

        // Insert the Tutorial Button
        displayIndex++;
        GameObject tutorialButton = Instantiate(buttonPrefab, menuContainer);
        tutorialButton.transform.localPosition = new Vector3(-0f, -displayIndex * verticalSpacing, 0);
        tutorialButton.name = "TutorialButton";

        TMP_Text tutorialButtonText = tutorialButton.GetComponent<TMP_Text>();
        tutorialButtonText.text = "Tutorial";
        tutorialButtonText.color = Color.white;

        // Aggiungi collider e trigger al pulsante precedente
        GameObject tutorialColliderObj = new GameObject("tutorialButtonCollider");
        tutorialColliderObj.transform.SetParent(tutorialButton.transform);
        tutorialColliderObj.transform.localPosition = Vector3.zero;

        BoxCollider tutorialCollider = tutorialColliderObj.AddComponent<BoxCollider>();
        tutorialCollider.size = new Vector3(0.5f, 0.1f, 0.01f);
        tutorialCollider.center = Vector3.zero;

        ButtonTrigger tutorialTrigger = tutorialColliderObj.AddComponent<ButtonTrigger>();
        tutorialTrigger.Setup(() => Tutorial());
        tutorialTrigger.SetTextComponent(tutorialButtonText);

        // Insert the Exit Button
        displayIndex++;
        GameObject exitButton = Instantiate(buttonPrefab, menuContainer);
        exitButton.transform.localPosition = new Vector3(-0f, -displayIndex * verticalSpacing, 0);
        exitButton.name = "ExitButton";

        TMP_Text exitButtonText = exitButton.GetComponent<TMP_Text>();
        exitButtonText.text = "Exit App";
        exitButtonText.color = Color.white;

        // Aggiungi collider e trigger al pulsante precedente
        GameObject exitColliderObj = new GameObject("ExitButtonCollider");
        exitColliderObj.transform.SetParent(exitButton.transform);
        exitColliderObj.transform.localPosition = Vector3.zero;

        BoxCollider exitCollider = exitColliderObj.AddComponent<BoxCollider>();
        exitCollider.size = new Vector3(0.5f, 0.1f, 0.01f);
        exitCollider.center = Vector3.zero;

        ButtonTrigger exitTrigger = exitColliderObj.AddComponent<ButtonTrigger>();
        exitTrigger.Setup(() => ExitApp());
        exitTrigger.SetTextComponent(exitButtonText);

    }


    public void ExitApp()
    {
        Application.Quit();
    }

    public void Tutorial()
    {
        menuPanel.SetActive(false); // Chiudi il menu dopo la selezione
        helpCanvas.enabled = true;
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
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Diagnostics;

public class ButtonHandler : MonoBehaviour
{
    public InputActionReference buttonMenu;         // Tasto per aprire il menu
    public InputActionReference buttonA;            // Tasto A per la modalità occhio sinistro
    public GameObject menuPanel;                    // Il pannello del menu
    public Renderer panoramaRenderer;               // La sfera che mostra il panorama
    private bool leftEyeOnly = false;               // Stato dell'occhio sinistro
    public FileSelector fileSelector;               // Riferimento a FileSelector per selezionare i file
    public Canvas helpCanvas;                       // Canvas con sfondo nero e testo guida

    [Header("Zoom (shader)")]
    public Camera centerEyeCamera;           // la camera “center eye”
    [Range(1f, 8f)] public float zoom = 1f;  // stato corrente
    public float zoomMin = 1f;
    public float zoomMax = 8f;
    public float zoomSpeed = 1.5f;           // sensibilità levetta
    public float zoomSmooth = 0.08f;         // smorzamento
    private float zoomVel;                   // interno per SmoothDamp


    void Start()
    {
        menuPanel.SetActive(false);
        if (helpCanvas != null) helpCanvas.enabled = true;

        if (centerEyeCamera == null)
            UnityEngine.Debug.LogError("Zoom: CenterEye Camera non assegnata: lo zoom shader usera' forward di Camera.main.");

        if (panoramaRenderer != null && panoramaRenderer.material != null)
        {
            var mat = panoramaRenderer.material;
            mat.SetFloat("_Zoom", zoom);
            var cam = centerEyeCamera != null ? centerEyeCamera : Camera.main;
            if (cam != null) mat.SetVector("_FocusDir", cam.transform.forward);
        }
    }

    void Update()
    {
        bool menuPressed = (buttonMenu.action.WasPressedThisFrame());
        if (menuPressed) ToggleMenu();

        bool aPressed = (buttonA.action.WasPressedThisFrame());   // A (destra)
        if (!menuPanel.activeSelf && aPressed) ToggleLeftEyeMode();

        // === ZOOM SHADER (levetta destra su/giù) ===
        if (panoramaRenderer != null && panoramaRenderer.material != null)
        {
            var mat = panoramaRenderer.material;
            var cam = centerEyeCamera != null ? centerEyeCamera : Camera.main;
            if (cam != null)
            {
                // direzione di fuoco comune ai due occhi
                mat.SetVector("_FocusDir", cam.transform.forward);

                // input levetta destra (Oculus)
                float stickY = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;

                // scala esponenziale -> zoom naturale; smorzamento per comfort
                float targetZoom = Mathf.Clamp(zoom * Mathf.Exp(stickY * zoomSpeed * Time.deltaTime), zoomMin, zoomMax);
                zoom = Mathf.SmoothDamp(zoom, targetZoom, ref zoomVel, zoomSmooth);

                mat.SetFloat("_Zoom", zoom);
            }
        }

    }

    void ToggleMenu()
    {
        UnityEngine.Debug.Log("Toggle Menu");
        if (!menuPanel.activeSelf)
        {
            bool activeSelf = fileSelector.ShowMenu();
            UnityEngine.Debug.Log("active self: " + activeSelf);
            if (activeSelf)
            {
                helpCanvas.enabled = false;
                menuPanel.SetActive(!menuPanel.activeSelf);
            }
        }
        else
        {
            UnityEngine.Debug.Log("Destroy");
            menuPanel.SetActive(!menuPanel.activeSelf);
            foreach (Transform child in fileSelector.menuContainer)
                Destroy(child.gameObject);
        }
    }

    void ToggleLeftEyeMode()
    {
        leftEyeOnly = !leftEyeOnly;
        if (panoramaRenderer != null && panoramaRenderer.material != null)
            panoramaRenderer.material.SetFloat("_LeftEyeOnlyMode", leftEyeOnly ? 1.0f : 0.0f);
        UnityEngine.Debug.Log("Left Eye Mode: " + leftEyeOnly);
    }
}

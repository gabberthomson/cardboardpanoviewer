using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Diagnostics;

public class ButtonHandler : MonoBehaviour
{
    public InputActionReference buttonMenu; // Tasto per aprire il menu
    public InputActionReference buttonA; // Tasto A per la modalità occhio sinistro
    public GameObject menuPanel; // Il pannello del menu
    public Renderer panoramaRenderer; // La sfera che mostra il panorama
    private bool leftEyeOnly = false; // Stato dell'occhio sinistro
    public FileSelector fileSelector; // Riferimento a FileSelector per selezionare i file
    public Canvas helpCanvas; // Canvas con sfondo nero e testo guida

    void Start()
    {
        menuPanel.SetActive(false); // Nasconde il menu all'avvio
        if (helpCanvas != null)
        {
            helpCanvas.enabled = true;
        }
    }

    void Update()
    {
        bool menuPressed =
            (buttonMenu.action.WasPressedThisFrame());

        if (menuPressed)
            ToggleMenu();

        bool aPressed =
            (buttonA.action.WasPressedThisFrame());   // A (destra)

        if (!menuPanel.activeSelf && aPressed)
            ToggleLeftEyeMode();
    }
    void ToggleMenu()
    {
        UnityEngine.Debug.Log("Toggle Menu");
        //fileSelector.ShowFileBrowser();
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
        else {
            UnityEngine.Debug.Log("Destroy");
            menuPanel.SetActive(!menuPanel.activeSelf); // Alterna visibilità del menu
            foreach (Transform child in fileSelector.menuContainer) // Cancella i pulsanti
            {
                Destroy(child.gameObject);
            }
        }
    }

    void ToggleLeftEyeMode()
    {
        leftEyeOnly = !leftEyeOnly;
        panoramaRenderer.material.SetFloat("_LeftEyeOnlyMode", leftEyeOnly ? 1.0f : 0.0f);
        UnityEngine.Debug.Log("Left Eye Mode: " + leftEyeOnly);
    }
}

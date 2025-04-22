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
        // Premendo "Menu", il menu si apre/chiude
        if (buttonMenu.action.WasPressedThisFrame())
        {
            helpCanvas.enabled = false;
            ToggleMenu();

        }

        // Se il menu è chiuso, il pulsante A cambia la modalità occhio sinistro
        if (!menuPanel.activeSelf && buttonA.action.WasPressedThisFrame())
        {
            ToggleLeftEyeMode();
        }
    }

    void ToggleMenu()
    {
        //fileSelector.ShowFileBrowser();
        if(!menuPanel.activeSelf)
        {
            fileSelector.ShowMenu();
        }
        else { 
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

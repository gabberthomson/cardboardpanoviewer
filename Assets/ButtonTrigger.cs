using UnityEngine;
using System;
using System.Diagnostics;
using TMPro;
// Classe aggiornata ButtonTrigger per supportare l'input a raggio
public class ButtonTrigger : MonoBehaviour
{
    private System.Action onClickAction;
    private TMP_Text textComponent;
    private Color normalColor = Color.white;
    private Color hoverColor = Color.yellow;
    private Color selectColor = Color.green;
    private bool isHovering = false;

    public void SetTextComponent(TMP_Text text)
    {
        textComponent = text;
        if (textComponent != null)
        {
            normalColor = textComponent.color;
        }
    }

    public void Setup(System.Action action)
    {
        onClickAction = action;
        UnityEngine.Debug.Log("Action setup on ButtonTrigger: " + gameObject.name);
    }

    public void OnHoverEnter()
    {
        isHovering = true;
        if (textComponent != null)
        {
            textComponent.color = hoverColor;
            // UnityEngine.Debug.Log("Hover entered: " + gameObject.name);
        }
    }

    public void OnHoverExit()
    {
        isHovering = false;
        if (textComponent != null)
        {
            textComponent.color = normalColor;
            // UnityEngine.Debug.Log("Hover exited: " + gameObject.name);
        }
    }

    public void OnClick()
    {
        UnityEngine.Debug.Log("Click detected on: " + gameObject.name);

        if (textComponent != null)
        {
            textComponent.color = selectColor;
            UnityEngine.Debug.Log("Text color changed to select color");
        }

        if (onClickAction != null)
        {
            UnityEngine.Debug.Log("Invoking click action");
            onClickAction.Invoke();
        }
        else
        {
            UnityEngine.Debug.LogError("onClickAction is null on: " + gameObject.name);
        }

        // Ripristina dopo un breve ritardo
        Invoke("ResetColor", 0.2f);
    }

    void ResetColor()
    {
        if (!isHovering && textComponent != null)
        {
            textComponent.color = normalColor;
        }
    }

    // Metodi trigger per compatibilità
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Controller") || other.CompareTag("PlayerHand"))
        {
            OnHoverEnter();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Controller") || other.CompareTag("PlayerHand"))
        {
            OnHoverExit();
        }
    }
}
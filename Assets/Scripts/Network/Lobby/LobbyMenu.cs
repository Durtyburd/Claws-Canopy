using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
    // public LobbyCameraManager lobbyCameraManager;
    List<LobbyButton> buttons;
    int currentIndex = 0;
    private const int inactivePriority = 10;
    const int activePriority = 20;
    
    private void Awake()
    {
        buttons = new List<LobbyButton>();
        
        foreach (Transform child in transform)
        {
            LobbyButton button = child.GetComponent<LobbyButton>();
            if (button)
            {
                buttons.Add(button);
                // button.myCamera.Priority = inactivePriority;
                Toggle(false);
                button.onHoverEnter = Hovered;
                button.onHoverExit = QuitHovering;
                ++currentIndex;
            }
        }
        currentIndex = 0;
        Toggle(true);
        
        InputSystem.actions.FindAction("Next").performed += _ =>
        {
            SwitchFocus((currentIndex+ 1) % buttons.Count);
        };
        InputSystem.actions.FindAction("Previous").performed += _ =>
        {
            SwitchFocus((currentIndex - 1 + buttons.Count) % buttons.Count);
        };
    }

    private void SwitchFocus(int newIndex)
    {
        Toggle(false);
        currentIndex = newIndex;
        Toggle(true);
    }

    private void Toggle(bool on, bool moveCamera = true)
    {
        buttons[currentIndex].myDinosaurAnimator.SetBool("isSelected", on);
        if (moveCamera)
        {
            buttons[currentIndex].myCamera.Priority = on ? activePriority : inactivePriority;
        }
        buttons[currentIndex].interactable = on;
    }
    
    private void Hovered(LobbyButton hovered)
    {
        int index = buttons.IndexOf(hovered);
        int tmp = currentIndex;
        Toggle(false, false);
        currentIndex = index;
        Toggle(true, false);
        currentIndex = tmp;
    }

    private void QuitHovering(LobbyButton hovered)
    {
        int index = buttons.IndexOf(hovered);
        if (currentIndex != index)
        {
            int tmp = currentIndex;
            currentIndex = index;
            Toggle(false, false);
            currentIndex = tmp;
            Toggle(true, false);
        }
    }
}

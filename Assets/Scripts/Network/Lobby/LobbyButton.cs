using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyButton : Button, IPointerEnterHandler, IPointerExitHandler
{
    public CinemachineCamera myCamera;
    public Animator myDinosaurAnimator;
    public bool selected = false;
    public UnityAction<LobbyButton> onHoverEnter;
    public UnityAction<LobbyButton> onHoverExit;


    public override void OnPointerEnter(PointerEventData eventData)
    {
        onHoverEnter.Invoke(this);
        base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit.Invoke(this);
        base.OnPointerExit(eventData);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // stops play mode in editor
#else
        Application.Quit(); // works in actual build
#endif
    }

    public void Settings()
    {
        SettingsMenu.Instance.OpenMenu();
        myDinosaurAnimator.SetBool("isAttacking", true);
    }

    public void SettingsClosed()
    {
        myDinosaurAnimator.SetBool("isAttacking", false);
    }
}
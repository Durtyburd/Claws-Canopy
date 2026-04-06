using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyCameraManager : MonoBehaviour
{
    public Transform virtualCameras;
    List<CinemachineCamera> cameras;
    private int currentIndex = 0;
}

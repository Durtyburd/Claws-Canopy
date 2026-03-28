using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayer : NetworkBehaviour
{

    InputAction moveAction;
    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            Vector2 movement = moveAction.ReadValue<Vector2>();
            Vector3 playerMovement = Time.deltaTime *new Vector3(movement.x, 0, movement.y);
            transform.position += playerMovement;
        }
    }
}

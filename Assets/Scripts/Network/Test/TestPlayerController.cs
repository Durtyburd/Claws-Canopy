using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevelPhysics2D;

public class TestPlayerController : NetworkBehaviour
{
    private ItemsManager itemsManagerScript;
    
    private Rigidbody rb;
    public const float walkingSpeed = 12f;
    private float movementSpeed; 
    
    private InputAction moveAction;

    private bool isWalking, isIdle;

    Vector3 movement;

    public LayerMask interactableMask;

    [HideInInspector] public GameObject objPlayerIsNear;
    
    [HideInInspector] 
    [SyncVar(hook = nameof(OnEquipItemChanged))]
    public string equippedItem;
    
    private void Awake()
    {
        itemsManagerScript = FindFirstObjectByType<ItemsManager>(FindObjectsInactive.Include);
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            Vector2 move = moveAction.ReadValue<Vector2>();
            
            movement = new Vector3(move.x, 0, move.y);
            // transform.position += movement * Time.deltaTime;
            if (movement.sqrMagnitude > 0.001f)
            {
                isWalking = true;
                isIdle = false;
                movementSpeed = walkingSpeed;
            }
            else
            {
                isWalking = false;
                isIdle = true;
            }

            Collider[] objectsDetected = new Collider[20];
            List<Collider> objectsDetectedList = new List<Collider>();
            int count = 0;

            if (isServer)
            {
                PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
                count = physicsScene.OverlapSphere(transform.position, 3.7f, objectsDetected, interactableMask, QueryTriggerInteraction.Ignore/*whether to hit triggers*/);
            }
            else
            {
                objectsDetected = Physics.OverlapSphere(transform.position, 3.7f, interactableMask);
                count = objectsDetected.Length;
            }

            GameObject objShortestDistance = null;
            for(int i = 0; i < count; ++i)
            {
                if (objectsDetected[i].GetComponent<NetworkIdentity>().netId != 0)
                {
                    if (objShortestDistance == null)
                    {
                        objShortestDistance = objectsDetected[i].gameObject;
                    }
                    else
                    {
                        //could add collider offset but no offset so i don't care
                        
                        float newDistance = Vector3.Distance(transform.position, objectsDetected[i].transform.position);
                        float oldDistance = Vector3.Distance(transform.position, objShortestDistance.transform.position);
                        if (newDistance < oldDistance)
                        {
                            objShortestDistance = objectsDetected[i].gameObject;
                        }
                    }
                }
            }
            
            objPlayerIsNear = objShortestDistance;
        }
    }

    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            movement*= walkingSpeed*Time.deltaTime;
            rb.MovePosition(rb.position + movement);
        }
    }

    private void OnEquipItemChanged(string oldValue, string newValue)
    {
        if (itemsManagerScript)
        {
            foreach (Transform child in transform.Find("EquippedItem"))
            {
                Destroy(child.gameObject);
            }

            if (newValue != "")
            {
                Transform newObj = Instantiate(itemsManagerScript.itemsListObj.transform.Find(newValue), transform.Find("EquippedItem"));
                newObj.transform.name = newValue;
                newObj.gameObject.SetActive(true);
            }
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TallGrassZone : MonoBehaviour
{
    private void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var vis = other.GetComponent<PlayerVisibility>();
        if (vis != null)
            vis.EnterGrass();
    }

    private void OnTriggerExit(Collider other)
    {
        var vis = other.GetComponent<PlayerVisibility>();
        if (vis != null)
            vis.ExitGrass();
    }
}

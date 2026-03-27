using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class FireArm : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float fireRate = 0.09f;
    [SerializeField] private float range = 500f;
    [SerializeField] private bool automatic;

    [SerializeField] private LayerMask shootableLayers;
    private bool canShoot = true;

    [Header("Sounds")]
    [SerializeField] private AudioClip shootSound;

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform shootPoint;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Decal")]
    [SerializeField] private Material bulletHoleMaterial;
    [SerializeField] private float decalSize = 0.15f;
    [SerializeField] private float decalLifetime = 30f;

    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && !automatic)
        {
            Shoot();
        }
        else if (automatic && Mouse.current.leftButton.isPressed)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (!canShoot) return;
        if (shootPoint == null) return;

        Debug.DrawRay(shootPoint.position, shootPoint.forward * range, Color.red, 1f);

        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out RaycastHit hit, range, shootableLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name} on layer {hit.collider.gameObject.layer}");
            var enemyAI = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.TakeDamage(damage);
            else
                SpawnDecal(hit);
        }
        else
        {
            Debug.Log("Raycast hit nothing");
        }

        StartCoroutine(ResetFireRate());

        if (anim != null)
        {
            anim.CrossFadeInFixedTime("Shoot", 0.1f);
        }

        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, shootPoint.position);
        }
    }

    private void SpawnDecal(RaycastHit hit)
    {
        if (bulletHoleMaterial == null) return;

        var decalObj = new GameObject("BulletHole");
        decalObj.transform.position = hit.point + hit.normal * 0.025f;
        decalObj.transform.rotation = Quaternion.LookRotation(-hit.normal)
            * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        var projector = decalObj.AddComponent<DecalProjector>();
        projector.material = bulletHoleMaterial;
        projector.size = new Vector3(decalSize, decalSize, 0.1f);
        projector.pivot = new Vector3(0f, 0f, 0.05f);

        Destroy(decalObj, decalLifetime);
    }

    private IEnumerator ResetFireRate()
    {
        canShoot = false;
        yield return new WaitForSeconds(fireRate);
        canShoot = true;
    }
}
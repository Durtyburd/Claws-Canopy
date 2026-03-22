using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private GameObject bulletHolePrefab;

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

    if (Physics.Raycast(shootPoint.position, shootPoint.forward, out RaycastHit hit, range, shootableLayers, QueryTriggerInteraction.Ignore))
    {
        if (bulletHolePrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            GameObject impact = Instantiate(
                bulletHolePrefab,
                hit.point + hit.normal * 0.001f,
                rotation
            );

            impact.transform.localScale = Vector3.one * 0.05f;

            Destroy(impact, 10f);
        }
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

    private IEnumerator ResetFireRate()
    {
        canShoot = false;
        yield return new WaitForSeconds(fireRate);
        canShoot = true;
    }
}
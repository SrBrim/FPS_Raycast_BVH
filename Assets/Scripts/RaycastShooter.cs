using UnityEngine;
using UnityEngine.InputSystem;
using System.Diagnostics;

public class RaycastShooter : MonoBehaviour
{
    public Transform firePoint;
    public float range = 100f;

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (firePoint == null)
        {
            UnityEngine.Debug.LogError("FirePoint não atribuído!");
            return;
        }

        Ray ray = new Ray(firePoint.position, firePoint.forward);
        RaycastHit hit;

        Stopwatch sw = Stopwatch.StartNew();

        if (Physics.Raycast(ray, out hit, range))
        {
            if (hit.collider is MeshCollider)
            {
                UnityEngine.Debug.Log("Acertou: " + hit.collider.name);
                Destroy(hit.collider.transform.root.gameObject);
            }
        }

        sw.Stop();

        UnityEngine.Debug.Log(
            "Raycast SEM BVH: " +
            sw.Elapsed.TotalMilliseconds.ToString("F4") +
            " ms"
        );
    }
}

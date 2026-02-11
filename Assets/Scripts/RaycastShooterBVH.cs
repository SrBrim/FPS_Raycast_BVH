using UnityEngine;
using UnityEngine.InputSystem;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class RaycastShooterBVH : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public GameObject tracerPrefab;
    public Transform firePoint;

    [Header("Config")]
    public float maxDistance = 200f;
    public bool destroyOnHit = true;

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
        if (firePoint == null || cam == null)
        {
            Debug.LogError("Camera ou FirePoint não configurados!");
            return;
        }

        // 🎯 Ray central da tela (mira)
        Ray screenRay = cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f)
        );

        Vector3 targetPoint;

        if (Physics.Raycast(screenRay, out RaycastHit screenHit, maxDistance))
        {
            targetPoint = screenHit.point;
        }
        else
        {
            targetPoint = screenRay.GetPoint(maxDistance);
        }

        // 🔫 Direção real do tiro sai do firePoint
        Vector3 shootDirection =
            (targetPoint - firePoint.position).normalized;

        Ray shootRay =
            new Ray(firePoint.position, shootDirection);

        EnemyBVH[] enemies =
            FindObjectsByType<EnemyBVH>(FindObjectsSortMode.None);

        Stopwatch sw = new Stopwatch();

        // ==============================
        // 🔴 FORÇA BRUTA (SEM BVH)
        // ==============================
        int bruteTests = 0;

        sw.Start();

        foreach (var enemy in enemies)
        {
            var meshColliders =
                enemy.GetComponentsInChildren<MeshCollider>();

            foreach (var col in meshColliders)
            {
                bruteTests++;
                col.Raycast(shootRay, out _, maxDistance);
            }
        }

        sw.Stop();
        double timeWithout = sw.Elapsed.TotalMilliseconds;
        sw.Reset();

        // ==============================
        // 🟢 COM BVH
        // ==============================
        int bvhRootTests = 0;

        EnemyBVH closestEnemy = null;
        RaycastHit closestHit = new RaycastHit();
        float closestDistance = Mathf.Infinity;

        sw.Start();

        foreach (var enemy in enemies)
        {
            if (enemy.root == null)
                continue;

            bvhRootTests++;

            if (enemy.root.Intersect(shootRay, out RaycastHit bvhHit))
            {
                if (bvhHit.distance < closestDistance)
                {
                    closestDistance = bvhHit.distance;
                    closestHit = bvhHit;
                    closestEnemy = enemy;
                }
            }
        }

        sw.Stop();
        double timeWith = sw.Elapsed.TotalMilliseconds;

        // ==============================
        // 📊 DEBUG LIMPO
        // ==============================
        Debug.Log(
            "\n===== RESULTADO DO TIRO =====\n" +
            "Sem BVH: " + timeWithout.ToString("F4") + " ms\n" +
            "Com BVH: " + timeWith.ToString("F4") + " ms\n" +
            "Testes Força Bruta (mesh): " + bruteTests + "\n" +
            "Testes BVH (raiz): " + bvhRootTests + "\n" +
            "Redução: " +
            ((1 - (timeWith / timeWithout)) * 100f).ToString("F2") +
            "%\n============================="
        );

        // ==============================
        // 🔥 TRACER VISUAL
        // ==============================
        Vector3 finalHitPoint =
            closestEnemy != null
            ? closestHit.point
            : firePoint.position + shootDirection * maxDistance;

        if (tracerPrefab != null)
        {
            GameObject tracer =
                Instantiate(tracerPrefab);

            tracer.GetComponent<BulletTracer>()
                .SetPositions(firePoint.position, finalHitPoint);
        }

        // ==============================
        // 💀 DESTRUIR INIMIGO
        // ==============================
        if (destroyOnHit && closestEnemy != null)
        {
            Destroy(closestEnemy.gameObject);
        }
    }
}

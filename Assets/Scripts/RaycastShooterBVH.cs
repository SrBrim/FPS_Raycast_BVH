using UnityEngine;
using UnityEngine.InputSystem;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

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

        // Debug reduzido: só informação básica
        Debug.Log($"Shoot: enemies encontrados = {enemies.Length}");

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
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (col.Raycast(shootRay, out RaycastHit bruteHit, maxDistance))
                {
                    // não logar aqui; usaremos display
                }
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

        // prepare trace lists
        List<BVHNode> visitedNodes = new List<BVHNode>();
        List<BVHNode> passedAABB = new List<BVHNode>();
        List<BVHNode> hitLeaves = new List<BVHNode>();

        sw.Start();

        foreach (var enemy in enemies)
        {
            if (enemy.root == null)
            {
                continue;
            }

            bvhRootTests++;

            // usa IntersectTrace para coletar informações do caminho
            if (enemy.root.IntersectTrace(shootRay, out RaycastHit bvhHit, visitedNodes, passedAABB, hitLeaves))
            {
                // Filtra por maxDistance para manter consistência com a verificação força bruta
                if (bvhHit.distance <= maxDistance && bvhHit.distance < closestDistance)
                {
                    closestDistance = bvhHit.distance;
                    closestHit = bvhHit;
                    closestEnemy = enemy;
                }
            }
        }

        sw.Stop();
        double timeWith = sw.Elapsed.TotalMilliseconds;

        // Calcula redução (speedup)
        double speedupPct = 0.0;
        double speedupRatio = 0.0;
        if (timeWithout > 0.0)
        {
            speedupPct = (1.0 - (timeWith / timeWithout)) * 100.0;
            if (timeWith > 0.0) speedupRatio = timeWithout / timeWith;
        }

        Debug.Log($"Tiro: timeWithout={timeWithout:F4}ms, timeWith={timeWith:F4}ms, speedup={speedupPct:F2}% (x{speedupRatio:F2})");

        // Mostra resultado formatado no painel
        string hitName = closestEnemy != null ? closestHit.collider.name : string.Empty;
        Vector3 hitPoint = closestEnemy != null ? closestHit.point : Vector3.zero;
        bool didHit = closestEnemy != null;

        ShotResultDisplay.Show(timeWithout, timeWith, bruteTests, bvhRootTests, hitName, hitPoint, didHit);

        // Envia trace para visualizador BVH (se houver dados)
        if (visitedNodes.Count > 0)
        {
            BVHTraceVisualizer.ShowTrace(visitedNodes, passedAABB, hitLeaves, 6f);
        }

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

        if (destroyOnHit && closestEnemy != null)
        {
            Destroy(closestEnemy.gameObject);
        }
    }
}

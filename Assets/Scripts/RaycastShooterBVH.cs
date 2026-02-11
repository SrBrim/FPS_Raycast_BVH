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

        
        Vector3 shootDirection =
            (targetPoint - firePoint.position).normalized;

        Ray shootRay =
            new Ray(firePoint.position, shootDirection);

        EnemyBVH[] enemies =
            FindObjectsByType<EnemyBVH>(FindObjectsSortMode.None);

        
        Debug.Log($"Shoot: enemies encontrados = {enemies.Length}");

        Stopwatch sw = new Stopwatch();

        // sem BVH
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
                    // usar display
                }
            }
        }

        sw.Stop();
        double timeWithout = sw.Elapsed.TotalMilliseconds;
        sw.Reset();

        // com BVH
        
        int bvhRootTests = 0;

        EnemyBVH closestEnemy = null;
        RaycastHit closestHit = new RaycastHit();
        float closestDistance = Mathf.Infinity;

        
        BVHNode traceRootToShow = null;
        List<BVHNode> traceVisited = null;
        List<BVHNode> tracePassed = null;
        List<BVHNode> traceHits = null;
        List<MeshCollider> traceHitColliders = null;

        sw.Start();

        foreach (var enemy in enemies)
        {
            if (enemy.root == null)
            {
                continue;
            }

            bvhRootTests++;

            
            List<BVHNode> visitedNodesLocal = new List<BVHNode>();
            List<BVHNode> passedAABBL = new List<BVHNode>();
            List<BVHNode> hitLeavesLocal = new List<BVHNode>();
            List<MeshCollider> hitCollidersLocal = new List<MeshCollider>();

            if (enemy.root.IntersectTrace(shootRay, out RaycastHit bvhHit, visitedNodesLocal, passedAABBL, hitLeavesLocal, hitCollidersLocal))
            {
                
                if (bvhHit.distance <= maxDistance && bvhHit.distance < closestDistance)
                {
                    closestDistance = bvhHit.distance;
                    closestHit = bvhHit;
                    closestEnemy = enemy;

                    
                    traceRootToShow = enemy.root;
                    traceVisited = visitedNodesLocal;
                    tracePassed = passedAABBL;
                    traceHits = hitLeavesLocal;
                    traceHitColliders = hitCollidersLocal;
                }
            }
        }

        sw.Stop();
        double timeWith = sw.Elapsed.TotalMilliseconds;

      
        double speedupPct = 0.0;
        double speedupRatio = 0.0;
        if (timeWithout > 0.0)
        {
            speedupPct = (1.0 - (timeWith / timeWithout)) * 100.0;
            if (timeWith > 0.0) speedupRatio = timeWithout / timeWith;
        }

        Debug.Log($"Tiro: timeWithout={timeWithout:F4}ms, timeWith={timeWith:F4}ms, speedup={speedupPct:F2}% (x{speedupRatio:F2})");

        
        string hitName = closestEnemy != null ? closestHit.collider.name : string.Empty;
        Vector3 hitPoint = closestEnemy != null ? closestHit.point : Vector3.zero;
        bool didHit = closestEnemy != null;

        ShotResultDisplay.Show(timeWithout, timeWith, bruteTests, bvhRootTests, hitName, hitPoint, didHit);

       
        if (traceRootToShow != null)
        {
            BVHTraceVisualizer.ShowTraceForRoot(traceRootToShow, traceVisited, tracePassed, traceHits, traceHitColliders, 6f);
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

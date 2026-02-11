using UnityEngine;
using System.Collections.Generic;

public class BVHNode
{
    public Bounds bounds;
    public BVHNode left;
    public BVHNode right;

    public SkinnedMeshRenderer leafMesh;

    public bool IsLeaf => leafMesh != null;

    public BVHNode(Bounds b)
    {
        bounds = b;
    }

    public bool Intersect(Ray ray, out RaycastHit closestHit)
    {
        closestHit = new RaycastHit();

        // Corta se AABB não é atingido
        if (!bounds.IntersectRay(ray))
            return false;

        if (IsLeaf)
        {
            // Procura todos os MeshCollider no objeto do SkinnedMeshRenderer e filhos
            MeshCollider[] cols = leafMesh.GetComponentsInChildren<MeshCollider>();
            foreach (var col in cols)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                // Testa colisão; chamador filtra por maxDistance
                if (col.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    closestHit = hit;
                    return true;
                }
            }

            return false;
        }

        bool hitLeft = false;
        bool hitRight = false;

        RaycastHit leftHit = new RaycastHit();
        RaycastHit rightHit = new RaycastHit();

        if (left != null)
            hitLeft = left.Intersect(ray, out leftHit);

        if (right != null)
            hitRight = right.Intersect(ray, out rightHit);

        if (hitLeft && hitRight)
        {
            closestHit = leftHit.distance < rightHit.distance ? leftHit : rightHit;
            return true;
        }
        else if (hitLeft)
        {
            closestHit = leftHit;
            return true;
        }
        else if (hitRight)
        {
            closestHit = rightHit;
            return true;
        }

        return false;
    }

    // Novo: realiza a interseção coletando um traço dos nós visitados
    public bool IntersectTrace(Ray ray, out RaycastHit closestHit, List<BVHNode> visitedNodes, List<BVHNode> passedAABB, List<BVHNode> hitLeaves)
    {
        closestHit = new RaycastHit();

        if (visitedNodes != null) visitedNodes.Add(this);

        // Corta se AABB não é atingido
        if (!bounds.IntersectRay(ray))
            return false;

        if (passedAABB != null) passedAABB.Add(this);

        if (IsLeaf)
        {
            MeshCollider[] cols = leafMesh.GetComponentsInChildren<MeshCollider>();
            foreach (var col in cols)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                if (col.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hitLeaves != null) hitLeaves.Add(this);
                    closestHit = hit;
                    return true;
                }
            }

            return false;
        }

        bool hitLeft = false;
        bool hitRight = false;

        RaycastHit leftHit = new RaycastHit();
        RaycastHit rightHit = new RaycastHit();

        if (left != null)
            hitLeft = left.IntersectTrace(ray, out leftHit, visitedNodes, passedAABB, hitLeaves);

        if (right != null)
            hitRight = right.IntersectTrace(ray, out rightHit, visitedNodes, passedAABB, hitLeaves);

        if (hitLeft && hitRight)
        {
            closestHit = leftHit.distance < rightHit.distance ? leftHit : rightHit;
            return true;
        }
        else if (hitLeft)
        {
            closestHit = leftHit;
            return true;
        }
        else if (hitRight)
        {
            closestHit = rightHit;
            return true;
        }

        return false;
    }
}

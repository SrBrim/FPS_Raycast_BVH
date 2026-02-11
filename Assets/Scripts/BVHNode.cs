using UnityEngine;

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

        if (!bounds.IntersectRay(ray))
            return false;

        if (IsLeaf)
        {
            MeshCollider col = leafMesh.GetComponent<MeshCollider>();
            if (col != null)
                return col.Raycast(ray, out closestHit, Mathf.Infinity);

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
}

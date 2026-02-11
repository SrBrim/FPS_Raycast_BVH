using UnityEngine;
using System.Collections.Generic;

public class EnemyBVH : MonoBehaviour
{
    public BVHNode root;

    private List<SkinnedMeshRenderer> meshes = new List<SkinnedMeshRenderer>();

    void Start()
    {
        meshes.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());

        Debug.Log("Skinned meshes encontrados: " + meshes.Count);

        root = BuildTree(meshes);
    }

    void Update()
    {
        if (root != null)
            UpdateNodeBounds(root);
    }

    BVHNode BuildTree(List<SkinnedMeshRenderer> meshList)
    {
        if (meshList.Count == 0)
            return null;

        Bounds b = meshList[0].bounds;
        foreach (var m in meshList)
            b.Encapsulate(m.bounds);

        BVHNode node = new BVHNode(b);

        if (meshList.Count == 1)
        {
            node.leafMesh = meshList[0];
            return node;
        }

        int mid = meshList.Count / 2;

        node.left = BuildTree(meshList.GetRange(0, mid));
        node.right = BuildTree(meshList.GetRange(mid, meshList.Count - mid));

        return node;
    }

    void UpdateNodeBounds(BVHNode node)
    {
        if (node == null) return;

        if (node.IsLeaf)
        {
            node.bounds = node.leafMesh.bounds;
        }
        else
        {
            if (node.left != null) UpdateNodeBounds(node.left);
            if (node.right != null) UpdateNodeBounds(node.right);

            Bounds b = node.left.bounds;
            if (node.right != null)
                b.Encapsulate(node.right.bounds);

            node.bounds = b;
        }
    }

    void OnDrawGizmos()
    {
        if (root == null) return;

        DrawNode(root, 0);
    }

    void DrawNode(BVHNode node, int depth)
    {
        if (node == null) return;

        if (depth == 0) Gizmos.color = Color.red;
        else if (node.IsLeaf) Gizmos.color = Color.green;
        else Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);

        DrawNode(node.left, depth + 1);
        DrawNode(node.right, depth + 1);
    }
}

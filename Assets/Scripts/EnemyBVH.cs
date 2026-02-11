using UnityEngine;
using System.Collections.Generic;

public class EnemyBVH : MonoBehaviour
{
    public BVHNode root;

    private List<SkinnedMeshRenderer> meshes = new List<SkinnedMeshRenderer>();

    // Para baking runtime dos colliders
    private List<Mesh> bakedMeshes = new List<Mesh>();
    private List<MeshCollider> targetColliders = new List<MeshCollider>();

    // Controle para reduzir custo: não rebake todo frame
    [Header("Runtime Baking")]
    public bool runtimeBake = true; // se falso, faz bake apenas no Start
    [Tooltip("Intervalo mínimo (seg) entre verificações de mudança nas meshes para rebake")]
    public float bakeInterval = 0.05f;
    [Tooltip("Threshold mínimo para considerar que os bounds mudaram (em unidades de dimensão)")]
    public float boundsChangeThreshold = 0.001f;

    private float bakeTimer = 0f;
    private List<Bounds> lastBounds = new List<Bounds>();

    void Start()
    {
        meshes.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());

        Debug.Log("Skinned meshes encontrados: " + meshes.Count);

        // Se quiser que o MeshCollider corresponda à malha animada,
        // vamos garantir que cada SkinnedMeshRenderer tenha um MeshCollider
        // com a mesh baked. Isso atualiza sharedMesh a cada frame no Update.
        bakedMeshes.Clear();
        targetColliders.Clear();
        lastBounds.Clear();

        foreach (var smr in meshes)
        {
            // cria/obtém MeshCollider no mesmo GameObject do SkinnedMeshRenderer
            MeshCollider col = smr.GetComponent<MeshCollider>();
            if (col == null)
                col = smr.gameObject.AddComponent<MeshCollider>();

            Mesh baked = new Mesh();
            smr.BakeMesh(baked);

            col.sharedMesh = baked;
            col.convex = false; // ajuste conforme necessidade (convex required para rigidbody convex)

            bakedMeshes.Add(baked);
            targetColliders.Add(col);

            // guarda bounds iniciais
            lastBounds.Add(smr.bounds);
        }

        root = BuildTree(meshes);

        // inicializa timer
        bakeTimer = bakeInterval;
    }

    void Update()
    {
        // Atualiza bounds do BVH antes de tudo
        if (root != null)
            UpdateNodeBounds(root);

        // Se não for para fazer baking runtime, pula atualizações caras
        if (!runtimeBake) return;

        // Throttle: só verifica mudanças a cada 'bakeInterval' segundos
        bakeTimer -= Time.deltaTime;
        if (bakeTimer > 0f) return;
        bakeTimer = bakeInterval;

        // Verifica cada mesh e rebake apenas se seus bounds mudaram o suficiente
        for (int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i] == null || bakedMeshes[i] == null || targetColliders[i] == null)
                continue;

            Bounds current = meshes[i].bounds;
            Bounds prev = lastBounds[i];

            bool significantChange =
                Vector3.Distance(current.center, prev.center) > boundsChangeThreshold ||
                Vector3.Distance(current.size, prev.size) > boundsChangeThreshold;

            if (!significantChange)
                continue;

            // Só aqui fazemos o bake e reatribuição ao collider (operação cara)
            meshes[i].BakeMesh(bakedMeshes[i]);
            targetColliders[i].sharedMesh = bakedMeshes[i];

            lastBounds[i] = current;

            // Debug leve para ajudar a diagnosticar
            Debug.Log($"Rebake mesh '{meshes[i].name}' em '{name}'");
        }
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

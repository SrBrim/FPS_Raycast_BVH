using UnityEngine;
using System.Collections.Generic;


public class BVHTraceVisualizer : MonoBehaviour
{
    private static BVHTraceVisualizer _instance;

    [Header("Display")]
    public Vector2 panelPos = new Vector2(10, 170);
    public Vector2 panelSize = new Vector2(420, 260);
    public int fontSize = 13;
    public float nodeSize = 22f;
    public float levelSpacing = 36f;

    [Header("Legend")]
    [Tooltip("Tamanho da fonte usada nas entradas da legenda")]
    public int legendFontSize = 14;
    [Tooltip("Tamanho da caixa colorida na legenda (largura, altura)")]
    public Vector2 legendSwatchSize = new Vector2(18f, 12f);

    
    private List<BVHNode> visited = new List<BVHNode>();
    private List<BVHNode> passed = new List<BVHNode>();
    private List<BVHNode> hitLeaves = new List<BVHNode>();
    private List<MeshCollider> hitColliders = new List<MeshCollider>();

    
    private BVHNode rootNode = null;

    private List<BVHNode> allNodes = new List<BVHNode>();

    private List<BVHNode> pathNodes = new List<BVHNode>();

    private float expireAt = 0f;
    public float displayDuration;

    private static Texture2D _whiteTex;

    public static BVHTraceVisualizer Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("BVHTraceVisualizer");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<BVHTraceVisualizer>();
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static void ShowTraceForRoot(BVHNode root, List<BVHNode> visitedNodes, List<BVHNode> passedAABB, List<BVHNode> hitLeafNodes, List<MeshCollider> hitCollidersList, float duration = 30f)
    {
        Instance.SetTraceRoot(root, visitedNodes, passedAABB, hitLeafNodes, hitCollidersList, duration);
    }

    public void SetTraceRoot(BVHNode root, List<BVHNode> visitedNodes, List<BVHNode> passedAABB, List<BVHNode> hitLeafNodes, List<MeshCollider> hitCollidersList, float duration)
    {
        rootNode = root;

        
        allNodes.Clear();
        pathNodes.Clear();

        if (rootNode != null)
        {
            Queue<BVHNode> q = new Queue<BVHNode>();
            Dictionary<BVHNode, BVHNode> parent = new Dictionary<BVHNode, BVHNode>();
            HashSet<BVHNode> seen = new HashSet<BVHNode>();
            q.Enqueue(rootNode);
            seen.Add(rootNode);
            parent[rootNode] = null;
            while (q.Count > 0)
            {
                var n = q.Dequeue();
                allNodes.Add(n);
                if (n.left != null && !seen.Contains(n.left)) { q.Enqueue(n.left); seen.Add(n.left); parent[n.left] = n; }
                if (n.right != null && !seen.Contains(n.right)) { q.Enqueue(n.right); seen.Add(n.right); parent[n.right] = n; }
            }

            
            HashSet<BVHNode> allSet = new HashSet<BVHNode>(allNodes);

            visited = visitedNodes != null ? new List<BVHNode>(visitedNodes) : new List<BVHNode>();
            visited.RemoveAll(n => n == null || !allSet.Contains(n));

            passed = passedAABB != null ? new List<BVHNode>(passedAABB) : new List<BVHNode>();
            passed.RemoveAll(n => n == null || !allSet.Contains(n));

            
            hitLeaves = hitLeafNodes != null ? new List<BVHNode>(hitLeafNodes) : new List<BVHNode>();
            hitLeaves.RemoveAll(n => n == null || !allSet.Contains(n) || !n.IsLeaf);

            
            hitColliders = hitCollidersList != null ? new List<MeshCollider>(hitCollidersList) : new List<MeshCollider>();
            hitColliders.RemoveAll(c => c == null);

            
            visited = new List<BVHNode>(new HashSet<BVHNode>(visited));
            passed = new List<BVHNode>(new HashSet<BVHNode>(passed));
            hitLeaves = new List<BVHNode>(new HashSet<BVHNode>(hitLeaves));

           
            if (hitLeaves != null && hitLeaves.Count > 0)
            {
                var leaf = hitLeaves[0];
                BVHNode cur = leaf;
                while (cur != null)
                {
                    pathNodes.Insert(0, cur);
                    if (!parent.TryGetValue(cur, out cur)) break;
                }
            }
            else if (hitColliders != null && hitColliders.Count > 0)
            {
                
                var col = hitColliders[0];
                var smr = col.GetComponentInParent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    
                    BVHNode match = allNodes.Find(n => n.IsLeaf && n.leafMesh == smr);
                    if (match != null)
                    {
                        BVHNode cur = match;
                        while (cur != null)
                        {
                            pathNodes.Insert(0, cur);
                            if (!parent.TryGetValue(cur, out cur)) break;
                        }

                        // marque a leaf
                        if (!hitLeaves.Contains(match)) hitLeaves.Add(match);
                    }
                }
            }
        }
        else
        {
            visited = new List<BVHNode>();
            passed = new List<BVHNode>();
            hitLeaves = new List<BVHNode>();
            hitColliders = new List<MeshCollider>();
        }

        expireAt = Time.realtimeSinceStartup + displayDuration;
    }

    void EnsureWhiteTex()
    {
        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }
    }

    void Update()
    {
        
        if (Time.realtimeSinceStartup > expireAt) return;
        if (rootNode == null) return;

        
        foreach (var n in allNodes)
        {
            Color col = Color.gray;
            if (pathNodes.Contains(n)) col = new Color(1f, 0.2f, 0.2f); 
            else if (hitLeaves.Contains(n)) col = new Color(0f, 1f, 0.2f); 
            else if (passed.Contains(n)) col = new Color(1f, 0.85f, 0f); 
            else if (visited.Contains(n)) col = new Color(0.2f, 1f, 1f); 

            DrawDebugBounds(n.bounds, col, 0f); 
        }

        
        foreach (var col in hitColliders)
        {
            if (col == null) continue;
            var b = col.sharedMesh != null ? col.sharedMesh.bounds : new Bounds(col.transform.position, Vector3.one * 0.1f);
            
            Matrix4x4 m = col.transform.localToWorldMatrix;
            Vector3 center = m.MultiplyPoint3x4(b.center);
            Vector3 extents = Vector3.Scale(b.extents, col.transform.lossyScale);
            DrawDebugBounds(new Bounds(center, extents * 2f), new Color(0f, 1f, 0.2f), 0f);
        }

        
        foreach (var n in allNodes)
        {
            if (n.left != null)
                Debug.DrawLine(n.bounds.center, n.left.bounds.center, Color.white);
            if (n.right != null)
                Debug.DrawLine(n.bounds.center, n.right.bounds.center, Color.white);
        }

        
        for (int i = 0; i < pathNodes.Count - 1; i++)
        {
            Debug.DrawLine(pathNodes[i].bounds.center, pathNodes[i + 1].bounds.center, Color.red);
        }
    }

    void OnGUI()
    {
        if (Time.realtimeSinceStartup > expireAt) return;

        EnsureWhiteTex();

        Rect panelRect = new Rect(panelPos.x, panelPos.y, panelSize.x, panelSize.y);
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.wordWrap = true;

        GUI.Box(panelRect, "BVH Trace", boxStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 22, panelRect.width - 16, panelRect.height - 28));

        GUIStyle label = new GUIStyle(GUI.skin.label);
        label.fontSize = fontSize;

        GUILayout.Label($"Visited nodes: {visited.Count}", label);
        GUILayout.Label($"Passed AABBs: {passed.Count}", label);
        GUILayout.Label($"Hit leaves: {hitLeaves.Count}", label);

        GUILayout.Space(6);

        if (rootNode != null && allNodes.Count > 0)
        {
            DrawTree(panelRect);
            GUILayout.Space(6);

            // legenda
            GUILayout.Label("Legenda:", label);
            DrawLegendEntry("Caminho (root -> hit)", new Color(1f, 0.2f, 0.2f));
            DrawLegendEntry("Hit (leaf)", new Color(0f, 1f, 0.2f));
            DrawLegendEntry("Hit collider (exato)", new Color(0f, 0.8f, 0f));
            DrawLegendEntry("Passed AABB", new Color(1f, 0.85f, 0f));
            DrawLegendEntry("Visited", new Color(0.2f, 1f, 1f));
            DrawLegendEntry("Outros nós", Color.gray);
        }

        GUILayout.EndArea();
    }

    void DrawLegendEntry(string text, Color col)
    {
        Rect r = GUILayoutUtility.GetRect(200, 20);
        Rect cbox = new Rect(r.x, r.y + 4, legendSwatchSize.x, legendSwatchSize.y);
        Rect lbox = new Rect(r.x + legendSwatchSize.x + 6, r.y, r.width - (legendSwatchSize.x + 6), 20);
        DrawFilledRect(cbox, col);

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = legendFontSize;
        style.alignment = TextAnchor.MiddleLeft;
        GUI.Label(lbox, text, style);
    }

    void DrawTree(Rect panelRect)
    {
        
        Dictionary<int, List<BVHNode>> byDepth = new Dictionary<int, List<BVHNode>>();
        Queue<(BVHNode node, int depth)> q2 = new Queue<(BVHNode, int)>();
        q2.Enqueue((rootNode, 0));
        HashSet<BVHNode> seen2 = new HashSet<BVHNode>();
        seen2.Add(rootNode);
        int maxDepth = 0;
        while (q2.Count > 0)
        {
            var (n, d) = q2.Dequeue();
            if (!byDepth.ContainsKey(d)) byDepth[d] = new List<BVHNode>();
            byDepth[d].Add(n);
            if (d > maxDepth) maxDepth = d;
            if (n.left != null && !seen2.Contains(n.left)) { q2.Enqueue((n.left, d + 1)); seen2.Add(n.left); }
            if (n.right != null && !seen2.Contains(n.right)) { q2.Enqueue((n.right, d + 1)); seen2.Add(n.right); }
        }

        
        Dictionary<BVHNode, Vector2> positions = new Dictionary<BVHNode, Vector2>();
        float areaX = panelRect.x + 12;
        float areaY = panelRect.y + 80;
        float areaW = panelRect.width - 40;

        for (int depth = 0; depth <= maxDepth; depth++)
        {
            if (!byDepth.ContainsKey(depth)) continue;
            var list = byDepth[depth];
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                float x = areaX + (i + 1) * (areaW / (count + 1));
                float y = areaY + depth * levelSpacing;
                positions[list[i]] = new Vector2(x, y);
            }
        }

        
        foreach (var kv in positions)
        {
            BVHNode n = kv.Key;
            Vector2 p = kv.Value;
            if (n.left != null && positions.ContainsKey(n.left))
            {
                DrawLine(p, positions[n.left], Color.white, 2f);
            }
            if (n.right != null && positions.ContainsKey(n.right))
            {
                DrawLine(p, positions[n.right], Color.white, 2f);
            }
        }

        
        foreach (var kv in positions)
        {
            BVHNode n = kv.Key;
            Vector2 p = kv.Value;
            Rect r = new Rect(p.x - nodeSize / 2f, p.y - nodeSize / 2f, nodeSize, nodeSize);

            Color c = Color.gray;
            if (pathNodes.Contains(n)) c = new Color(1f, 0.2f, 0.2f);
            else if (hitLeaves.Contains(n)) c = new Color(0f, 1f, 0.2f);
            else if (passed.Contains(n)) c = new Color(1f, 0.85f, 0f);
            else if (visited.Contains(n)) c = new Color(0.2f, 1f, 1f);

            DrawFilledRect(r, c);

            GUIStyle lbl = new GUIStyle(GUI.skin.label);
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.normal.textColor = Color.black;
            lbl.fontSize = Mathf.Clamp(fontSize, 10, 18);
            GUI.Label(r, n.IsLeaf ? "L" : "N", lbl);
        }
    }

    void DrawFilledRect(Rect r, Color col)
    {
        Color old = GUI.color;
        GUI.color = col;
        GUI.DrawTexture(r, _whiteTex);
        GUI.color = old;
    }

    void DrawLine(Vector2 a, Vector2 b, Color col, float width)
    {
        Color oldColor = GUI.color;
        Matrix4x4 savedMatrix = GUI.matrix;

        Vector2 delta = b - a;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float len = delta.magnitude;

        GUI.color = col;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width / 2f, len, width), _whiteTex);
        GUI.matrix = savedMatrix;
        GUI.color = oldColor;
    }

    void DrawDebugBounds(Bounds b, Color col, float duration)
    {
        Vector3 c = b.center;
        Vector3 e = b.extents;

        
        Vector3[] corners = new Vector3[8];
        corners[0] = c + new Vector3(-e.x, -e.y, -e.z);
        corners[1] = c + new Vector3(e.x, -e.y, -e.z);
        corners[2] = c + new Vector3(e.x, -e.y, e.z);
        corners[3] = c + new Vector3(-e.x, -e.y, e.z);
        corners[4] = c + new Vector3(-e.x, e.y, -e.z);
        corners[5] = c + new Vector3(e.x, e.y, -e.z);
        corners[6] = c + new Vector3(e.x, e.y, e.z);
        corners[7] = c + new Vector3(-e.x, e.y, e.z);

        
        Debug.DrawLine(corners[0], corners[1], col, duration);
        Debug.DrawLine(corners[1], corners[2], col, duration);
        Debug.DrawLine(corners[2], corners[3], col, duration);
        Debug.DrawLine(corners[3], corners[0], col, duration);

       
        Debug.DrawLine(corners[4], corners[5], col, duration);
        Debug.DrawLine(corners[5], corners[6], col, duration);
        Debug.DrawLine(corners[6], corners[7], col, duration);
        Debug.DrawLine(corners[7], corners[4], col, duration);

        
        Debug.DrawLine(corners[0], corners[4], col, duration);
        Debug.DrawLine(corners[1], corners[5], col, duration);
        Debug.DrawLine(corners[2], corners[6], col, duration);
        Debug.DrawLine(corners[3], corners[7], col, duration);
    }

    void OnDrawGizmos()
    {
        if (rootNode == null) return;

        // desenha todos os bounds
        HashSet<BVHNode> visitedSet = new HashSet<BVHNode>(visited);
        HashSet<BVHNode> passedSet = new HashSet<BVHNode>(passed);
        HashSet<BVHNode> hitSet = new HashSet<BVHNode>(hitLeaves);

        foreach (var n in allNodes)
        {
            if (n == null) continue;
            Color c = Color.gray;
            if (pathNodes.Contains(n)) c = new Color(1f, 0.2f, 0.2f);
            else if (hitSet.Contains(n)) c = new Color(0f, 1f, 0.2f);
            else if (passedSet.Contains(n)) c = new Color(1f, 0.85f, 0f);
            else if (visitedSet.Contains(n)) c = new Color(0.2f, 1f, 1f);

            Gizmos.color = c;
            Gizmos.DrawWireCube(n.bounds.center, n.bounds.size);

            if (n.left != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(n.bounds.center, n.left.bounds.center);
            }
            if (n.right != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(n.bounds.center, n.right.bounds.center);
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

// Visualizador simples da árvore BVH no HUD quando um inimigo é atingido/detectado.
// Funciona assim:
// - Chame BVHTraceVisualizer.ShowTrace(...) passando listas de nós visitados/passatados/folhas atingidas.
// - O visualizador desenha um painel com uma árvore simplificada e destaca o caminho escolhido.

public class BVHTraceVisualizer : MonoBehaviour
{
    private static BVHTraceVisualizer _instance;

    [Header("Display")]
    public Vector2 panelPos = new Vector2(10, 170);
    public Vector2 panelSize = new Vector2(420, 260);
    public int fontSize = 13;
    public float nodeSize = 22f;
    public float levelSpacing = 36f;

    // Trace data
    private List<BVHNode> visited = new List<BVHNode>();
    private List<BVHNode> passed = new List<BVHNode>();
    private List<BVHNode> hitLeaves = new List<BVHNode>();

    private float expireAt = 0f;
    public float displayDuration = 5f;

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

    public static void ShowTrace(List<BVHNode> visitedNodes, List<BVHNode> passedAABB, List<BVHNode> hitLeafNodes, float duration = 5f)
    {
        Instance.SetTrace(visitedNodes, passedAABB, hitLeafNodes, duration);
    }

    public void SetTrace(List<BVHNode> visitedNodes, List<BVHNode> passedAABB, List<BVHNode> hitLeafNodes, float duration)
    {
        visited = visitedNodes != null ? new List<BVHNode>(visitedNodes) : new List<BVHNode>();
        passed = passedAABB != null ? new List<BVHNode>(passedAABB) : new List<BVHNode>();
        hitLeaves = hitLeafNodes != null ? new List<BVHNode>(hitLeafNodes) : new List<BVHNode>();
        expireAt = Time.realtimeSinceStartup + duration;
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

        if (visited.Count > 0)
        {
            DrawTree(panelRect);
        }

        GUILayout.EndArea();
    }

    void DrawTree(Rect panelRect)
    {
        // Constrói conjunto dos nós relevantes (visitados)
        HashSet<BVHNode> nodesSet = new HashSet<BVHNode>(visited);

        BVHNode root = visited[0];

        // BFS para agrupar por profundidade, apenas incluindo nós que estão no nodesSet
        Dictionary<int, List<BVHNode>> byDepth = new Dictionary<int, List<BVHNode>>();
        Queue<(BVHNode node, int depth)> q = new Queue<(BVHNode, int)>();
        HashSet<BVHNode> seen = new HashSet<BVHNode>();
        q.Enqueue((root, 0));
        seen.Add(root);

        int maxDepth = 0;
        while (q.Count > 0)
        {
            var (n, d) = q.Dequeue();
            if (!nodesSet.Contains(n)) continue;

            if (!byDepth.ContainsKey(d)) byDepth[d] = new List<BVHNode>();
            byDepth[d].Add(n);
            if (d > maxDepth) maxDepth = d;

            if (n.left != null && !seen.Contains(n.left)) { q.Enqueue((n.left, d + 1)); seen.Add(n.left); }
            if (n.right != null && !seen.Contains(n.right)) { q.Enqueue((n.right, d + 1)); seen.Add(n.right); }
        }

        // Calcula posições
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

        // Desenha linhas entre pai e filho (se ambos presentes)
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

        // Desenha nós
        foreach (var kv in positions)
        {
            BVHNode n = kv.Key;
            Vector2 p = kv.Value;
            Rect r = new Rect(p.x - nodeSize / 2f, p.y - nodeSize / 2f, nodeSize, nodeSize);

            Color c = Color.gray;
            if (hitLeaves.Contains(n)) c = Color.green;
            else if (passed.Contains(n)) c = Color.yellow;
            else if (visited.Contains(n)) c = Color.cyan;

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
}

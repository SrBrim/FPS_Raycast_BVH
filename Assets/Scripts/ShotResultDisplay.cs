using UnityEngine;



public class ShotResultDisplay : MonoBehaviour
{
    private static ShotResultDisplay _instance;

    [Header("Display")]
    public float displayDuration = 5f;
    public Vector2 panelSize = new Vector2(360f, 150f);
    public int fontSize = 14;

    public enum Anchor { TopLeft, TopRight, BottomLeft, BottomRight }
    [Tooltip("Escolha o canto de ancoragem do painel")] public Anchor anchor = Anchor.TopRight;
    [Tooltip("Offset a partir do canto selecionado (em pixels)")] public Vector2 panelOffset = new Vector2(10f, 10f);

   
    private double timeWithout;
    private double timeWith;
    private int bruteTests;
    private int bvhRootTests;
    private string hitName;
    private Vector3 hitPoint;
    private bool didHit;
    private float expireAt = 0f;

    
    public static ShotResultDisplay Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ShotResultDisplay");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ShotResultDisplay>();
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

   
    public void ShowResult(double timeWithout, double timeWith, int bruteTests, int bvhRootTests, string hitName, Vector3 hitPoint, bool didHit)
    {
        this.timeWithout = timeWithout;
        this.timeWith = timeWith;
        this.bruteTests = bruteTests;
        this.bvhRootTests = bvhRootTests;
        this.hitName = string.IsNullOrEmpty(hitName) ? "(nenhum)" : hitName;
        this.hitPoint = hitPoint;
        this.didHit = didHit;
        this.expireAt = Time.realtimeSinceStartup + displayDuration;
    }

    
    public static void Show(double timeWithout, double timeWith, int bruteTests, int bvhRootTests, string hitName, Vector3 hitPoint, bool didHit)
    {
        Instance.ShowResult(timeWithout, timeWith, bruteTests, bvhRootTests, hitName, hitPoint, didHit);
    }

    void OnGUI()
    {
        if (Time.realtimeSinceStartup > expireAt) return;

        
        float x = 0f;
        float y = 0f;

        switch (anchor)
        {
            default:
            case Anchor.TopLeft:
                x = panelOffset.x;
                y = panelOffset.y;
                break;
            case Anchor.TopRight:
                x = Screen.width - panelOffset.x - panelSize.x;
                y = panelOffset.y;
                break;
            case Anchor.BottomLeft:
                x = panelOffset.x;
                y = Screen.height - panelOffset.y - panelSize.y;
                break;
            case Anchor.BottomRight:
                x = Screen.width - panelOffset.x - panelSize.x;
                y = Screen.height - panelOffset.y - panelSize.y;
                break;
        }

        Rect panelRect = new Rect(x, y, panelSize.x, panelSize.y);

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.alignment = TextAnchor.UpperLeft;
        panelStyle.wordWrap = true;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;
        labelStyle.richText = true;

        GUI.Box(panelRect, "Resultados do Tiro", panelStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 22, panelRect.width - 16, panelRect.height - 28));

        GUILayout.Label($"Tempo (sem BVH): <b>{timeWithout.ToString("F4")} ms</b>", labelStyle);
        GUILayout.Label($"Tempo (com BVH): <b>{timeWith.ToString("F4")} ms</b>", labelStyle);

        double speedupPct = 0.0;
        double ratio = 0.0;
        if (timeWithout > 0.0)
        {
            speedupPct = (1.0 - (timeWith / timeWithout)) * 100.0;
            if (timeWith > 0.0) ratio = timeWithout / timeWith;
        }

        GUILayout.Label($"Redução (speedup): <b>{speedupPct.ToString("F2")}%</b> (x{ratio.ToString("F2")})", labelStyle);

        GUILayout.Space(6);

        GUILayout.Label($"Testes força bruta (mesh): <b>{bruteTests}</b>", labelStyle);
        GUILayout.Label($"Testes BVH (raiz): <b>{bvhRootTests}</b>", labelStyle);

        GUILayout.Space(6);

        GUILayout.Label($"Acertou: <b>{(didHit ? "Sim" : "Não")}</b>", labelStyle);
        if (didHit)
        {
            GUILayout.Label($"Alvo: <b>{hitName}</b>", labelStyle);
            GUILayout.Label($"Ponto: <b>{hitPoint.x.ToString("F3")}, {hitPoint.y.ToString("F3")}, {hitPoint.z.ToString("F3")}</b>", labelStyle);
        }

        GUILayout.EndArea();
    }
}

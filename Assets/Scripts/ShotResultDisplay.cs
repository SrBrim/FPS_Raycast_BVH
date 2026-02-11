using UnityEngine;

// Simples painel para exibir o resultado do tiro de forma legível.
// Uso:
// ShotResultDisplay.Show(timeWithout, timeWith, bruteTests, bvhRootTests, hitName, hitPoint, didHit);

public class ShotResultDisplay : MonoBehaviour
{
    private static ShotResultDisplay _instance;

    [Header("Display")]
    public float displayDuration = 5f;
    public Vector2 panelSize = new Vector2(360f, 150f);
    public int fontSize = 14;

    // Dados do último resultado
    private double timeWithout;
    private double timeWith;
    private int bruteTests;
    private int bvhRootTests;
    private string hitName;
    private Vector3 hitPoint;
    private bool didHit;
    private float expireAt = 0f;

    // Acesso rápido
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

    // Método público simples para mostrar os dados
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

    // Static helper para chamar de qualquer lugar
    public static void Show(double timeWithout, double timeWith, int bruteTests, int bvhRootTests, string hitName, Vector3 hitPoint, bool didHit)
    {
        Instance.ShowResult(timeWithout, timeWith, bruteTests, bvhRootTests, hitName, hitPoint, didHit);
    }

    void OnGUI()
    {
        if (Time.realtimeSinceStartup > expireAt) return;

        // Painel centralizado no topo
        float x = 10f;
        float y = 10f;

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

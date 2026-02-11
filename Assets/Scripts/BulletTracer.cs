using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    public float duration = 0.08f;
    public AnimationCurve widthCurve;

    private LineRenderer lr;
    private float timer;
    private Material instanceMat;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        timer = duration;

        // garante uma curva padrão se não for configurada
        if (widthCurve == null)
            widthCurve = AnimationCurve.Linear(0f, 0.05f, 1f, 0f);

        lr.widthCurve = widthCurve;

        // cache do material para evitar acessar shared material a cada frame
        instanceMat = lr.material;
    }

    public void SetPositions(Vector3 start, Vector3 end)
    {
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    void Update()
    {
        timer -= Time.deltaTime;

        float alpha = Mathf.Clamp01(timer / duration);
        if (instanceMat != null)
        {
            Color c = instanceMat.color;
            c.a = alpha;
            instanceMat.color = c;
        }

        if (timer <= 0f)
            Destroy(gameObject);
    }
}

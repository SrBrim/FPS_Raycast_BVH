using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    public float duration = 0.08f;
    public AnimationCurve widthCurve;

    private LineRenderer lr;
    private float timer;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        timer = duration;

        if (widthCurve != null)
            lr.widthCurve = widthCurve;
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

        float alpha = timer / duration;
        Color c = lr.material.color;
        c.a = alpha;
        lr.material.color = c;

        if (timer <= 0f)
            Destroy(gameObject);
    }
}

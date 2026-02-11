using UnityEngine;
using UnityEngine.InputSystem;

public class FPSMove : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        if (Keyboard.current == null) return;

        float h = 0f;
        float v = 0f;

        if (Keyboard.current.aKey.isPressed) h -= 1f;
        if (Keyboard.current.dKey.isPressed) h += 1f;
        if (Keyboard.current.wKey.isPressed) v += 1f;
        if (Keyboard.current.sKey.isPressed) v -= 1f;

        Vector3 move = transform.right * h + transform.forward * v;
        transform.position += move * speed * Time.deltaTime;
    }
}

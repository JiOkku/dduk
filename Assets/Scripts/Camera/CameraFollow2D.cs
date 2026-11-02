using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Follow the interpolated render position after movement updates.
        transform.position = target.position + offset;
    }
}

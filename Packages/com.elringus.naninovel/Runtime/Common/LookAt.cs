using Naninovel;
using UnityEngine;

/// <summary>
/// A generic helper to make host transform look at the specified target.
/// </summary>
[AddComponentMenu("Naninovel/ UI/Look At")]
[DisallowMultipleComponent]
public class LookAt : MonoBehaviour
{
    [Tooltip("Tag of a game object to look at. Has to be available on scene when the component is enabled."), GameObjectTag]
    public string TargetTag;
    [Min(0), Tooltip("Rotation smoothing speed in degrees per second. Set to 0 for instant rotation.")]
    public float Speed;
    [Tooltip("Up vector to stabilize the rotation.")]
    public Vector3 UpAxis = Vector3.up;

    private Transform target;

    private void OnEnable ()
    {
        if (GameObject.FindGameObjectWithTag(TargetTag) is { } obj && obj)
            target = obj.transform;
    }

    private void LateUpdate ()
    {
        if (!target) return;
        var rotation = Quaternion.LookRotation(transform.position - target.position, UpAxis);
        if (Speed <= 0f) transform.rotation = rotation;
        else transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, Speed * Time.deltaTime);
    }
}

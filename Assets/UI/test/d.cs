using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class TrapDisable : MonoBehaviour
{
    void OnDisable ()
    {
        Debug.LogError($"[TrapDisable] {name} disabled at:\n{System.Environment.StackTrace}", this);
    }
}

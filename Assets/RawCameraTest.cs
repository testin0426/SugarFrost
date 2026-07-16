using System.Collections;
using UnityEngine;

public class RawCameraTest : MonoBehaviour
{
    public Transform lookContainer;
    public Vector3 targetOffset = new Vector3(5, 0, 0);
    public float duration = 3f;
    public float startDelay = 2f; // wait for Naninovel to init

    private float elapsed;
    private Vector3 startOffset;
    private bool running;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);

        if (lookContainer == null)
        {
            // Search all transforms for the LookContainer
            var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t.name.Contains("LookContainer") || t.name.Contains("Camera"))
                    Debug.Log($"[RawCameraTest] Found: {t.name} at {t.position} (parent={t.parent?.name})");
            }

            // Try to find Naninovel's LookContainer by hierarchy path
            var cm = GameObject.Find("CameraManager");
            if (cm)
            {
                lookContainer = cm.transform.Find("MainCameraLookContainer");
                Debug.Log($"[RawCameraTest] Found via CameraManager: {lookContainer?.name ?? "NULL"}");
            }
            if (!lookContainer)
            {
                // Last resort: find any active camera that isn't UI
                foreach (var cam in Camera.allCameras)
                {
                    if (cam.enabled && cam.targetTexture == null && cam.name.ToLower().Contains("main"))
                    {
                        // Move the camera's parent if it has one
                        lookContainer = cam.transform.parent;
                        Debug.Log($"[RawCameraTest] Using camera parent: {lookContainer?.name ?? "NULL"} for camera {cam.name}");
                        break;
                    }
                }
            }
        }

        if (lookContainer == null)
        {
            Debug.LogError("[RawCameraTest] No LookContainer found! Drag one in the Inspector.");
            yield break;
        }

        var initialPos = new Vector3(0, 0, -10);
        startOffset = lookContainer.position - initialPos;
        running = true;
        Debug.Log($"[RawCameraTest] Starting pan from offset={startOffset} to {targetOffset} over {duration}s. LookContainer={lookContainer.name}");
    }

    void Update()
    {
        if (!running) return;

        elapsed += Time.deltaTime;
        var t = Mathf.Clamp01(elapsed / duration);
        var current = Vector3.Lerp(startOffset, targetOffset, t);
        var initialPos = new Vector3(0, 0, -10);
        lookContainer.position = initialPos + current;

        if (t >= 1f)
        {
            running = false;
            Debug.Log($"[RawCameraTest] Done. Final position={lookContainer.position}");
        }
    }
}

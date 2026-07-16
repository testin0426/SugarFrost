using UnityEngine;
using UnityEngine.Profiling;

// Lightweight on-screen RAM/FPS readout. Add to any GameObject in a scene.
// Works in the Editor AND in a real build (unlike the Profiler window).
// Shows: total memory Unity has allocated, the reserved pool, mono (C#) heap,
// and current FPS. Toggle with F3.
public class MemoryHud : MonoBehaviour
{
    private bool _show = true;
    private float _fps;
    private GUIStyle _style;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3)) _show = !_show;
        _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f), 0.1f);
    }

    private void OnGUI()
    {
        if (!_show) return;

        if (_style == null)
            _style = new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } };

        long totalMb = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        long reservedMb = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
        long monoMb = Profiler.GetMonoUsedSizeLong() / (1024 * 1024);

        string text =
            $"FPS: {_fps:0}\n" +
            $"Total allocated: {totalMb} MB\n" +
            $"Reserved (pool): {reservedMb} MB\n" +
            $"Mono C# heap:    {monoMb} MB\n" +
            "(F3 to hide)";

        GUI.Box(new Rect(10, 10, 260, 120), GUIContent.none);
        GUI.Label(new Rect(20, 16, 260, 120), text, _style);
    }
}

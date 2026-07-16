#if TIMELINE_AVAILABLE

using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace Naninovel
{
    [CustomTimelineEditor(typeof(PlayScriptMarker))]
    public class PlayScriptMarkerEditor : MarkerEditor
    {
        public override void DrawOverlay (IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
        {
            if (marker is not PlayScriptMarker) return;
            DrawLine(region);
        }

        private static void DrawLine (MarkerOverlayRegion region)
        {
            var rect = new Rect(region.markerRegion.x,
                region.timelineRegion.y,
                region.markerRegion.width,
                region.timelineRegion.height);
            var color = new Color(0f, 0.45f, 1f, 0.1f);
            EditorGUI.DrawRect(rect, color);
        }
    }
}

#endif

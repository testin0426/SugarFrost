using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class CameraSettings : ConfigurationSettings<CameraConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(CameraConfiguration.CustomUICameraPrefab)] = p => DrawWhen(Configuration.UseUICamera, p);
            drawers[nameof(CameraConfiguration.ThumbnailResolution)] = p => DrawWhen(Configuration.CaptureThumbnails, p);
            drawers[nameof(CameraConfiguration.HideUIInThumbnails)] = p => DrawWhen(Configuration.CaptureThumbnails, p);
            return drawers;
        }
    }
}

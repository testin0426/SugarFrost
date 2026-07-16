using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Serializable state of <see cref="IBackgroundActor"/>.
    /// </summary>
    [System.Serializable]
    public class BackgroundState : ActorState<IBackgroundActor>
    {
        public BackgroundState () { }
        public BackgroundState ([CanBeNull] string appearance = null, bool? visible = null, Vector3? position = null,
            Quaternion? rotation = null, Vector3? scale = null, Color? tintColor = null)
            : base(appearance, visible, position, rotation, scale, tintColor) { }
    }
}

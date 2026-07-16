using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Optional parameters for <see cref="Engine.Instantiate"/>.
    /// </summary>
    public struct InstantiateOptions
    {
        /// <summary>
        /// Name of the instantiated object.
        /// Will use the name of the prototype when not specified.
        /// </summary>
        public string Name;
        /// <summary>
        /// Layer of the instantiated object.
        /// Will use the layer set in the engine configuration when not specified.
        /// </summary>
        public int? Layer;
        /// <summary>
        /// When specified, will make the instantiated object child of the transform.
        /// </summary>
        public Transform Parent;
        /// <summary>
        /// World-space position of the instantiated object.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// World-space rotation of the instantiated object.
        /// </summary>
        public Quaternion Rotation;
        /// <summary>
        /// Local scale of the instantiated object.
        /// </summary>
        public Vector3? Scale;
    }
}

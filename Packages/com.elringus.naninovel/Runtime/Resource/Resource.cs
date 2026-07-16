using Object = UnityEngine.Object;

namespace Naninovel
{
    /// <summary>
    /// Represents a <see cref="UnityEngine.Object"/> associated with a unique identifier (resource path).
    /// </summary>
    public class Resource
    {
        /// <summary>
        /// A cached invalid resource.
        /// </summary>
        public static readonly Resource Invalid = new(null, null);

        /// <summary>
        /// Unique identifier of the resource.
        /// </summary>
        public readonly string FullPath;
        /// <summary>
        /// Associated Unity object of the resource.
        /// </summary>
        public readonly Object Object;
        /// <summary>
        /// Whether <see cref="Object"/> is a valid (not-destroyed) instance.
        /// </summary>
        public bool Valid => ObjectUtils.IsValid(Object);

        public Resource (string fullPath, Object obj)
        {
            FullPath = fullPath;
            Object = obj;
        }

        public Resource (string prefix, string path, Object obj)
        {
            FullPath = BuildFullPath(prefix, path);
            Object = obj;
        }

        /// <summary>
        /// Builds full resource path from the specified prefix and local path.
        /// </summary>
        public static string BuildFullPath (string prefix, string path)
        {
            return $"{prefix}/{path}";
        }

        public static implicit operator Object (Resource resource) => resource?.Object;

        public override string ToString () => $"Resource<{(Valid ? Object.GetType().Name : "INVALID")}>@{FullPath}";
    }

    /// <summary>
    /// A strongly typed <see cref="UnityEngine.Object"/>.
    /// </summary>
    public class Resource<TResource> : Resource
        where TResource : Object
    {
        /// <summary>
        /// A cached invalid resource.
        /// </summary>
        public new static readonly Resource<TResource> Invalid = new(null, null);

        /// <summary>
        /// Actual object (data) represented by the resource.
        /// </summary>
        public new TResource Object => CastObject(base.Object);

        public Resource (string fullPath, TResource obj) : base(fullPath, obj) { }
        public Resource (string prefix, string path, TResource obj) : base(prefix, path, obj) { }

        public static implicit operator TResource (Resource<TResource> r) => r?.Object;
        public static Resource<TResource> From (Resource r) => new(r.FullPath, (TResource)r.Object);

        private TResource CastObject (Object obj)
        {
            if (!Valid) return null;

            if (obj is not TResource castedObj)
                throw new Error($"Resource '{FullPath}' is not of type `{typeof(TResource).FullName}`.");

            return castedObj;
        }
    }
}

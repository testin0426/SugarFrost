// TODO: Remove in 1.22 (superseded by Assets)

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Naninovel
{
    /// <summary>
    /// Reference to a Unity asset registered as a Naninovel resource.
    /// </summary>
    [Serializable]
    public class EditorResource : IEquatable<EditorResource>
    {
        /// <summary>
        /// Unique global identifier of the associated Unity asset.
        /// </summary>
        public string Guid => guid;
        /// <summary>
        /// Local path of the resource.
        /// </summary>
        public string Path => name;
        /// <summary>
        /// Resource's type discriminator; prefixed to the local path to build the full path.
        /// </summary>
        public string Prefix => prefix;
        /// <summary>
        /// Full path of the resource.
        /// </summary>
        public string FullPath => $"{Prefix}/{Path}"; // don't cache, as unity sets prefix/name after this is accessed

        [FormerlySerializedAs("Name")]
        [SerializeField] private string name;
        [FormerlySerializedAs("PathPrefix"), FormerlySerializedAs("pathPrefix")]
        [SerializeField] private string prefix;
        [FormerlySerializedAs("Guid")]
        [SerializeField] private string guid;

        public EditorResource (string path, string prefix, string guid)
        {
            name = path;
            this.prefix = prefix;
            this.guid = guid;
        }

        public bool Equals (EditorResource other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return name == other.name && prefix == other.prefix && guid == other.guid;
        }

        public override bool Equals (object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EditorResource)obj);
        }

        public override int GetHashCode () => HashCode.Combine(Path, Prefix, Guid);
        public static bool operator == (EditorResource lhs, EditorResource rhs) => Equals(lhs, rhs);
        public static bool operator != (EditorResource lhs, EditorResource rhs) => !Equals(lhs, rhs);
    }
}

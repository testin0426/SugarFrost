using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Converts raw bytes into <see cref="Resource"/> objects of specific type.
    /// </summary>
    public interface IResourceConverter
    {
        /// <summary>
        /// Whether this converter supports specified file extension (including the dot prefix).
        /// </summary>
        bool Supports (string extension);
        /// <summary>
        /// Converts specified raw bytes into a resource object with the specified full path.
        /// </summary>
        Object Convert (byte[] bytes, string fullPath);
    }
}

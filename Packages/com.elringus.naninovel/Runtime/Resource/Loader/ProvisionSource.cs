using System;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Represents a <see cref="IResourceProvider"/> associated with a path prefix used to evaluate full path to the provider resources.
    /// </summary>
    public readonly struct ProvisionSource : IEquatable<ProvisionSource>
    {
        /// <summary>
        /// Provider associated with the source.
        /// </summary>
        public readonly IResourceProvider Provider;
        /// <summary>
        /// Path prefix to build full paths to the provider resources.
        /// </summary>
        public readonly string PathPrefix;

        public ProvisionSource (IResourceProvider provider, string pathPrefix)
        {
            Provider = provider;
            PathPrefix = pathPrefix;
        }

        /// <summary>
        /// Given a local path to the resource, builds full path using predefined <see cref="PathPrefix"/>.
        /// </summary>
        public static string BuildFullPath ([CanBeNull] string prefix, [CanBeNull] string localPath)
        {
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                if (!string.IsNullOrWhiteSpace(localPath))
                    return Resource.BuildFullPath(prefix, localPath);
                return prefix;
            }
            return localPath;
        }

        /// <summary>
        /// Given a full path to the resource, builds local path using predefined <see cref="PathPrefix"/>.
        /// </summary>
        public static string BuildLocalPath ([CanBeNull] string pathPrefix, [NotNull] string fullPath)
        {
            if (!string.IsNullOrWhiteSpace(pathPrefix))
            {
                var prefixAndSlash = $"{pathPrefix}/";
                if (!fullPath.Contains(prefixAndSlash))
                    throw new Error($"Failed to build local path from `{fullPath}`: the specified path doesn't contain `{pathPrefix}` path prefix.");
                return fullPath.GetAfterFirst(prefixAndSlash);
            }
            return fullPath;
        }

        /// <inheritdoc cref="BuildFullPath(string,string)"/>
        public string BuildFullPath (string localPath) => BuildFullPath(PathPrefix, localPath);
        /// <inheritdoc cref="BuildLocalPath(string,string)"/>
        public string BuildLocalPath (string fullPath) => BuildLocalPath(PathPrefix, fullPath);

        public bool Equals (ProvisionSource other) => Provider.Equals(other.Provider) && PathPrefix == other.PathPrefix;
        public override bool Equals (object obj) => obj is ProvisionSource other && Equals(other);
        public override int GetHashCode () => HashCode.Combine(Provider, PathPrefix);
    }
}

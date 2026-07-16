using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    [Serializable]
    public struct SpawnedObjectState : IEquatable<SpawnedObjectState>
    {
        public string Path => path;
        public IReadOnlyList<string> Parameters => parameters?.Select(s => s?.Value).ToArray();
        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public Vector3 Scale => scale;

        [SerializeField] private string path;
        [SerializeField] private NullableString[] parameters;
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation;
        [SerializeField] private Vector3 scale;

        public SpawnedObjectState (SpawnedObject obj)
        {
            path = obj.Path;
            parameters = obj.Parameters?.Select(s => (NullableString)s).ToArray();
            position = obj.GameObject.transform.position;
            rotation = obj.GameObject.transform.rotation;
            scale = obj.GameObject.transform.localScale;
        }

        public void ApplyTo (SpawnedObject obj)
        {
            if (!obj.Path.EqualsOrdinal(Path))
                throw new Error($"Failed to apply '{Path}' spawned object state to '{obj.Path}': paths are different.");
            obj.SetSpawnParameters(Parameters, true);
            obj.GameObject.transform.position = Position;
            obj.GameObject.transform.rotation = Rotation;
            obj.GameObject.transform.localScale = Scale;
        }

        public bool Equals (SpawnedObjectState other) => path == other.path;
        public override bool Equals (object obj) => obj is SpawnedObjectState other && Equals(other);
        public override int GetHashCode () => Path != null ? Path.GetHashCode() : 0;
        public static bool operator == (SpawnedObjectState left, SpawnedObjectState right) => left.Equals(right);
        public static bool operator != (SpawnedObjectState left, SpawnedObjectState right) => !left.Equals(right);
    }
}

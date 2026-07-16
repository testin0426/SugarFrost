using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Stores <see cref="Syntax.IdentifiedText"/> ID to text map
    /// of the associated <see cref="Script"/> asset.
    /// </summary>
    [Serializable]
    public class ScriptTextMap
    {
        [Serializable]
        public class SerializableTextMap : SerializableMap<string, string>
        {
            public SerializableTextMap () { }
            public SerializableTextMap (IReadOnlyDictionary<string, string> dictionary)
                : base(dictionary) { }
        }

        /// <summary>
        /// Identifiers to text dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string> Map => idToText;

        [SerializeField] private SerializableTextMap idToText;

        public ScriptTextMap (IReadOnlyDictionary<string, string> idToText)
        {
            this.idToText = new(idToText);
        }

        /// <summary>
        /// Attempts to retrieve text value associated with the specified ID;
        /// returns null in case the ID is not found.
        /// </summary>
        public string GetTextOrNull (string id)
        {
            if (id.StartsWithOrdinal(ScriptTextIdentifier.RefPrefix)) id = id[1..];
            return idToText.GetValueOrDefault(id);
        }
    }
}

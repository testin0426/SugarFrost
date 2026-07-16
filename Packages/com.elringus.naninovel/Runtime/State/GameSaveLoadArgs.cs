using System;

namespace Naninovel
{
    /// <summary>
    /// Arguments associated with the game save and load events invoked by <see cref="IStateManager"/>. 
    /// </summary>
    public readonly struct GameSaveLoadArgs : IEquatable<GameSaveLoadArgs>
    {
        /// <summary>
        /// ID of the save slot the operation is associated with.
        /// </summary>
        public readonly string SlotId;
        /// <summary>
        /// The type of the save/load operation.
        /// </summary>
        public readonly SaveType Type;

        public GameSaveLoadArgs (string slotId, SaveType type)
        {
            SlotId = slotId;
            Type = type;
        }

        public bool Equals (GameSaveLoadArgs other)
        {
            return SlotId == other.SlotId && Type == other.Type;
        }
        public override bool Equals (object obj)
        {
            return obj is GameSaveLoadArgs other && Equals(other);
        }
        public override int GetHashCode ()
        {
            return HashCode.Combine(SlotId, Type);
        }
    }
}

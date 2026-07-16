using System;
using System.Globalization;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents serializable session-specific state of the engine services and related data (aka saved game status).
    /// </summary>
    [Serializable]
    public class GameStateMap : StateMap
    {
        /// <summary>
        /// State of <see cref="ScriptTrack.PlaybackSpot"/> of the main track at the time snapshot was taken;
        /// expected to be set by the player service on serialization.
        /// </summary>
        public PlaybackSpot PlaybackSpot { get => playbackSpot; set => playbackSpot = value; }
        /// <summary>
        /// Date and time when the snapshot was taken.
        /// </summary>
        public DateTime SaveDateTime { get; set; }
        /// <summary>
        /// Preview of the screen when the snapshot was taken, or null when thumbnails are disabled.
        /// </summary>
        [CanBeNull] public Texture2D Thumbnail { get; set; }
        /// <summary>
        /// Whether player is allowed rolling back to this snapshot; see remarks for more info.
        /// </summary>
        /// <remarks>
        /// Player expects rollback to occur between the points where they've interacted with the game to progress
        /// it further (clicked a printer to continue reading, picked up a choice, etc). This flag can be set before
        /// mutating game state after a meaningful player interaction to indicate that the snapshot can be used when
        /// handling "rollback" input.
        /// </remarks>
        public bool PlayerRollbackAllowed { get => playerRollbackAllowed; set => playerRollbackAllowed = value; }
        /// <summary>
        /// Whether this snapshot should always be serialized when saving the game,
        /// no matter if <see cref="PlayerRollbackAllowed"/>.
        /// </summary>
        public bool ForcedSerialize { get => forcedSerialize; set => forcedSerialize = value; }
        /// <summary>
        /// JSON representation of the rollback stack when the snapshot was taken.
        /// </summary>
        [CanBeNull] public string RollbackStackJson { get => rollbackStackJson; set => rollbackStackJson = value; }
        /// <summary>
        /// The <see cref="Script.Hash"/> of the currently played script (on the main track) or empty when none.
        /// Used by <see cref="StateConfiguration.RecoveryRollback"/> to detect post-save played script changes.
        /// </summary>
        [CanBeNull] public string PlayedHash { get => playedHash; set => playedHash = value; }
        /// <summary>
        /// The game state used by <see cref="StateConfiguration.RecoveryRollback"/> to recover when loading
        /// after played script was modified.
        /// </summary>
        [CanBeNull] public string RecoveryJson { get => recoveryJson; set => recoveryJson = value; }

        private const string dateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        [SerializeField] private PlaybackSpot playbackSpot;
        [SerializeField] private bool playerRollbackAllowed;
        [SerializeField] private bool forcedSerialize;
        [SerializeField] private string saveDateTime;
        [SerializeField] private string thumbnailBase64;
        [SerializeField] private string rollbackStackJson;
        [SerializeField] private string playedHash;
        [SerializeField] private string recoveryJson;

        /// <inheritdoc cref="StateMap.With"/>
        public static GameStateMap With (params (object state, string id)[] records)
        {
            return StateMap.With<GameStateMap>(records);
        }

        public override void OnBeforeSerialize ()
        {
            base.OnBeforeSerialize();

            saveDateTime = SaveDateTime.ToString(dateTimeFormat, CultureInfo.InvariantCulture);
            thumbnailBase64 = Thumbnail ? Convert.ToBase64String(Thumbnail.EncodeToJPG()) : null;
        }

        public override void OnAfterDeserialize ()
        {
            base.OnAfterDeserialize();

            SaveDateTime = string.IsNullOrEmpty(saveDateTime)
                ? DateTime.MinValue
                : DateTime.ParseExact(saveDateTime, dateTimeFormat, CultureInfo.InvariantCulture);
            Thumbnail = string.IsNullOrEmpty(thumbnailBase64) ? null : GetThumbnail();
        }

        /// <summary>
        /// Allows this state snapshot to be used for player-driven state rollback.
        /// </summary>
        public void AllowPlayerRollback () => playerRollbackAllowed = true;

        /// <summary>
        /// Forces the snapshot to be serialized, regardless of <see cref="PlayerRollbackAllowed"/>.
        /// </summary>
        public void ForceSerialize () => forcedSerialize = true;

        private Texture2D GetThumbnail ()
        {
            var tex = new Texture2D(2, 2);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.LoadImage(Convert.FromBase64String(thumbnailBase64));
            return tex;
        }
    }
}

using System;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Configures directive callback of a <see cref="Choice"/>.
    /// </summary>
    [Serializable]
    public struct DirectiveChoiceCallback
    {
        /// <summary>
        /// Custom variable set expression to execute on choice.
        /// </summary>
        [CanBeNull] public string Set;
        /// <summary>
        /// Canonical script path to navigate the playback on choice.
        /// </summary>
        [CanBeNull] public string Goto;
        /// <summary>
        /// Canonical script path to execute a subroutine on choice.
        /// Ignored when <see cref="Goto"/> is specified.
        /// </summary>
        [CanBeNull] public string Gosub;
        /// <summary>
        /// Identifier of a <see cref="IScriptTrack"/> on which to execute the callback.
        /// Will execute on the <see cref="IScriptPlayer.MainTrack"/> when not specified.
        /// </summary>
        [CanBeNull] public string TrackId;
    }
}

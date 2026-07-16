namespace Naninovel.Commands
{
    /// <summary>
    /// A base implementation for the audio-related commands.
    /// </summary>
    public abstract class AudioCommand : Command
    {
        protected virtual IAudioManager AudioManager => Engine.GetServiceOrErr<IAudioManager>();
    }
}

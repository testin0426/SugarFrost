
namespace Naninovel
{
    /// <inheritdoc cref="IBackgroundManager"/>
    [InitializeAtRuntime]
    public class BackgroundManager : OrthoActorManager<IBackgroundActor, BackgroundState, BackgroundMetadata, BackgroundsConfiguration>, IBackgroundManager
    {
        public BackgroundManager (BackgroundsConfiguration cfg, CameraConfiguration cameraCfg)
            : base(cfg, cameraCfg) { }
    }
}

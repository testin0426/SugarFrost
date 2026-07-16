using System;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Plays a movie with the specified name (path).",
        @"
Will fade-out the screen before playing the movie and fade back in after the play.
Playback can be canceled by activating a `cancel` input (`Esc` key by default).",
        @"
; Given an 'Opening' video clip is added to the movie resources, plays it.
@movie Opening"
    )]
    [Serializable, Alias("movie"), VisualsGroup, Icon("Film")]
    public class PlayMovie : Command, Command.IPreloadable
    {
        [Doc("Local path of the movie resource to play.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ResourceContext(MoviesConfiguration.DefaultPathPrefix)]
        public StringParameter MoviePath;
        [Doc("Duration (in seconds) of the fade animation. When not specified, will use fade duration set in the movie configuration.")]
        [Alias("time")]
        public DecimalParameter Duration;
        [Doc("Whether to block interaction with the game while the movie is playing, preventing the player from skipping it.")]
        [Alias("block")]
        public BooleanParameter BlockInteraction;

        protected virtual IMoviePlayer Player => Engine.GetServiceOrErr<IMoviePlayer>();

        public virtual async Awaitable PreloadResources ()
        {
            if (!Assigned(MoviePath) || MoviePath.DynamicValue) return;
            await Player.HoldResources(MoviePath, this);
        }

        public virtual void ReleaseResources ()
        {
            if (!Assigned(MoviePath) || MoviePath.DynamicValue) return;
            Player?.ReleaseResources(MoviePath, this);
        }

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var movieUI = Engine.GetService<IUIManager>()?.GetUI<IMovieUI>();
            if (movieUI is null) return;

            var blocker = BlockInteraction ? new InteractionBlocker() : null;

            var fadeDuration = Assigned(Duration) ? Duration.Value : Player.Configuration.FadeDuration;
            await movieUI.ChangeVisibility(true, fadeDuration, ctx.Token);

            var movieTexture = await Player.Play(MoviePath, ctx.Token);
            movieUI.SetMovieTexture(movieTexture);

            while (Player.Playing)
                await Async.NextFrame(ctx.Token);

            blocker?.Dispose();

            await movieUI.ChangeVisibility(false, fadeDuration, ctx.Token);
        }
    }
}

using System.Globalization;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Base class for FX spawn commands (@shake, @rain, @bokeh, etc).
    /// </summary>
    public abstract class SpawnEffect : Command, Command.IPreloadable
    {
        [Doc("Whether to wait for the effect warm-up animation before playing next command.")]
        public BooleanParameter Wait;

        /// <summary>
        /// Resource path of the effect to spawn.
        /// </summary>
        protected abstract string Path { get; }
        /// <summary>
        /// Whether the effect should de-spawn.
        /// </summary>
        protected virtual bool DestroyWhen { get; } = false;

        protected virtual ISpawnManager SpawnManager => Engine.GetServiceOrErr<ISpawnManager>();

        public virtual async Awaitable PreloadResources ()
        {
            await SpawnManager.HoldResources(Path, this);
        }

        public virtual void ReleaseResources ()
        {
            SpawnManager.ReleaseResources(Path, this);
        }

        public override Awaitable Execute (ExecutionContext ctx) => DestroyWhen
            ? new DestroySpawned {
                PlaybackSpot = PlaybackSpot,
                Path = Path,
                Params = GetDestroyParameters(),
                Wait = Wait,
                ConditionalExpression = ConditionalExpression,
                InvertedConditionalExpression = InvertedConditionalExpression
            }.Execute(ctx)
            : new Spawn {
                PlaybackSpot = PlaybackSpot,
                Path = Path,
                Params = GetSpawnParameters(),
                ScenePosition = GetScenePosition(),
                Position = GetPosition(),
                Rotation = GetRotation(),
                Scale = GetScale(),
                Wait = Wait,
                ConditionalExpression = ConditionalExpression,
                InvertedConditionalExpression = InvertedConditionalExpression
            }.Execute(ctx);

        protected abstract StringListParameter GetSpawnParameters ();
        protected virtual StringListParameter GetDestroyParameters () => null;

        protected virtual DecimalListParameter GetScenePosition () => null;
        protected virtual DecimalListParameter GetPosition () => null;
        protected virtual DecimalListParameter GetRotation () => null;
        protected virtual DecimalListParameter GetScale () => null;

        protected virtual string ToSpawnParam (StringParameter param) => Assigned(param) ? param.Value : null;
        protected virtual string ToSpawnParam (IntegerParameter param) => Assigned(param) ? param.Value.ToString() : null;
        protected virtual string ToSpawnParam (DecimalParameter param) => Assigned(param) ? param.Value.ToString(CultureInfo.InvariantCulture) : null;
        protected virtual string ToSpawnParam (BooleanParameter param) => Assigned(param) ? param.Value.ToString() : null;
    }
}

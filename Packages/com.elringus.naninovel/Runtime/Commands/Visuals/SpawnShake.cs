using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Applies [shake effect](/guide/special-effects#shake)
for the actor with the specified ID or main camera.",
        null,
        @"
; Shake 'Dialogue' text printer with default params.
@shake Dialogue",
        @"
; Start shaking 'Kohaku' character, show choice to stop and act accordingly.
@shake Kohaku loop!
@choice ""Stop shaking""
    @shake Kohaku !loop
...",
        @"
; Shake main Naninovel camera horizontally 5 times.
@shake Camera count:5 hor! !ver"
    )]
    [Serializable, Alias("shake"), VisualsGroup, Icon("WavePulse")]
    public class SpawnShake : SpawnEffect
    {
        [Doc("ID of the actor to shake. In case multiple actors with the same ID found " +
             "(eg, a character and a printer), will affect only the first found one. " +
             "When not specified, will shake the default text printer. " +
             "To shake main camera, use `Camera` keyword.")]
        [Alias(NamelessParameterAlias), ActorContext]
        public StringParameter ActorId;
        [Doc("The number of shake iterations. Ignored when `loop` is enabled.")]
        [Alias("count")]
        public IntegerParameter ShakeCount;
        [Doc("Whether to continue shaking until disabled.")]
        public BooleanParameter Loop;
        [Doc("The base duration of each shake iteration, in seconds.")]
        [Alias("time")]
        public DecimalParameter ShakeDuration;
        [Doc("The randomizer modifier applied to the base duration of the effect.")]
        [Alias("deltaTime")]
        public DecimalParameter DurationVariation;
        [Doc("The base displacement amplitude of each shake iteration, in units.")]
        [Alias("power")]
        public DecimalParameter ShakeAmplitude;
        [Doc("The randomized modifier applied to the base displacement amplitude.")]
        [Alias("deltaPower")]
        public DecimalParameter AmplitudeVariation;
        [Doc("Whether to displace the actor horizontally (by x-axis).")]
        [Alias("hor")]
        public BooleanParameter ShakeHorizontally;
        [Doc("Whether to displace the actor vertically (by y-axis).")]
        [Alias("ver")]
        public BooleanParameter ShakeVertically;

        protected override string Path => ResolvePath();
        protected override bool DestroyWhen => Assigned(Loop) && !Loop || Assigned(ShakeCount) && ShakeCount == -1;
        protected const string CameraId = "Camera";

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (Assigned(ShakeCount) && Assigned(Loop) && Loop)
                Warn("Count parameter in shake command is ignored when 'loop' is enabled.");
            if (Assigned(ShakeCount) && ShakeCount == 0)
                Warn("Use loop! instead of count:0 to start shake loop.");
            if (Assigned(ShakeCount) && ShakeCount == -1)
                Warn("Use !loop instead of count:-1 to stop shake loop.");
            return base.Execute(ctx);
        }

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(Assigned(ActorId) ? ActorId : Engine.GetServiceOrErr<ITextPrinterManager>().DefaultPrinterId),
            ToSpawnParam(ShakeCount),
            ToSpawnParam(Loop),
            ToSpawnParam(ShakeDuration),
            ToSpawnParam(DurationVariation),
            ToSpawnParam(ShakeAmplitude),
            ToSpawnParam(AmplitudeVariation),
            ToSpawnParam(ShakeHorizontally),
            ToSpawnParam(ShakeVertically)
        };

        protected virtual string ResolvePath ()
        {
            if (ActorId == CameraId) return "ShakeCamera";
            var manager = Engine.FindService<IActorManager, string>(ActorId,
                static (manager, actorId) => manager.ActorExists(actorId));
            if (manager is ICharacterManager) return $"ShakeCharacter#{ActorId}";
            if (manager is IBackgroundManager) return $"ShakeBackground#{ActorId}";
            return $"ShakePrinter#{ActorId}";
            // Can't throw here, as the actor may not be available (eg, pre-loading with dynamic policy).
        }
    }
}

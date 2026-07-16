using System;
using System.Collections.Generic;

namespace Naninovel.Commands
{
    [Doc(
        @"
Spawns particle system simulating [snow](/guide/special-effects#snow).",
        null,
        @"
; Start intensive snow over 10 seconds.
@snow power:1 time:10
; Stop the snow over 30 seconds.
@snow power:0 time:30"
    )]
    [Serializable, Alias("snow"), VisualsGroup, Icon("Snowflake")]
    public class SpawnSnow : SpawnLocalizedEffect
    {
        [Doc("The intensity of the snow (particles spawn rate per second), in 0.0 to 1.0 range; defaults to 0.5. Set to 0 to disable (de-spawn) the effect.")]
        [Alias("power")]
        public DecimalParameter Intensity;
        [Doc("The particle system will gradually grow the spawn rate to the target level over the specified time, in seconds.")]
        [Alias("time")]
        public DecimalParameter FadeDuration;

        protected override string Path => "Snow";
        protected override bool DestroyWhen => Assigned(Intensity) && Intensity == 0;

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(Intensity),
            ToSpawnParam(FadeDuration)
        };

        protected override StringListParameter GetDestroyParameters () => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}

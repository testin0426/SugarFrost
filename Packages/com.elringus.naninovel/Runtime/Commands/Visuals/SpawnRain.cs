using System;
using System.Collections.Generic;

namespace Naninovel.Commands
{
    [Doc(
        @"
Spawns particle system simulating [rain](/guide/special-effects#rain).",
        null,
        @"
; Start intensive rain over 10 seconds.
@rain power:1 time:10
; Stop the rain over 30 seconds.
@rain power:0 time:30"
    )]
    [Serializable, Alias("rain"), VisualsGroup, Icon("CloudRainDuo")]
    public class SpawnRain : SpawnLocalizedEffect
    {
        [Doc("The intensity of the rain (particles spawn rate per second), in 0.0 to 1.0 range; defaults to 0.5. Set to 0 to disable (de-spawn) the effect.")]
        [Alias("power")]
        public DecimalParameter Intensity;
        [Doc("The particle system will gradually grow the spawn rate to the target level over the specified time, in seconds.")]
        [Alias("time")]
        public DecimalParameter FadeDuration;
        [Doc("Multiplier to the horizontal speed of the particles. Use to change angle of the rain drops.")]
        [Alias("xSpeed")]
        public DecimalParameter XVelocity;
        [Doc("Multiplier to the vertical speed of the particles.")]
        [Alias("ySpeed")]
        public DecimalParameter YVelocity;

        protected override string Path => "Rain";
        protected override bool DestroyWhen => Assigned(Intensity) && Intensity == 0;

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(Intensity),
            ToSpawnParam(FadeDuration),
            ToSpawnParam(XVelocity),
            ToSpawnParam(YVelocity)
        };

        protected override StringListParameter GetDestroyParameters () => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}

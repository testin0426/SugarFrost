using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Activates/disables camera look mode, when player can offset the main camera with input devices
(eg, by moving a mouse or using gamepad analog stick).
Check [this video](https://youtu.be/rC6C9mA7Szw) for a quick demonstration of the command.",
        null,
        @"
; Activate camera look mode with default parameters.
@look",
        @"
; Activate camera look mode with custom parameters.
@look zone:6.5,4 speed:3,2.5 gravity!",
        @"
; Disable look mode and instantly reset the offset.
@look false",
        @"
; Disable look, but reset gradually, with 0.25 speed.
@look false gravity! speed:0.25"
    )]
    [Serializable, Alias("look"), UIGroup, Icon("Eye")]
    public class CameraLook : Command
    {
        [Doc("Whether to enable or disable the camera look mode. Default: true.")]
        [Alias(NamelessParameterAlias), ParameterDefaultValue("true")]
        public BooleanParameter Enable;
        [Doc("A bound box with X,Y sizes in units from the initial camera position, " +
             "describing how far the camera can be moved. Default: 5.0,3.0")]
        [Alias("zone"), VectorContext("X,Y")]
        public DecimalListParameter LookZone;
        [Doc("Camera movement speed (sensitivity) by X,Y axes. Default: 1.5,1.0")]
        [Alias("speed"), VectorContext("X,Y")]
        public DecimalListParameter LookSpeed;
        [Doc("Whether to automatically move camera to the initial position when the look input is not active " +
             "(eg, mouse is not moving or analog stick is in default position). Default: false.")]
        [ParameterDefaultValue("false")]
        public BooleanParameter Gravity;

        protected virtual Vector2 DefaultZone => new(5, 3);
        protected virtual Vector2 DefaultSpeed => new(1.5f, 1);

        public override Awaitable Execute (ExecutionContext ctx)
        {
            Engine.GetServiceOrErr<ICameraManager>().SetLookMode(
                GetAssignedOrDefault(Enable, true),
                ArrayUtils.ToVector2(LookZone, DefaultZone),
                ArrayUtils.ToVector2(LookSpeed, DefaultSpeed),
                GetAssignedOrDefault(Gravity, false));
            return Async.Completed;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Naninovel;
using Naninovel.Commands;
using TMPro;
using UnityEngine.Rendering.Universal;

[Alias("setRenderer")]
public class SetRenderer : Command
{
    public IntegerParameter Index = 0;

    public override Awaitable Execute(ExecutionContext ctx)
    {
        var cameraManager = Engine.GetServiceOrErr<ICameraManager>();
        var camera = cameraManager.Camera;

        var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
        if(cameraData != null)
        {
            cameraData.SetRenderer(Index.Value);
        }
        else
        {
            Debug.LogWarning("Universal");
        }
        return Async.Completed;
    }
}

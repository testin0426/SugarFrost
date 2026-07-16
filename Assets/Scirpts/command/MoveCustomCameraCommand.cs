using Naninovel;
using Naninovel.Commands;
using UnityEngine;
using DG.Tweening;

[Alias("moveCam")]
public class MoveCustomCameraCommand : Command
{
    // The exact name of the Camera GameObject in your Unity hierarchy
    [RequiredParameter] public StringParameter CameraName;
    
    // Accepts an array of 3 numbers: [x, y, z]
    public DecimalListParameter Position; 
    
    // Accepts an array of 3 numbers for Euler angles: [x, y, z]
    public DecimalListParameter Rotation; 
    
    public DecimalParameter Duration = 1f;

    public override async Awaitable Execute(ExecutionContext ctx)
    {
        // Locate the camera in the active scene
        GameObject camObj = GameObject.Find(CameraName);
        
        if (camObj == null)
        {
            Debug.LogError($"[moveCam] Camera with name '{CameraName}' was not found in the scene.");
            return;
        }

        // Create a DOTween sequence to play multiple animations simultaneously
        Sequence camSequence = DOTween.Sequence();
        float animDuration = Duration;

        // Apply Position Animation if the parameter is provided in the .nani script
        if (Assigned(Position) && Position.Length >= 3)
        {
            Vector3 targetPos = new Vector3(Position[0], Position[1], Position[2]);
            camSequence.Join(camObj.transform.DOMove(targetPos, animDuration).SetEase(Ease.InOutQuad));
        }

        // Apply Rotation Animation if the parameter is provided in the .nani script
        if (Assigned(Rotation) && Rotation.Length >= 3)
        {
            Vector3 targetRot = new Vector3(Rotation[0], Rotation[1], Rotation[2]);
            camSequence.Join(camObj.transform.DORotate(targetRot, animDuration).SetEase(Ease.InOutQuad));
        }

        // Await the completion of the animation duration before Naninovel proceeds to the next line
        await Async.Delay(System.TimeSpan.FromSeconds(animDuration), ctx.Token);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Naninovel;
using Naninovel.Commands;
using Naninovel.UI;
using TMPro;

[Alias("play3D")]
public class Play3D : Command
{
   public StringParameter SceneName;

   public static AwaitableCompletionSource SceneCompletionSource;

   public override async Awaitable Execute(ExecutionContext ctx)
    {
        if(!Assigned(SceneName)) return;
        var stateManager = Engine.GetService<IStateManager>();
        var uiManager = Engine.GetService<IUIManager>();
        var transitionUI = uiManager.GetUI<ISceneTransitionUI>();

        await transitionUI.CaptureScene();
        //uiManager.GetUI<Naninovel.UI.ScriptBoundingUI>().Visible = false;

        uiManager.SetUIVisibleWithToggle(false);
        SceneCompletionSource = new AwaitableCompletionSource();

        await SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);

        var defaultTransition = new Transition("Crossfade");
        await transitionUI.Transition(defaultTransition, new Tween(1f), ctx.Token);
        await SceneCompletionSource.Awaitable;

        await transitionUI.CaptureScene();
        await SceneManager.UnloadSceneAsync(SceneName);

        uiManager.SetUIVisibleWithToggle(true);

        await transitionUI.Transition(defaultTransition, new Tween(1f), ctx.Token);
    }
}

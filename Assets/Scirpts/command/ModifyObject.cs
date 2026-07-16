using UnityEngine;
using Naninovel;
using Naninovel.Commands;
using DG.Tweening;

[Alias("modifyObject")]
public class ModifyObjectCommand : Command
{
    [RequiredParameter, Alias(NamelessParameterAlias)]
    public StringParameter ObjectName;

    [Alias("active")]
    public BooleanParameter Active;

    [Alias("prop"), ParameterDefaultValue("_Threshold")]
    public StringParameter ShaderProperty = "_Threshold";

    [Alias("from")]
    public DecimalParameter FromValue;

    [Alias("to")]
    public DecimalParameter ToValue;

    [Alias("time"), ParameterDefaultValue("1")]
    public DecimalParameter Duration = 1f;

    [Alias("ease"), ConstantContext(typeof(Ease))]
    public StringParameter EaseName;

    public BooleanParameter Wait;

    public override async Awaitable Execute(ExecutionContext ctx)
    {
        var target = GameObject.Find(ObjectName);
        if (target == null)
        {
            Debug.LogError($"[modifyObject] GameObject '{ObjectName}' not found.");
            return;
        }

        if (Assigned(Active)) target.SetActive(Active);

        if (!Assigned(ToValue)) return;

        if (!target.TryGetComponent<Renderer>(out var renderer))
        {
            Debug.LogError($"[modifyObject] '{ObjectName}' has no Renderer; cannot animate shader property.");
            return;
        }

        var mat = renderer.material;
        var prop = (string)ShaderProperty;
        if (!mat.HasProperty(prop))
        {
            Debug.LogError($"[modifyObject] Material on '{ObjectName}' has no property '{prop}'.");
            return;
        }

        var ease = Ease.InOutQuad;
        if (Assigned(EaseName) && !System.Enum.TryParse(EaseName, true, out ease))
            Debug.LogWarning($"[modifyObject] Failed to parse ease '{EaseName}'.");

        if (Assigned(FromValue)) mat.SetFloat(prop, FromValue);

        var time = Mathf.Max(Duration, 0.0001f);
        var tween = mat.DOFloat(ToValue, prop, time).SetEase(ease);

        if (Wait)
        {
            using (ctx.Token.CancellationToken.Register(() => tween.Kill()))
                while (tween.IsActive() && !tween.IsComplete())
                    await Async.NextFrame(ctx.Token);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Naninovel;
using Naninovel.Commands;

[Alias("hello")]
public class HelloWorld : Command
{
    // Start is called before the first frame update
    public StringParameter Name;

    public override Awaitable Execute (ExecutionContext ctx)
    {
        if (Assigned(Name)) Debug.Log($"Hello, {Name!}");
        else Debug.Log("Hello World!");
        return Async.Completed;
    }
}

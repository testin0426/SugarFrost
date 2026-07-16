using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Opens specified URL (web address) with default web browser.",
        @"
When outside of WebGL or in editor, Unity's `Application.OpenURL` method is used to handle the command;
consult the [documentation](https://docs.unity3d.com/ScriptReference/Application.OpenURL.html) for behaviour details and limitations.
Under WebGL native `window.open()` JS function is invoked: https://developer.mozilla.org/en-US/docs/Web/API/Window/open.",
        @"
; Open blank page in the current tab.
@openURL ""about:blank""",
        @"
; Open Naninovel website in new tab.
@openURL ""https://naninovel.com"" target:_blank"
    )]
    [Serializable, UIGroup, Icon("UpRightFromSquareDuo")]
    public class OpenURL : Command
    {
        [Doc("URL to open.")]
        [Alias(NamelessParameterAlias), RequiredParameter]
        public StringParameter URL;
        [Doc("Browsing context: _self (current tab), _blank (new tab), _parent, _top.")]
        [ParameterDefaultValue("_self")]
        public StringParameter Target;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            WebUtils.OpenURL(URL, GetAssignedOrDefault(Target, "_self"));
            return Async.Completed;
        }
    }
}

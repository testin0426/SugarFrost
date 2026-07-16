using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// Allows controlling scrollbar with Naninovel input.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollController : MonoBehaviour
    {
        private ScrollRect rect;
        private IInputHandle input;

        private void Awake ()
        {
            rect = GetComponent<ScrollRect>();
            input = Engine.GetServiceOrErr<IInputManager>().GetScroll();
        }

        private void Update ()
        {
            if (input is { Active: true })
                rect.content.anchoredPosition -= new Vector2(0, input.Force.y * rect.scrollSensitivity);
        }
    }
}

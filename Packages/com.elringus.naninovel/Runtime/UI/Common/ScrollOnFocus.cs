using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// Scrolls parent scrollbar to the specified content when the element gets focus (is selected).
    /// Work only in gamepad and keyboard input modes.
    /// </summary>
    [AddComponentMenu("Naninovel/ UI/Scroll On Focus")]
    public class ScrollOnFocus : MonoBehaviour, ISelectHandler
    {
        [Tooltip("Content to focus.")]
        [SerializeField] private RectTransform content;

        private ScrollRect rect;

        public virtual void OnSelect (BaseEventData eventData)
        {
            var mode = Engine.GetServiceOrErr<IInputManager>().InputMode;
            if (mode != InputMode.Gamepad && mode != InputMode.Keyboard) return;
            if (!rect) rect = GetComponentInParent<ScrollRect>();
            if (!rect) throw Engine.Fail("Failed to find scroll rect in parents.");
            if (!rect.Contains(content)) rect.ScrollTo(content);
        }

        protected virtual void Awake ()
        {
            this.AssertRequiredObjects(content);
        }
    }
}

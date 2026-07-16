using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <summary>
    /// Attach to a game object to make it activate an <see cref="IInputHandle"/> when clicked or touched.
    /// </summary>
    [AddComponentMenu("Naninovel/ UI/Input Trigger")]
    public class InputTrigger : MonoBehaviour, IPointerClickHandler
    {
        protected virtual string InputId => inputId;

        [Tooltip("The identifier of an input to activate on click or touch.")]
        [SerializeField] private string inputId;

        public virtual void OnPointerClick (PointerEventData _)
        {
            Engine.GetService<IInputManager>()?.GetInput(InputId)?.Pulse();
        }
    }
}

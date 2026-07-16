using UnityEngine;
using Naninovel;
using System.Collections.Generic;

[RequireComponent(typeof(GenericCharacterBehaviour))]
public class CharacterAppearanceBridge : MonoBehaviour
{
    private GenericCharacterBehaviour behaviour;
    private Animator animator;
    private readonly HashSet<string> cachedTriggers = new();

    void Awake()
    {
        behaviour = GetComponent<GenericCharacterBehaviour>();

        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("[Bridge] No Animator found in children!");
            return;
        }


        foreach (var p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger)
                cachedTriggers.Add(p.name);

        Debug.Log($"[Bridge] Animator='{animator.name}', " +
                  $"Cached triggers: {string.Join(", ", cachedTriggers)}");

        behaviour.OnAppearanceChanged += HandleAppearance;
    }

    void OnDestroy()
    {
        if (behaviour != null)
            behaviour.OnAppearanceChanged -= HandleAppearance;
    }

    private void HandleAppearance(string appearance)
    {
        var triggerName = appearance.Contains("/")
            ? appearance.Substring(appearance.IndexOf('/') + 1)
            : appearance;

        if (cachedTriggers.Contains(triggerName))
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"[Bridge] SetTrigger '{triggerName}' ✓");
        }
        else
            Debug.LogWarning($"[Bridge] '{triggerName}' not in cache. " +
                             $"Cached: {string.Join(", ", cachedTriggers)}");
    }
}
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A choice option managed by <see cref="IChoiceHandlerActor"/>.
    /// </summary>
    [Serializable]
    public struct Choice : IEquatable<Choice>
    {
        /// <summary>
        /// Unique identifier of the choice.
        /// </summary>
        public string Id => id;
        /// <summary>
        /// The type of the choice callback.
        /// </summary>
        public ChoiceCallbackType CallbackType => ResolveCallbackType();
        /// <summary>
        /// The choice callback when <see cref="CallbackType"/> is <see cref="ChoiceCallbackType.Directive"/>.
        /// </summary>
        public DirectiveChoiceCallback DirectiveCallback => GetCallbackChecked(directive, ChoiceCallbackType.Directive);
        /// <summary>
        /// The choice callback when <see cref="CallbackType"/> is <see cref="ChoiceCallbackType.Transient"/>.
        /// </summary>
        public TransientChoiceCallback TransientCallback => GetCallbackChecked(transient, ChoiceCallbackType.Transient);
        /// <summary>
        /// The choice callback when <see cref="CallbackType"/> is <see cref="ChoiceCallbackType.Nested"/>.
        /// </summary>
        public NestedChoiceCallback NestedCallback => GetCallbackChecked(nested, ChoiceCallbackType.Nested);
        /// <summary>
        /// The summary of the choice.
        /// </summary>
        public LocalizableText Summary => options.Summary;
        /// <summary>
        /// Whether the choice is locked/disabled.
        /// </summary>
        public bool Locked => options.Lock;
        /// <summary>
        /// Local resource path to a custom choice button variant.
        /// </summary>
        [CanBeNull] public string ButtonPath => options.ButtonPath;
        /// <summary>
        /// Position of the choice button local to the parent choice handler.
        /// </summary>
        public Vector2? ButtonPosition => options.ButtonPosition;

        [SerializeField] private string id;
        [SerializeField] private ChoiceOptions options;
        [SerializeField] private DirectiveChoiceCallback directive;
        [SerializeField] private TransientChoiceCallback transient;
        [SerializeField] private NestedChoiceCallback nested;

        /// <summary>
        /// Creates a choice with a <see cref="ChoiceCallbackType.Directive"/> callback.
        /// </summary>
        /// <param name="callback">The directive callback to execute when the choice is handled.</param>
        /// <param name="options">Optional choice preferences.</param>
        public Choice (DirectiveChoiceCallback callback, ChoiceOptions options = default)
            : this(callback, default, default, options) { }

        /// <summary>
        /// Creates a choice with a <see cref="ChoiceCallbackType.Transient"/> callback.
        /// </summary>
        /// <param name="callback">The transient callback to execute when the choice is handled.</param>
        /// <param name="options">Optional choice preferences.</param>
        public Choice (TransientChoiceCallback callback, ChoiceOptions options = default)
            : this(default, callback, default, options) { }

        /// <summary>
        /// Creates a choice with a <see cref="ChoiceCallbackType.Nested"/> callback.
        /// </summary>
        /// <param name="callback">The nested callback to execute when the choice is handled.</param>
        /// <param name="options">Optional choice preferences.</param>
        public Choice (NestedChoiceCallback callback, ChoiceOptions options = default)
            : this(default, default, callback, options) { }

        private Choice (DirectiveChoiceCallback directive, TransientChoiceCallback transient,
            NestedChoiceCallback nested, ChoiceOptions options)
        {
            id = string.IsNullOrEmpty(options.Id) ? Guid.NewGuid().ToString("N") : options.Id;
            this.options = options;
            this.directive = directive;
            this.transient = transient;
            this.nested = nested;
        }

        public override bool Equals (object obj) => obj is Choice state && Equals(state);
        public bool Equals (Choice other) => id == other.id;
        public override int GetHashCode () => 1877310944 + EqualityComparer<string>.Default.GetHashCode(Id);
        public static bool operator == (Choice left, Choice right) => left.Equals(right);
        public static bool operator != (Choice left, Choice right) => !(left == right);

        private ChoiceCallbackType ResolveCallbackType ()
        {
            if (nested.HostedAt.Valid) return ChoiceCallbackType.Nested;
            if (!string.IsNullOrEmpty(transient.Scenario)) return ChoiceCallbackType.Transient;
            return ChoiceCallbackType.Directive;
        }

        private T GetCallbackChecked<T> (T callback, ChoiceCallbackType type)
        {
            if (CallbackType == type) return callback;
            throw new Error($"Accessing invalid choice callback: the callback is '{CallbackType}', while accessed '{type}'.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Builds and applies layer composition expressions.
    /// </summary>
    /// <remarks>
    /// Value format: path/to/object/parent>SelectedObjName,another/path+EnabledObjName,another/path-DisabledObjName,...
    /// Select operator (>) means that only this object is enabled inside the group, others should be disabled.
    /// When no target objects specified, all the layers inside the group will be affected (recursively, including child groups).
    /// </remarks>
    public class LayeredCompositor
    {
        public const char GroupSymbol = '/';
        public const char SelectSymbol = '>';
        public const char EnableSymbol = '+';
        public const char DisableSymbol = '-';
        public const char SplitSymbol = ',';

        private readonly StringBuilder builder = new();
        private readonly string actorId;
        private readonly IReadOnlyCollection<KeyValuePair<string, string>> aliases;
        private IReadOnlyCollection<ILayeredRendererLayer> layers;

        public LayeredCompositor ([CanBeNull] string actorId = null, [CanBeNull] IReadOnlyCollection<KeyValuePair<string, string>> aliases = null)
        {
            this.actorId = actorId ?? "";
            this.aliases = aliases ?? Array.Empty<KeyValuePair<string, string>>();
        }

        /// <summary>
        /// Applies specified composition expression to the specified layers.
        /// </summary>
        public void Apply (string composition, IReadOnlyCollection<ILayeredRendererLayer> layers)
        {
            if (string.IsNullOrWhiteSpace(composition)) return;
            this.layers = layers;
            using var _ = ListPool<Text>.Rent(out var expressions);
            CollectExpressions(composition, expressions);
            for (var i = 0; i < expressions.Count; i++)
                ApplyExpression(expressions[i].Span);
        }

        /// <summary>
        /// Builds composition expression from the specified layers.
        /// </summary>
        public string Compose (IReadOnlyCollection<ILayeredRendererLayer> layers)
        {
            builder.Clear();
            var i = 0;
            foreach (var layer in layers)
            {
                if (i++ > 0) builder.Append(SplitSymbol);
                builder.Append(layer.Group);
                builder.Append(layer.Enabled ? EnableSymbol : DisableSymbol);
                builder.Append(layer.Name);
            }
            return builder.ToString();
        }

        private void CollectExpressions (string composition, ICollection<Text> expressions)
        {
            var span = composition.AsSpan().Trim();
            if (span.IsEmpty) return;

            var start = 0;
            for (var i = 0; i <= span.Length; i++)
            {
                var end = i == span.Length;
                if (!end && span[i] != SplitSymbol) continue;

                var len = i - start;
                if (len > 0)
                {
                    var txt = new Text(composition, composition.Length - span.Length + start, len);
                    if (GetAliasedComposition(txt) is { } aliased)
                        CollectExpressions(aliased, expressions);
                    else expressions.Add(txt);
                }

                start = i + 1; // move over comma
            }
        }

        [CanBeNull]
        private string GetAliasedComposition (in Text alias)
        {
            foreach (var (aliasKey, comp) in aliases)
                if (alias == aliasKey)
                    return comp;
            return null;
        }

        private void ApplyExpression (in ReadOnlySpan<char> exp)
        {
            var opIdx = exp.IndexOfAny(SelectSymbol, EnableSymbol, DisableSymbol);
            if (opIdx < 0) throw new Error($"Failed to parse '{actorId}' layered expression: '{exp.ToString()}'.");

            var group = exp[..opIdx];
            var name = exp[(opIdx + 1)..];
            switch (exp[opIdx])
            {
                case SelectSymbol: ApplySelect(group, name); break;
                case EnableSymbol: ApplyEnable(group, name); break;
                case DisableSymbol: ApplyDisable(group, name); break;
            }
        }

        private void ApplySelect (in ReadOnlySpan<char> group, in ReadOnlySpan<char> name)
        {
            var anySelected = false;
            if (name.IsEmpty)
            {
                var parent = GetParent(group);
                foreach (var layer in layers)
                    if (InScope(parent, layer.Group))
                        if (layer.Enabled = InScope(group, layer.Group))
                            anySelected = true;
            }
            else
            {
                foreach (var layer in layers)
                    if (Equals(layer.Group, group))
                        if (layer.Enabled = Equals(layer.Name, name))
                            anySelected = true;
            }
            if (!anySelected) WarnUnaffected(group, name, SelectSymbol);
        }

        private void ApplyEnable (in ReadOnlySpan<char> group, in ReadOnlySpan<char> name)
        {
            var anyEnabled = false;
            if (name.IsEmpty)
            {
                foreach (var layer in layers)
                    if (InScope(group, layer.Group))
                        layer.Enabled = anyEnabled = true;
            }
            else
            {
                foreach (var layer in layers)
                    if (Equals(layer.Group, group) && Equals(layer.Name, name))
                    {
                        layer.Enabled = true;
                        return;
                    }
            }
            if (!anyEnabled) WarnUnaffected(group, name, EnableSymbol);
        }

        private void ApplyDisable (in ReadOnlySpan<char> group, in ReadOnlySpan<char> name)
        {
            var noneDisabled = true;
            if (name.IsEmpty)
            {
                foreach (var layer in layers)
                    if (InScope(group, layer.Group))
                        layer.Enabled = noneDisabled = false;
            }
            else
            {
                foreach (var layer in layers)
                    if (Equals(layer.Group, group) && Equals(layer.Name, name))
                    {
                        layer.Enabled = false;
                        return;
                    }
            }
            if (noneDisabled) WarnUnaffected(group, name, DisableSymbol);
        }

        private static bool Equals (string s, in ReadOnlySpan<char> span)
        {
            return s.AsSpan().Equals(span, StringComparison.Ordinal);
        }

        private static bool InScope (in ReadOnlySpan<char> scope, in ReadOnlySpan<char> group)
        {
            if (scope.IsEmpty) return true;
            if (group.Equals(scope, StringComparison.Ordinal)) return true;
            return group.Length > scope.Length &&
                   group.StartsWith(scope, StringComparison.Ordinal) &&
                   group[scope.Length] == GroupSymbol;
        }

        private static ReadOnlySpan<char> GetParent (in ReadOnlySpan<char> group)
        {
            var idx = group.LastIndexOf(GroupSymbol);
            return idx < 0 ? ReadOnlySpan<char>.Empty : group[..idx];
        }

        private void WarnUnaffected (in ReadOnlySpan<char> group, in ReadOnlySpan<char> name, char op)
        {
            var exp = $"{group.ToString()}{op}{name.ToString()}";
            Engine.Warn($"Layered expression '{exp}' applied to '{actorId}' didn't affect any layers.");
        }
    }
}

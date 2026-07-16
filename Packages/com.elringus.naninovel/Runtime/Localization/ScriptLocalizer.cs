using System;
using System.Collections.Generic;
using System.Reflection;
using Naninovel.Commands;
using Naninovel.ManagedText;
using Naninovel.Syntax;

namespace Naninovel
{
    /// <summary>
    /// Generates localization documents for scenario scripts.
    /// </summary>
    public class ScriptLocalizer
    {
        /// <summary>
        /// Generation options.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// Syntax to use when generating the document.
            /// </summary>
            public ISymbols Symbols { get; set; } = Compiler.Symbols;
            /// <summary>
            /// Whether to insert annotations for the localization records with the context of the localized
            /// content, such as authors and inlined commands of generic text lines and full command lines
            /// hosting localized parameters. Will as well insert comment lines placed before localized lines.
            /// </summary>
            public bool Annotate { get; set; } = true;
            /// <summary>
            /// String to insert before annotation lines to distinguish them for the localized text.
            /// </summary>
            public string AnnotationPrefix { get; set; } = "> ";
            /// <summary>
            /// Text character to join composite parts of the localized content, such as parts of generic text lines.
            /// </summary>
            public char Separator { get; set; } = '|';
            /// <summary>
            /// Delegate to invoke when an untranslated text is discovered; invoked with managed text record key.
            /// Has effect only when existing (previously generated) localization document is specified.
            /// </summary>
            public Action<string> OnUntranslated { get; set; }
        }

        private readonly List<string> ids = new();
        private readonly List<string> text = new();
        private readonly List<ManagedTextRecord> records = new();
        private readonly ScriptFormatter fmt;
        private readonly TextSplitter splitter;
        private readonly Options options;
        private Script script;
        private ManagedTextDocument existing;
        private string prevComment;

        public ScriptLocalizer (Options options = null)
        {
            this.options = options ?? new Options();
            splitter = new(this.options.Separator);
            fmt = new(this.options.Symbols);
        }

        /// <summary>
        /// Generates localization document for specified scenario script asset.
        /// When existing (previously generated) document is specified, preserves localized values.
        /// </summary>
        /// <remarks>
        /// Localization record structure:<br/>
        ///  • <see cref="ManagedTextRecord.Key"/> is the unique identifier of the localized content.<br/>
        ///  • <see cref="ManagedTextRecord.Comment"/> contains the text to translate optionally
        /// preceded by an annotation (localization context), when <see cref="Options.Annotate"/>
        /// is enabled in generation options.<br/>
        ///  • <see cref="ManagedTextRecord.Value"/> contains existing translation (when found in the
        /// specified existing document) or is empty and is intended to be filled with the translation.
        /// <br/><br/>
        /// Composite parts of the localized content, such as parts of generic text lines are merged
        /// into single record with <see cref="Options.Separator"/>.
        /// </remarks>
        /// <param name="script">Source script asset to generate localization document for.</param>
        /// <param name="existing">Document previously generated for the source script with localized values to preserve.</param>
        /// <returns>Generated localization document.</returns>
        public ManagedTextDocument Localize (Script script, ManagedTextDocument existing = null)
        {
            Reset(script, existing);
            foreach (var line in script.Lines)
                if (line is CommentLine comment) VisitLine(comment);
                else if (line is CommandLine command) VisitLine(command);
                else if (line is GenericLine generic) VisitLine(generic);
                else prevComment = null;
            return new(records.ToArray());
        }

        private void Reset (Script script, ManagedTextDocument existing)
        {
            ids.Clear();
            text.Clear();
            records.Clear();
            this.script = script;
            this.existing = existing;
            prevComment = null;
        }

        private void VisitLine (CommentLine line)
        {
            if (string.IsNullOrWhiteSpace(line.CommentText))
                prevComment = null;
            else prevComment = line.CommentText;
        }

        private void VisitLine (CommandLine line)
        {
            CollectLocalizableContent(line.Command);
            if (ids.Count > 0) AppendRecord(Annotate(line));
            prevComment = null;
        }

        private void VisitLine (GenericLine line)
        {
            foreach (var inlined in line.InlinedCommands)
                CollectLocalizableContent(inlined);
            if (ids.Count > 0) AppendRecord(Annotate(line));
            prevComment = null;
        }

        private void CollectLocalizableContent (Command command)
        {
            if (command is not Command.ILocalizable) return;
            foreach (var field in command.GetType().GetFields())
                CollectLocalizableContent(command, field);
        }

        private void CollectLocalizableContent (Command command, FieldInfo field)
        {
            if (field.FieldType != typeof(LocalizableTextParameter)) return;
            var value = (field.GetValue(command) as LocalizableTextParameter)?.RawValue;
            if (!value.HasValue) return;
            foreach (var part in value.Value.Parts)
                if (part.Kind == ParameterValuePartKind.IdentifiedText &&
                    !part.Id.StartsWithOrdinal(ScriptTextIdentifier.RefPrefix))
                    CollectLocalizableContent(part.Id);
        }

        private void CollectLocalizableContent (string mapId)
        {
            ids.Add(mapId);
            var text = script.TextMap.GetTextOrNull(mapId);
            if (text is null) throw new Error($"Failed to extract script text with '{mapId}' ID. Make sure '{script.Path}' script asset is imported without errors.");
            this.text.Add(text);
        }

        private void AppendRecord (string annotation = null)
        {
            var key = splitter.Join(ids);
            var comment = splitter.Join(text);
            var value = ResolveExisting(key);
            if (options.Annotate)
            {
                if (!string.IsNullOrWhiteSpace(annotation))
                    comment = $"{options.AnnotationPrefix}{annotation}\n{comment}";
                if (!string.IsNullOrWhiteSpace(prevComment))
                    comment = $"{options.AnnotationPrefix}{prevComment}\n{comment}";
            }
            records.Add(new(key, value, comment));
            ids.Clear();
            text.Clear();
        }

        private string Annotate (ScriptLine line)
        {
            if (!options.Annotate || IsSimpleGenericLine(line)) return null;
            var annotation = fmt.Format(line, script.TextMap);
            for (var i = 0; i < ids.Count; i++)
            {
                var id = $"{options.Symbols.TextIdOpen}{ids[i]}{options.Symbols.TextIdClose}";
                annotation = annotation.Replace($"{text[i]}{id}", id);
            }
            return annotation.TrimFull();
        }

        private string ResolveExisting (string key)
        {
            if (existing is null) return null;
            if (existing.TryGet(key, out var val) &&
                !string.IsNullOrEmpty(val.Value)) return val.Value;
            options.OnUntranslated?.Invoke(key);
            return null;
        }

        private bool IsSimpleGenericLine (ScriptLine line)
        {
            if (line is not GenericLine generic) return false;
            foreach (var inlined in generic.InlinedCommands)
                if (inlined is not PrintText print) return false;
                else if (print.Text.DynamicValue || Command.Assigned(print.AuthorId)) return false;
            return generic.InlinedCommands.Count == 1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Naninovel.Commands;
using Naninovel.Syntax;

namespace Naninovel
{
    /// <summary>
    /// Allows transforming compiled <see cref="Script"/> back into source scenario text.
    /// </summary>
    public class ScriptFormatter
    {
        private readonly ISymbols smb;
        private readonly Syntax.ScriptFormatter scriptFmt;
        private readonly ListValueFormatter listFmt;
        private readonly NamedValueFormatter namedFmt;
        private readonly StringBuilder builder = new();
        private readonly List<IValueComponent> components = new();
        private ScriptTextMap map;

        public ScriptFormatter (ISymbols smb)
        {
            this.smb = smb;
            scriptFmt = new(smb);
            listFmt = new(smb);
            namedFmt = new(smb);
        }

        public string Format (Script script)
        {
            Reset(script.TextMap);
            foreach (var line in script.Lines)
            {
                AppendLine(line);
                builder.Append('\n');
            }
            return builder.ToString();
        }

        public string Format (ScriptLine line, ScriptTextMap map)
        {
            Reset(map);
            AppendLine(line);
            return builder.ToString();
        }

        private void Reset (ScriptTextMap map)
        {
            builder.Clear();
            components.Clear();
            this.map = map;
        }

        private void AppendLine (ScriptLine line)
        {
            AppendIndent(line);
            if (line is EmptyLine empty) AppendEmptyLine(empty);
            if (line is CommentLine comment) AppendCommentLine(comment);
            if (line is LabelLine label) AppendLabelLine(label);
            if (line is CommandLine cmd) AppendCommandLine(cmd);
            if (line is GenericLine generic) AppendGenericLine(generic);
        }

        private void AppendIndent (ScriptLine line)
        {
            for (int i = 0; i < line.Indent; i++)
                builder.Append("    ");
        }

        private void AppendEmptyLine (EmptyLine _)
        {
            builder.Append('\n');
        }

        private void AppendCommentLine (CommentLine line)
        {
            builder.Append(smb.CommentLine).Append(' ').Append(line.CommentText);
        }

        private void AppendLabelLine (LabelLine line)
        {
            builder.Append(smb.LabelLine).Append(' ').Append(line.LabelText);
        }

        private void AppendCommandLine (CommandLine line)
        {
            builder.Append(smb.CommandLine);
            AppendCommand(line.Command);
        }

        private void AppendGenericLine (GenericLine line)
        {
            AppendPrefix();

            var shouldSkipFirst = line.InlinedCommands.FirstOrDefault() is ModifyCharacter { IsGenericPrefix: { Value: true } };
            DelimitStartWhiteSpace();
            for (int i = shouldSkipFirst ? 1 : 0; i < line.InlinedCommands.Count; i++)
                AppendInlined(line.InlinedCommands[i]);
            DelimitEndWhiteSpace();

            void DelimitStartWhiteSpace ()
            {
                if (line.InlinedCommands.Skip(shouldSkipFirst ? 1 : 0).FirstOrDefault() is PrintText text &&
                    text.Text?.RawValue != null &&
                    char.IsWhiteSpace(FormatRaw(text.Text.RawValue.Value, new()).FirstOrDefault()))
                    builder.Append(smb.InlinedOpen).Append(smb.InlinedClose);
            }

            void DelimitEndWhiteSpace ()
            {
                if (line.InlinedCommands.LastOrDefault() is PrintText text &&
                    text.Text?.RawValue != null &&
                    char.IsWhiteSpace(FormatRaw(text.Text.RawValue.Value, new()).LastOrDefault()))
                    builder.Append(smb.InlinedOpen).Append(smb.InlinedClose);
            }

            void AppendPrefix ()
            {
                if (line.InlinedCommands.FirstOrDefault() is ModifyCharacter { IsGenericPrefix: { Value: true } } mod)
                    AppendPrefixFromModification(mod);
                else FindAndAppendAuthorId();
            }

            void AppendPrefixFromModification (ModifyCharacter mod)
            {
                AppendValue(mod.IdAndAppearance, default);
                builder.Append(smb.AuthorAssign);
            }

            void FindAndAppendAuthorId ()
            {
                var authored = line.InlinedCommands.OfType<PrintText>().FirstOrDefault(p => Command.Assigned(p.AuthorId));
                if (authored == null) return;
                AppendValue(authored.AuthorId, default);
                builder.Append(smb.AuthorAssign);
            }

            void AppendInlined (Command inlined)
            {
                if (inlined is I && line.InlinedCommands.LastOrDefault() == inlined) return;
                if (inlined is PrintText print) AppendGenericText(print);
                else
                {
                    builder.Append(smb.InlinedOpen);
                    AppendCommand(inlined);
                    builder.Append(smb.InlinedClose);
                }
            }

            void AppendGenericText (PrintText print)
            {
                AppendValue(print.Text, new() {
                    FirstGenericContent = line.InlinedCommands.IndexOf(print) == 0 && !Command.Assigned(print.AuthorId)
                });
                var appendWait = print.WaitForInput && line.InlinedCommands.LastOrDefault() != print;
                if (appendWait) builder.Append("[-]");
            }
        }

        private void AppendCommand (Command command)
        {
            var commandId = Command.CommandTypes.First(kv => kv.Value == command.GetType()).Key.FirstToLower();
            var parameters = CommandParameter.Extract(command);
            builder.Append(commandId);
            for (var i = 0; i < parameters.Count; i++)
                AppendParameter(parameters[i]);

            void AppendParameter (ParameterInfo info)
            {
                if (!info.Instance.HasValue) return;
                var value = SerializeValue(info.Instance, new() {
                    ParameterValue = true,
                    NamelessParameterValue = info.Nameless,
                    ParameterValueQuoted = info.Instance is LocalizableTextParameter
                });
                if (info.DefaultValue?.EqualsIgnoreCase(value) ?? false) return;
                builder.Append(' ');
                var id = info.Alias ?? info.Id.FirstToLower();
                if (id != Command.NamelessParameterAlias && !ShouldSerializeAsBoolFlag(info.Instance))
                    builder.Append(id).Append(smb.ParameterAssign);
                if (ShouldSerializeAsBoolFlag(info.Instance))
                    if (((BooleanParameter)info.Instance).Value) builder.Append(id).Append(smb.BooleanFlag);
                    else builder.Append(smb.BooleanFlag).Append(id);
                else builder.Append(value);
            }
        }

        private bool ShouldSerializeAsBoolFlag (ICommandParameter param)
        {
            return param is BooleanParameter && param.HasValue && !param.DynamicValue;
        }

        private void AppendValue (ICommandParameter param, FormatterContext ctx)
        {
            builder.Append(SerializeValue(param, ctx));
        }

        private string SerializeValue (ICommandParameter param, FormatterContext ctx)
        {
            if (param.RawValue.HasValue) return FormatRaw(param.RawValue.Value, ctx);
            if (param is StringParameter str) return SerializeString(str.Value);
            if (param is LocalizableTextParameter text) return SerializeLocalizableText(text.Value);
            if (param is BooleanParameter boolean) return SerializeBool(boolean.Value);
            if (param is DecimalParameter dec) return SerializeDecimal(dec.Value);
            if (param is IntegerParameter integer) return SerializeInteger(integer.Value);
            if (param is NamedStringParameter namedString) return SerializeNamedString(namedString.Value);
            if (param is NamedBooleanParameter namedBoolean) return SerializeNamedBoolean(namedBoolean.Value);
            if (param is NamedDecimalParameter namedDecimal) return SerializeNamedDecimal(namedDecimal.Value);
            if (param is NamedIntegerParameter namedInteger) return SerializeNamedInteger(namedInteger.Value);
            if (param is StringListParameter stringList) return BuildList(stringList, SerializeString);
            if (param is BooleanListParameter booleanList) return BuildList(booleanList, SerializeBool);
            if (param is DecimalListParameter decimalList) return BuildList(decimalList, SerializeDecimal);
            if (param is IntegerListParameter integerList) return BuildList(integerList, SerializeInteger);
            if (param is NamedStringListParameter namedStringList) return BuildList(namedStringList, SerializeNamedString);
            if (param is NamedBooleanListParameter namedBooleanList) return BuildList(namedBooleanList, SerializeNamedBoolean);
            if (param is NamedDecimalListParameter namedDecimalList) return BuildList(namedDecimalList, SerializeNamedDecimal);
            if (param is NamedIntegerListParameter namedIntegerList) return BuildList(namedIntegerList, SerializeNamedInteger);
            return "";

            string SerializeString (NullableString value) => !value.HasValue ? "" : value.Value;
            string SerializeLocalizableText (LocalizableText value) => value.ToString();
            string SerializeBool (NullableBoolean value) => !value.HasValue ? "" : value.Value ? smb.True : smb.False;
            string SerializeDecimal (NullableFloat value) => !value.HasValue ? "" : value.Value.ToString(CultureInfo.InvariantCulture);
            string SerializeInteger (NullableInteger value) => !value.HasValue ? "" : value.Value.ToString(CultureInfo.InvariantCulture);
            string SerializeNamedString (NullableNamedString value) => !value.HasValue ? "" : BuildNamed(value.Value.Name, SerializeString(value.Value.Value));
            string SerializeNamedBoolean (NullableNamedBoolean value) => !value.HasValue ? "" : BuildNamed(value.Value.Name, SerializeBool(value.Value.Value));
            string SerializeNamedDecimal (NullableNamedFloat value) => !value.HasValue ? "" : BuildNamed(value.Value.Name, SerializeDecimal(value.Value.Value));
            string SerializeNamedInteger (NullableNamedInteger value) => !value.HasValue ? "" : BuildNamed(value.Value.Name, SerializeInteger(value.Value.Value));

            string BuildNamed (string name, string value)
            {
                if (string.IsNullOrEmpty(name)) name = null;
                if (string.IsNullOrEmpty(value)) value = null;
                return namedFmt.Format(name, value);
            }

            string BuildList<T> (IEnumerable<T> items, Func<T, string> serializeItem)
            {
                var serializedItems = items.Select(serializeItem).Select(v => string.IsNullOrEmpty(v) ? null : v);
                return listFmt.Format(serializedItems.ToArray());
            }
        }

        private string FormatRaw (RawValue raw, FormatterContext ctx)
        {
            components.Clear();
            foreach (var part in raw.Parts)
                if (part.Kind == ParameterValuePartKind.IdentifiedText)
                    components.Add(new IdentifiedText(new(map.GetTextOrNull(part.Id)), new(part.Id)));
                else if (part.Kind == ParameterValuePartKind.Expression)
                    components.Add(new Syntax.Expression(new(part.Expression)));
                else components.Add(new PlainText(part.Text));
            return scriptFmt.Format(components, ctx);
        }
    }
}

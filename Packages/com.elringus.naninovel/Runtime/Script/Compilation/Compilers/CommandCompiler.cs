using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Naninovel.Syntax;

namespace Naninovel
{
    public class CommandCompiler
    {
        protected virtual IErrorHandler ErrorHandler { get; }
        protected virtual Syntax.Command Syntax => syntax;
        protected virtual Command Command => command;
        protected virtual Type CommandType => commandType;
        protected virtual string CommandId => syntax.Identifier;
        protected virtual bool HashAllowed { get; }

        private readonly HashSet<string> supportedParamIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly MixedValueCompiler mixedCompiler;
        private readonly StringBuilder builder = new();
        private Syntax.Command syntax;
        private Command command;
        private Type commandType;
        private PlaybackSpot spot;
        private CommandLocalization l10n;

        public CommandCompiler (ITextIdentifier identifier, IErrorHandler errorHandler = null, bool hashText = true)
        {
            mixedCompiler = new(identifier);
            ErrorHandler = errorHandler;
            HashAllowed = hashText;
        }

        public virtual Command Compile (Syntax.Command stx, PlaybackSpot spot, int indent)
        {
            ResetState(stx, spot);
            if (!TryGetCommandType(out commandType)) return null;
            if (!TryCreateCommand(out command)) return null;
            Compiler.Commands.TryGetValue(commandType.Name, out l10n);
            Command.PlaybackSpot = spot;
            Command.Indent = indent;
            AssignParameters();
            return command;
        }

        protected virtual void ResetState (Syntax.Command stx, PlaybackSpot spot)
        {
            supportedParamIds.Clear();
            syntax = stx;
            command = null;
            commandType = null;
            this.spot = spot;
            l10n = default;
        }

        protected virtual void AddError (string error)
        {
            ErrorHandler?.HandleError(new(error, 0, 0));
        }

        protected virtual bool TryGetCommandType (out Type commandType)
        {
            commandType = Command.ResolveCommandType(CommandId);
            if (commandType is null) AddError($"Command '{CommandId}' is not found.");
            return commandType != null;
        }

        protected virtual bool TryCreateCommand (out Command command)
        {
            command = Activator.CreateInstance(CommandType) as Command;
            if (command is null) AddError($"Failed to create instance of '{CommandType}' command.");
            return command != null;
        }

        protected virtual void AssignParameters ()
        {
            var fields = CommandType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => typeof(ICommandParameter).IsAssignableFrom(f.FieldType)).ToArray();
            supportedParamIds.UnionWith(fields.Select(f => f.Name));
            foreach (var field in fields)
                AssignField(field);
            CheckUnsupportedParameters();
        }

        protected virtual void AssignField (FieldInfo field)
        {
            if (!TryFindSyntaxFor(field, out var stx)) return;
            if (!TryCreateParameter(field, out var parameter)) return;
            var raw = mixedCompiler.Compile(stx.Value, ShouldHashPlainText(field));
            parameter.AssignRaw(raw, spot, out var errors);
            if (!string.IsNullOrEmpty(errors)) AddError(errors);
            field.SetValue(Command, parameter);
        }

        protected virtual bool TryFindSyntaxFor (FieldInfo field, out Parameter stx)
        {
            var l10nAlias = l10n.Parameters?.FirstOrDefault(p => p.Id == field.Name).Alias;
            var required = field.GetCustomAttribute<Command.RequiredParameterAttribute>() != null;
            var alias = string.IsNullOrWhiteSpace(l10nAlias) ? field.GetCustomAttribute<Command.AliasAttribute>()?.Alias : l10nAlias;
            if (alias != null) supportedParamIds.Add(alias);

            var id = alias != null && Syntax.Parameters.Any(m => m.Nameless && alias == "" || (m.Identifier?.Text.EqualsIgnoreCase(alias) ?? false)) ? alias : field.Name;
            stx = Syntax.Parameters.FirstOrDefault(m => m.Nameless && id == "" || (m.Identifier?.Text.EqualsIgnoreCase(id) ?? false));
            if (stx is null && required) AddError($"Command '{CommandId}' is missing '{id}' parameter.");
            return stx != null;
        }

        protected virtual bool TryCreateParameter (FieldInfo field, out ICommandParameter parameter)
        {
            parameter = Activator.CreateInstance(field.FieldType) as ICommandParameter;
            if (parameter is null) AddError($"Failed to create instance of '{field.FieldType}' parameter for '{CommandId}' command.");
            return parameter != null;
        }

        protected virtual void CheckUnsupportedParameters ()
        {
            foreach (var stx in Syntax.Parameters)
                if (!supportedParamIds.Contains(stx.Identifier ?? ""))
                    AddError($"Command '{CommandId}' has an unsupported '{GetId(stx)}' parameter.");
            string GetId (Parameter p) => p.Nameless ? "nameless" : p.Identifier?.Text;
        }

        protected virtual bool ShouldHashPlainText (FieldInfo field)
        {
            return HashAllowed && field.FieldType == typeof(LocalizableTextParameter);
        }

        protected virtual string Interpolate (IEnumerable<IValueComponent> mixed)
        {
            builder.Clear();
            foreach (var value in mixed)
                if (value is PlainText text) builder.Append(text);
                else if (value is Syntax.Expression expression)
                {
                    builder.Append(Compiler.Symbols.ExpressionOpen[0]);
                    builder.Append(expression.Body);
                    builder.Append(Compiler.Symbols.ExpressionClose[0]);
                }
            return builder.ToString();
        }
    }
}

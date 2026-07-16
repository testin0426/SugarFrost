using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Commands;
using Naninovel.Syntax;
using static Naninovel.Command;
using static Naninovel.CommandParameter;

namespace Naninovel
{
    public class GenericLineCompiler : ScriptLineCompiler<GenericLine, Syntax.GenericLine>
    {
        protected virtual CommandCompiler CommandCompiler { get; }
        protected virtual Syntax.GenericLine Syntax { get; private set; }
        protected virtual IList<Command> InlinedCommands { get; } = new List<Command>();
        protected virtual string AuthorId => Syntax.Prefix?.Author ?? "";
        protected virtual string AuthorAppearance => Syntax.Prefix?.Appearance ?? "";
        protected virtual PlaybackSpot Spot => new(ScriptPath, LineIndex, InlinedCommands.Count);

        private readonly MixedValueCompiler mixedCompiler;
        private readonly NamedValueFormatter namedFmt;
        private readonly IErrorHandler errorHandler;
        private readonly Type printCommandType;

        public GenericLineCompiler (ITextIdentifier identifier, IErrorHandler errorHandler = null)
        {
            this.errorHandler = errorHandler;
            mixedCompiler = new(identifier);
            namedFmt = new(Compiler.Symbols);
            CommandCompiler = new(identifier, errorHandler);
            printCommandType = CommandTypes.Values.First(typeof(PrintText).IsAssignableFrom);
        }

        protected override GenericLine Compile (Syntax.GenericLine lineStx)
        {
            ResetState(lineStx);
            AddAppearanceChange();
            AddContent();
            AddLastWaitInput();
            return new(InlinedCommands, LineIndex, lineStx.Indent, LineHash);
        }

        protected virtual void ResetState (Syntax.GenericLine stx)
        {
            Syntax = stx;
            InlinedCommands.Clear();
        }

        protected virtual void AddAppearanceChange ()
        {
            if (string.IsNullOrEmpty(AuthorId)) return;
            if (string.IsNullOrEmpty(AuthorAppearance)) return;
            // raw assignment required for the addressables labeler to detect the resource context
            var idRaw = new RawValue(new[] { RawValuePart.FromPlainText(namedFmt.Format(AuthorId, AuthorAppearance)) });
            AddCommand(new ModifyCharacter {
                IsGenericPrefix = true,
                IdAndAppearance = FromRaw<NamedStringParameter>(idRaw, Spot, out _),
                Wait = false,
                PlaybackSpot = Spot,
                Indent = Syntax.Indent
            });
        }

        protected virtual void AddContent ()
        {
            for (var i = 0; i < Syntax.Content.Count; i++)
                if (Syntax.Content[i] is InlinedCommand inlined)
                    if (string.IsNullOrEmpty(inlined.Command.Identifier)) continue;
                    else AddCommand(inlined.Command, i);
                else AddGenericText(Syntax.Content[i] as MixedValue);
        }

        protected virtual void AddCommand (Syntax.Command stx, int contentIdx)
        {
            var spot = new PlaybackSpot(ScriptPath, LineIndex, InlinedCommands.Count);
            var command = CommandCompiler.Compile(stx, spot, Syntax.Indent);
            LintCommand(command, spot, contentIdx);
            AddCommand(command);
        }

        protected virtual void LintCommand (Command cmd, PlaybackSpot spot, int contentIdx)
        {
            if (cmd is I && contentIdx == Syntax.Content.Count - 1)
                Engine.Warn("[-] has not effect when placed at the end of generic line.", spot);
            if (cmd is DisableAwaitInput && contentIdx < Syntax.Content.Count - 1)
                Engine.Warn("[>] has not effect when placed inside generic line.", spot);
        }

        protected virtual void AddCommand (Command cmd)
        {
            if (cmd is ParametrizeGeneric param)
            {
                for (int i = InlinedCommands.Count - 1; i >= 0; i--)
                    if (InlinedCommands[i] is ParametrizeGeneric) break;
                    else if (InlinedCommands[i] is PrintText p) Parameterize(param, p);
                InlinedCommands.Add(cmd);
            }
            // Route [-] after printed text to wait input param of the print command.
            else if (cmd is I && InlinedCommands.LastOrDefault() is PrintText p1)
                p1.WaitForInput = true;
            else InlinedCommands.Add(cmd);
        }

        protected virtual void Parameterize (ParametrizeGeneric p, PrintText print)
        {
            if (Assigned(p.PrinterId)) print.PrinterId = Ref(p.PrinterId);
            if (Assigned(p.AuthorId)) print.AuthorId = Ref(p.AuthorId);
            if (Assigned(p.AuthorLabel)) print.AuthorLabel = Ref(p.AuthorLabel);
            if (Assigned(p.RevealSpeed)) print.RevealSpeed = Ref(p.RevealSpeed);
            if (Assigned(p.Join))
                if (!p.Join.DynamicValue) print.ResetPrinter = !p.Join;
                else throw Engine.Fail("Dynamic 'join' in [< ...] is not supported.", Spot);
            if (Assigned(p.SkipAwaitInput))
                if (!p.SkipAwaitInput.DynamicValue) print.WaitForInput = !p.SkipAwaitInput;
                else throw Engine.Fail("Dynamic 'skip' in [< ...] is not supported.", Spot);
            if (Assigned(p.NoWait))
                if (!p.NoWait.DynamicValue) print.Wait = print.WaitForInput = !p.NoWait;
                else throw Engine.Fail("Dynamic 'nowait' in [< ...] is not supported.", Spot);
        }

        protected virtual void AddGenericText (MixedValue genericText)
        {
            var printedBefore = InlinedCommands.Any(c => c is PrintText);
            var print = (PrintText)Activator.CreateInstance(printCommandType);
            var raw = mixedCompiler.Compile(genericText, true);
            print.Text = FromRaw<LocalizableTextParameter>(raw, Spot, out var errors);
            if (errors != null) errorHandler?.HandleError(new(errors, 0, 0));
            if (!string.IsNullOrEmpty(AuthorId)) print.AuthorId = AuthorId;
            if (printedBefore)
            {
                print.Append = true;
                print.ResetPrinter = false;
            }
            print.Wait = true;
            print.WaitForInput = false;
            print.PlaybackSpot = Spot;
            print.Indent = Syntax.Indent;
            AddCommand(print);
        }

        protected virtual void AddLastWaitInput ()
        {
            if (!InlinedCommands.Any(c => c is PrintText)) return;
            if (InlinedCommands.Any(c => c is DisableAwaitInput ||
                                         c is ParametrizeGeneric p && p.SkipAwaitInput)) return;
            var last = InlinedCommands.LastOrDefault();
            if (last is ParametrizeGeneric p &&
                (Assigned(p.SkipAwaitInput) && !p.SkipAwaitInput ||
                 Assigned(p.NoWait) && p.NoWait)) return;
            if (last is I) return;
            if (last is PrintText print) print.WaitForInput = true;
            else AddCommand(new I { PlaybackSpot = Spot, Indent = Syntax.Indent });
        }
    }
}

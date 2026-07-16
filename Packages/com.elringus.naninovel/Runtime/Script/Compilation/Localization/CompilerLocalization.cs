using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Syntax;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Locale-specific NaniScript compiler options.
    /// </summary>
    [CreateAssetMenu(menuName = "Naninovel/Compiler Localization", fileName = "NewNaniScriptL10n")]
    public class CompilerLocalization : ScriptableObject
    {
        [Tooltip("Marks beginning of comment lines. ';' by default.")]
        public string CommentLine = Symbols.Canon.CommentLine;
        [Tooltip("Marks beginning of label lines. '#' by default.")]
        public string LabelLine = Symbols.Canon.LabelLine;
        [Tooltip("Marks beginning of the command lines. '@' by default.")]
        public string CommandLine = Symbols.Canon.CommandLine;
        [Tooltip("Delimits author prefix from generic text content. ': ' by default.")]
        public string AuthorAssign = Symbols.Canon.AuthorAssign;
        [Tooltip("Delimits author appearance from author identifier in generic text line prefix. '.' by default.")]
        public string AuthorAppearance = Symbols.Canon.AuthorAppearance;
        [Tooltip("Marks beginning of script expression. '{' by default.")]
        public string ExpressionOpen = Symbols.Canon.ExpressionOpen;
        [Tooltip("Marks end of script expression. '}' by default.")]
        public string ExpressionClose = Symbols.Canon.ExpressionClose;
        [Tooltip("Marks beginning of inlined command in generic text line. '[' by default.")]
        public string InlinedOpen = Symbols.Canon.InlinedOpen;
        [Tooltip("Marks end of inlined command in generic text line. ']' by default.")]
        public string InlinedClose = Symbols.Canon.InlinedClose;
        [Tooltip("Delimits command parameter value from parameter identifier. ':' by default.")]
        public string ParameterAssign = Symbols.Canon.ParameterAssign;
        [Tooltip("Delimits items in list parameter value. ',' by default.")]
        public string ListDelimiter = Symbols.Canon.ListDelimiter;
        [Tooltip("Delimits value from name in named parameter value. '.' by default.")]
        public string NamedDelimiter = Symbols.Canon.NamedDelimiter;
        [Tooltip("Marks beginning of text identifier in localizable text parameter value and generic text line. '|#' by default.")]
        public string TextIdOpen = Symbols.Canon.TextIdOpen;
        [Tooltip("Marks end of text identifier in localizable text parameter value and generic text line. '|' by default.")]
        public string TextIdClose = Symbols.Canon.TextIdClose;
        [Tooltip("The flag placed before/after identifier of boolean command parameter to represent negative/positive value. '!' by default.")]
        public string BooleanFlag = Symbols.Canon.BooleanFlag;
        [Tooltip("Constant representing positive boolean value. 'true' by default.")]
        public string True = Symbols.Canon.True;
        [Tooltip("Constant representing negative boolean value. 'false' by default.")]
        public string False = Symbols.Canon.False;
        [Tooltip("Identifies start of a text tag.")]
        public string TagOpen = Symbols.Canon.TagOpen;
        [Tooltip("Identifies end of a text tag.")]
        public string TagClose = Symbols.Canon.TagClose;
        [Tooltip("Identifies command tag.")]
        public string CommandTag = Symbols.Canon.CommandTag;
        [Tooltip("Identifies expression tag.")]
        public string ExpressionTag = Symbols.Canon.ExpressionTag;
        [Tooltip("Identifies wait input tag.")]
        public string WaitInputTag = Symbols.Canon.WaitInputTag;
        [Tooltip("Identifies select tag.")]
        public string SelectTag = Symbols.Canon.SelectTag;
        [Tooltip("Used to delimit options in the select tag.")]
        public string OptionDelimiter = Symbols.Canon.OptionDelimiter;

        [Tooltip("Prefix to identify global script variables. 'g_' by default.")]
        public string GlobalVariablePrefix = "g_";
        [Tooltip("Prefix to identify script constants pulled from managed text. 't_' by default.")]
        public string ScriptConstantPrefix = "t_";

        [Tooltip("Locale-specific command aliases.")]
        public List<CommandLocalization> Commands = new();
        [Tooltip("Locale-specific expression function aliases.")]
        public List<FunctionLocalization> Functions = new();
        [Tooltip("Locale-specific constant aliases.")]
        public List<ConstantLocalization> Constants = new();

        public Symbols GetSymbols () => new() {
            CommentLine = CommentLine,
            LabelLine = LabelLine,
            CommandLine = CommandLine,
            AuthorAssign = AuthorAssign,
            AuthorAppearance = AuthorAppearance,
            ExpressionOpen = ExpressionOpen,
            ExpressionClose = ExpressionClose,
            InlinedOpen = InlinedOpen,
            InlinedClose = InlinedClose,
            ParameterAssign = ParameterAssign,
            ListDelimiter = ListDelimiter,
            NamedDelimiter = NamedDelimiter,
            TextIdOpen = TextIdOpen,
            TextIdClose = TextIdClose,
            BooleanFlag = BooleanFlag,
            True = True,
            TagOpen = TagOpen,
            TagClose = TagClose,
            CommandTag = CommandTag,
            ExpressionTag = ExpressionTag,
            WaitInputTag = WaitInputTag,
            SelectTag = SelectTag,
            OptionDelimiter = OptionDelimiter
        };

        [ContextMenu("Add Existing Commands")]
        private void AddExistingCommands ()
        {
            var hash = new HashSet<string>(Commands.Select(c => c.Id));
            foreach (var cmd in Command.CommandTypes.Values.OrderBy(t => t.Name))
                if (!hash.Contains(cmd.Name))
                    Commands.Add(new() {
                        Id = cmd.Name,
                        Parameters = AddExistingParameters(cmd)
                    });
        }

        [ContextMenu("Add Existing Functions")]
        private void AddExistingFunctions ()
        {
            var hash = new HashSet<string>(Functions.Select(c => c.MethodName));
            foreach (var fn in ExpressionFunctions.Resolve().DistinctBy(t => t.Method.Name).OrderBy(t => t.Method.Name))
                if (!hash.Contains(fn.Method.Name))
                    Functions.Add(new() {
                        MethodName = fn.Method.Name
                    });
        }

        private List<ParameterLocalization> AddExistingParameters (Type commandType)
        {
            return CommandParameter.ExtractFields(commandType)
                .OrderBy(t => t.Name)
                .Select(f => new ParameterLocalization {
                    Id = f.Name,
                    Alias = ""
                }).ToList();
        }
    }
}

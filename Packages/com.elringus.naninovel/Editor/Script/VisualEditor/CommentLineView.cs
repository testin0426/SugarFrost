using UnityEngine.UIElements;

namespace Naninovel
{
    public class CommentLineView : ScriptLineView
    {
        private readonly LineTextField valueField;

        public CommentLineView (int lineIndex, Syntax.CommentLine model, VisualElement container)
            : base(lineIndex, model.Indent, container)
        {
            var value = model.Comment;
            valueField = new(Compiler.Symbols.CommentLine, value);
            valueField.multiline = true;
            Content.Add(valueField);
        }

        public override string GenerateLineText ()
        {
            return $"{GenerateLineIndent()}{Compiler.Symbols.CommentLine} {valueField.value}";
        }
    }
}

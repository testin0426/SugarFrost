namespace Naninovel
{
    public class CommentLineCompiler : ScriptLineCompiler<CommentLine, Syntax.CommentLine>
    {
        protected override CommentLine Compile (Syntax.CommentLine stx)
        {
            return new(stx.Comment.Text, LineIndex, stx.Indent, LineHash);
        }
    }
}

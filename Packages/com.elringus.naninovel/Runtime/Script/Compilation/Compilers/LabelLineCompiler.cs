namespace Naninovel
{
    public class LabelLineCompiler : ScriptLineCompiler<LabelLine, Syntax.LabelLine>
    {
        protected override LabelLine Compile (Syntax.LabelLine stx)
        {
            return new(stx.Label.Text, LineIndex, stx.Indent, LineHash);
        }
    }
}

using System.Collections.Generic;
using Naninovel.Syntax;

namespace Naninovel
{
    public class MixedValueCompiler
    {
        private readonly List<RawValuePart> parts = new();
        private readonly ITextIdentifier identifier;

        public MixedValueCompiler (ITextIdentifier identifier)
        {
            this.identifier = identifier;
        }

        /// <summary>
        /// Compiles specified mixed value syntax into <see cref="RawValue"/>.
        /// </summary>
        public RawValue Compile (MixedValue stx, bool hashPlainText)
        {
            parts.Clear();
            foreach (var component in stx)
                if (component is PlainText plain)
                    if (hashPlainText) parts.Add(HashPlainText(plain));
                    else parts.Add(RawValuePart.FromPlainText(plain));
                else if (component is IdentifiedText idText) parts.Add(RawValuePart.FromIdentifiedText(idText.Id.Body));
                else if (component is Syntax.Expression expression) parts.Add(RawValuePart.FromExpression(expression.Body));
            return new(parts.ToArray());
        }

        private RawValuePart HashPlainText (PlainText plain)
        {
            var hash = ScriptTextIdentifier.VolatilePrefix + CryptoUtils.PersistentHexCode(plain.Text);
            identifier.Identify(hash, plain.Text);
            return RawValuePart.FromIdentifiedText(hash);
        }
    }
}

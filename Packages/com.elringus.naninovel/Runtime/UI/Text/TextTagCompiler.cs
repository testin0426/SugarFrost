using System;
using System.Text;
using JetBrains.Annotations;
using Naninovel.Utilities;
using TMPro;

namespace Naninovel.UI
{
    /// <summary>
    /// Compiles various text tags (ruby, tip, select, etc) in the scenario plain text
    /// into the TMPro-supported format.
    /// </summary>
    public class TextTagCompiler
    {
        public struct Options
        {
            public float RubySizeScale;
            [CanBeNull] public string RubyVerticalOffset;
            [CanBeNull] public string LinkTemplate;
            [CanBeNull] public string TipTemplate;
            [CanBeNull] public Action<string> OnTip;
            [CanBeNull] public Action<(int Index, string Body)> OnEvent;
            [CanBeNull] public Action<int> OnWaitInput;
            [CanBeNull] public Func<string, string> OnExpression;
            [CanBeNull] public Func<string, string> OnSelect;
        }

        public const string RubyLinkId = "NANINOVEL.RUBY";
        public const string TipIdPrefix = "NANINOVEL.TIP.";
        public const string TipTemplateLiteral = "%TIP%";
        public const string LinkTemplateLiteral = "%LINK%";

        private readonly StringBuilder builder = new(1024);
        private readonly Options options;
        private readonly TMP_Text tmp;

        public TextTagCompiler (TMP_Text tmp, Options options = default)
        {
            this.tmp = tmp;
            this.options = options;
        }

        public string Compile (string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            builder.Clear();
            builder.EnsureCapacity(text.Length + 32);
            CompileRange(text, 0, text.Length);
            return builder.ToString();
        }

        private void CompileRange (string content, int start, int end)
        {
            var i = start;
            while (i < end)
            {
                if (content[i] != '<') // not a tag
                {
                    // attempt to append a batch of plain text before the next tag, if any
                    var nextTag = content.AsSpan(i, end - i).IndexOf('<');
                    var next = nextTag >= 0 ? i + nextTag : end;
                    var len = next - i;
                    if (len > 0) builder.Append(content, i, len);
                    i = next;
                    continue;
                }

                if (TryExpression(content, end, ref i)) continue;
                if (TrySelect(content, end, ref i)) continue;
                if (TryEvent(content, end, ref i)) continue;
                if (TryWaitInput(content, end, ref i)) continue;
                if (TryRuby(content, end, ref i)) continue;
                if (TryTip(content, end, ref i)) continue;
                if (TryLink(content, end, ref i)) continue;

                // unknown tag
                var close = content.AsSpan(i, end - i).IndexOf('>');
                if (close >= 0) // append a batch of plain text until it's closed
                {
                    builder.Append(content, i, close + 1);
                    i += close + 1;
                }
                else // ...or to the end
                {
                    builder.Append(content, i, end - i);
                    i = end;
                }
            }
        }

        /// <summary>
        /// Replaces expression tag (<:...>) with evaluated expression result.
        /// </summary>
        private bool TryExpression (string content, int end, ref int i)
        {
            if (options.OnExpression is not { } onExpression) return false;
            if (i + 2 >= end || content[i] != '<' || content[i + 1] != ':') return false;
            var bodyStart = i + 2;
            var local = content.AsSpan(bodyStart, end - bodyStart).IndexOf('>');
            if (local < 0) return false;
            var close = bodyStart + local;

            var expression = new string(content.AsSpan(bodyStart, close - bodyStart));
            var result = onExpression(expression);
            if (!string.IsNullOrEmpty(result)) builder.Append(result);

            i = close + 1;
            return true;
        }

        /// <summary>
        /// Replaces select tag (</...>) with evaluated expression result.
        /// </summary>
        private bool TrySelect (string content, int end, ref int i)
        {
            if (options.OnSelect is not { } onSelect) return false;
            if (i + 2 >= end || content[i] != '<' || content[i + 1] != '/') return false;

            var bodyStart = i + 2;
            var local = content.AsSpan(bodyStart, end - bodyStart).IndexOf('>');
            if (local < 0) return false;
            var close = bodyStart + local;

            // ignore closing tags, like </i>; require multiple '/' inside the body
            var bodySpan = content.AsSpan(bodyStart, close - bodyStart);
            if (bodySpan.IndexOf('/') < 0) return false;

            var bodyLen = close - bodyStart;
            var slashCount = 0;
            foreach (var c in content.AsSpan(bodyStart, bodyLen))
                if (c == '/')
                    slashCount++;
            var expressionLen = 10 + bodyLen + 2 * slashCount;
            var expression = string.Create<(string content, int start, int len)>(expressionLen,
                (content, bodyStart, bodyLen), static (o, s) => { // @formatter:off
                    o[0] = 's'; o[1] = 'e'; o[2] = 'l'; o[3] = 'e'; o[4] = 'c'; o[5] = 't'; o[6] = '('; o[7] = '"';
                    var di = 8;
                    var src = s.content.AsSpan(s.start, s.len);
                    for (var si = 0; si < src.Length; si++)
                        if (src[si] == '/') { o[di++] = '"'; o[di++] = ','; o[di++] = '"'; }
                        else o[di++] = src[si];
                    o[di++] = '"'; o[di] = ')';
                }); // @formatter:on 
            var result = onSelect(expression);
            if (!string.IsNullOrEmpty(result)) builder.Append(result);

            i = close + 1;
            return true;
        }

        /// <summary>
        /// Registers event tag (<@...>) and removes them from specified content.
        /// </summary>
        private bool TryEvent (string content, int end, ref int i)
        {
            if (options.OnEvent is not { } onEvent) return false;
            if (i + 2 >= end || content[i] != '<' || content[i + 1] != '@') return false;
            var bodyStart = i + 2;
            var local = content.AsSpan(bodyStart, end - bodyStart).IndexOf('>');
            if (local < 0) return false;
            var close = bodyStart + local;

            var body = new string(content.AsSpan(bodyStart, close - bodyStart));
            onEvent.Invoke((builder.Length, body));

            i = close + 1;
            return true;
        }

        /// <summary>
        /// Registers wait input event tag (<->) and removes them from specified content.
        /// </summary>
        private bool TryWaitInput (string content, int end, ref int i)
        {
            if (options.OnWaitInput is not { } waitInput) return false;
            if (i + 2 >= end || content[i] != '<' || content[i + 1] != '-') return false;
            var bodyStart = i + 2;
            var local = content.AsSpan(bodyStart, end - bodyStart).IndexOf('>');
            if (local < 0) return false;
            var close = bodyStart + local;

            waitInput.Invoke(builder.Length);

            i = close + 1;
            return true;
        }

        /// <summary>
        /// Given the input, extracts text wrapped in tip tags and replace it with tags natively supported by TMP.
        /// </summary>
        private bool TryTip (string content, int end, ref int i)
        {
            const string open = "<tip=\"";
            const string close = "</tip>";
            if (i + open.Length > end || !content.AsSpan(i, open.Length).SequenceEqual(open)) return false;

            var idStart = i + open.Length;
            var qLocal = content.AsSpan(idStart, end - idStart).IndexOf('"');
            if (qLocal < 0) return false;
            var idEnd = idStart + qLocal;

            var afterIdQuote = idEnd + 1;
            if (afterIdQuote >= end || content[afterIdQuote] != '>') return false;

            var innerStart = afterIdQuote + 1;
            var cLocal = content.AsSpan(innerStart, end - innerStart).IndexOf(close.AsSpan());
            if (cLocal < 0) return false;
            var closeStart = innerStart + cLocal;

            if (options.OnTip is { } onTip)
                onTip.Invoke(new string(content.AsSpan(idStart, idEnd - idStart)));

            builder.Append("<link=\"");
            builder.Append(TipIdPrefix);
            builder.Append(content, idStart, idEnd - idStart);
            builder.Append("\">");

            var template = options.TipTemplate ?? string.Empty;
            var marker = TipTemplateLiteral;

            var scan = 0;
            while (true) // replace all occurrences of the marker
            {
                var mPos = template.IndexOf(marker, scan, StringComparison.Ordinal);
                if (mPos < 0) break;
                if (mPos > scan) builder.Append(template, scan, mPos - scan);
                CompileRange(content, innerStart, closeStart);
                scan = mPos + marker.Length;
            }
            if (scan < template.Length)
                builder.Append(template, scan, template.Length - scan);

            builder.Append("</link>");

            i = closeStart + close.Length;
            return true;
        }

        /// <summary>
        /// When 'LinkTemplate' is assigned, will modify the link content in accordance with the template.
        /// </summary>
        private bool TryLink (string content, int end, ref int i)
        {
            const string open = "<link=\"";
            const string close = "</link>";
            if (i + open.Length > end || !content.AsSpan(i, open.Length).SequenceEqual(open)) return false;

            var idStart = i + open.Length;
            var qLocal = content.AsSpan(idStart, end - idStart).IndexOf('"');
            if (qLocal < 0) return false;
            var idEnd = idStart + qLocal;

            var afterIdQuote = idEnd + 1;
            if (afterIdQuote >= end || content[afterIdQuote] != '>') return false;

            var innerStart = afterIdQuote + 1;
            var cLocal = content.AsSpan(innerStart, end - innerStart).IndexOf(close.AsSpan());
            if (cLocal < 0) return false;
            var closeStart = innerStart + cLocal;

            var template = options.LinkTemplate;
            var marker = LinkTemplateLiteral;

            if (string.IsNullOrEmpty(template) || template.IndexOf(marker, StringComparison.Ordinal) < 0)
            {
                builder.Append("<link=\"");
                builder.Append(content, idStart, idEnd - idStart);
                builder.Append("\">");
                CompileRange(content, innerStart, closeStart);
            }
            else
            {
                builder.Append("<link=\"");
                builder.Append(content, idStart, idEnd - idStart);
                builder.Append("\">");

                var scan = 0;
                while (true) // replace all occurrences of the marker
                {
                    var mPos = template.IndexOf(marker, scan, StringComparison.Ordinal);
                    if (mPos < 0) break;
                    if (mPos > scan) builder.Append(template, scan, mPos - scan);
                    CompileRange(content, innerStart, closeStart);
                    scan = mPos + marker.Length;
                }
                if (scan < template.Length)
                    builder.Append(template, scan, template.Length - scan);
            }

            builder.Append(close);

            i = closeStart + close.Length;
            return true;
        }

        /// <summary>
        /// Given the input, extracts text wrapped in ruby tag and replace it with tags natively supported by TMP.
        /// </summary>
        private bool TryRuby (string content, int end, ref int i)
        {
            const string open = "<ruby=\"";
            const string close = "</ruby>";
            if (i + open.Length > end || !content.AsSpan(i, open.Length).SequenceEqual(open)) return false;

            var rubyStart = i + open.Length;
            var qLocal = content.AsSpan(rubyStart, end - rubyStart).IndexOf('"');
            if (qLocal < 0) return false;
            var rubyEnd = rubyStart + qLocal;

            var afterValQuote = rubyEnd + 1;
            if (afterValQuote >= end || content[afterValQuote] != '>') return false;

            var innerStart = afterValQuote + 1;
            var cLocal = content.AsSpan(innerStart, end - innerStart).IndexOf(close.AsSpan());
            if (cLocal < 0) return false;
            var closeStart = innerStart + cLocal;

            // Prepending zero-width space before the annotated text and wrapping it with no-break tag
            // to prevent both normal and hard wrapping, because ruby should never spread across lines.
            builder.Append('\u200B').Append("<nobr>");
            var baseStart = builder.Length;
            CompileRange(content, innerStart, closeStart);
            var baseLen = builder.Length - baseStart;

            var margin = ((tmp.margin.x > 0 ? tmp.margin.x : 0) + (tmp.margin.z > 0 ? tmp.margin.z : 0)) / 2f;
            var baseStr = baseLen > 0 ? builder.ToString(baseStart, baseLen) : string.Empty;
            var rubyStr = new string(content.AsSpan(rubyStart, rubyEnd - rubyStart));
            var baseWidth = tmp.GetPreferredValues(baseStr).x - margin;
            var rubyWidth = tmp.GetPreferredValues(rubyStr).x * options.RubySizeScale - margin;
            var rubyOffset = -(baseWidth + rubyWidth - margin) / 2f;
            var reverseOffset = (baseWidth - rubyWidth - margin) / 2f;

            builder.Append("<space=").AppendFloat(rubyOffset).Append('>');
            builder.Append("<voffset=").Append(options.RubyVerticalOffset ?? "0").Append('>');
            builder.Append("<size=").AppendFloat(options.RubySizeScale * 100f).Append("%>");
            builder.Append("<link=\"").Append(RubyLinkId).Append("\">");
            builder.Append(content, rubyStart, rubyEnd - rubyStart);
            builder.Append("</link></size></voffset>");
            builder.Append("<space=").AppendFloat(reverseOffset).Append('>');
            builder.Append("</nobr>");

            i = closeStart + close.Length;
            return true;
        }
    }
}

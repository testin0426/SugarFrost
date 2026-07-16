using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Naninovel.ArabicSupport;

namespace Naninovel.UI
{
    /// <summary>
    /// Wrapper over TMPro text with Naninovel-specific tags and arabic text support.
    /// </summary>
    [AddComponentMenu("Naninovel/ UI/Naninovel Text")]
    public class NaninovelTMProText : TextMeshProUGUI, IPointerClickHandler
    {
        [Serializable]
        public class LinkClickedEvent : UnityEvent<TMP_LinkInfo> { }

        public override string text { get => unprocessedText; set => SetTMProText(value); }

        protected virtual bool ContinueTrigger => false;
        protected virtual string RubyVerticalOffset => rubyVerticalOffset;
        protected virtual float RubySizeScale => rubySizeScale;
        protected virtual bool UnlockTipsOnPrint => unlockTipsOnPrint;
        protected virtual bool FixArabicText => fixArabicText;
        protected virtual string TipTemplate => tipTemplate;
        protected virtual string LinkTemplate => linkTemplate;
        protected virtual Canvas TopmostCanvas => topmostCanvasCache ? topmostCanvasCache : topmostCanvasCache = gameObject.FindTopmostComponent<Canvas>();
        protected virtual bool Edited => !Application.isPlaying || ObjectUtils.IsEditedInPrefabMode(gameObject);

        [Tooltip("Vertical line offset to use for the ruby (furigana) text; supported units: em, px, %.")]
        [SerializeField] private string rubyVerticalOffset = "1em";
        [Tooltip("Font size scale (relative to the main text font size) to apply for the ruby (furigana) text.")]
        [SerializeField] private float rubySizeScale = .5f;
        [Tooltip("Whether to automatically unlock associated tip records when text wrapped in <tip> tags is printed.")]
        [SerializeField] private bool unlockTipsOnPrint = true;
        [Tooltip("Template to use when processing text wrapped in <tip> tags. " + TextTagCompiler.TipTemplateLiteral + " will be replaced with the actual tip content.")]
        [SerializeField] private string tipTemplate = $"<u>{TextTagCompiler.TipTemplateLiteral}</u>";
        [Tooltip("Invoked when a text wrapped in <tip> tags is clicked; returned string argument is the ID of the clicked tip. Be aware, that the default behaviour (showing 'ITipsUI' when a tip is clicked) won't be invoked when a custom handler is assigned.")]
        [SerializeField] private StringUnityEvent onTipClicked;
        [Tooltip("Whether to modify the text to support arabic languages (fix letters connectivity issues).")]
        [SerializeField] private bool fixArabicText;
        [Tooltip("When 'Fix Arabic Text' is enabled, controls to whether also fix Farsi characters.")]
        [SerializeField] private bool fixArabicFarsi = true;
        [Tooltip("When 'Fix Arabic Text' is enabled, controls to whether also fix rich text tags.")]
        [SerializeField] private bool fixArabicTextTags = true;
        [Tooltip("When 'Fix Arabic Text' is enabled, controls to whether preserve numbers.")]
        [SerializeField] private bool fixArabicPreserveNumbers;
        [Tooltip("Template to use when processing text wrapped in <link> tags. " + TextTagCompiler.LinkTemplateLiteral + " will be replaced with the actual tip content. When nothing is specified, the link tags won't be modified.")]
        [SerializeField] private string linkTemplate = $"<u>{TextTagCompiler.LinkTemplateLiteral}</u>";
        [Tooltip("Invoked when a text wrapped in <link> tags is clicked.")]
        [SerializeField] private LinkClickedEvent onLinkClicked;

        private static readonly int rubyLinkHash = TMP_TextUtilities.GetSimpleHashCode(TextTagCompiler.RubyLinkId);
        private readonly FastStringBuilder arabicBuilder = new(RTLSupport.DefaultBufferSize);
        private readonly SortedList<int, string> eventByCharIdx = new();
        private TextTagCompiler tagCompiler;
        private string unprocessedText = "";
        private Canvas topmostCanvasCache;

        public virtual void OnPointerClick (PointerEventData evt)
        {
            var renderCamera = TopmostCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : TopmostCanvas.worldCamera;
            var idx = TMP_TextUtilities.FindIntersectingLink(this, evt.position, renderCamera);
            if (idx >= 0 && textInfo.linkInfo[idx].hashCode != rubyLinkHash) OnLinkClicked(textInfo.linkInfo[idx]);
            else if (ContinueTrigger) Engine.GetService<IInputManager>()?.GetContinue()?.Pulse();
        }

        /// <summary>
        /// Whether event tags placed at or before the specified character index exists.
        /// </summary>
        public virtual bool HasEventsAtChar (int index)
        {
            return eventByCharIdx.Count > 0 && eventByCharIdx.Keys[0] <= index;
        }

        /// <summary>
        /// Collects bodies of the event tags placed at or before the specified character index to the specified collection.
        /// Collected bodies are removed and won't be added on subsequent invocations.
        /// </summary>
        public virtual void PopEventsAtChar (int index, ICollection<string> events)
        {
            while (eventByCharIdx.Count > 0)
            {
                if (eventByCharIdx.Keys[0] > index) break;
                events.Add(eventByCharIdx.Values[0]);
                eventByCharIdx.RemoveAt(0);
            }
        }

        protected override void Awake ()
        {
            base.Awake();
            if (!Edited) text = base.text ?? "";
        }

        protected virtual void OnLinkClicked (TMP_LinkInfo linkInfo)
        {
            if (onLinkClicked?.GetPersistentEventCount() > 0)
                onLinkClicked.Invoke(linkInfo);

            var linkId = linkInfo.GetLinkID();
            if (linkId.StartsWithOrdinal(TextTagCompiler.TipIdPrefix))
                OnTipClicked(linkId.GetAfter(TextTagCompiler.TipIdPrefix));
        }

        protected virtual void OnTipClicked (string tipId)
        {
            if (onTipClicked?.GetPersistentEventCount() > 0)
            {
                onTipClicked.Invoke(tipId);
                return;
            }

            if (Engine.GetService<IUIManager>()?.GetUI<ITipsUI>() is { } tips)
            {
                tips.SelectTipRecord(tipId);
                tips.Show();
            }
        }

        protected virtual void SetTMProText (string text)
        {
            eventByCharIdx.Clear();
            unprocessedText = text;
            tagCompiler ??= CreateTagCompiler(); // keep the compiler init here, because Awake is not reliable
            base.text = FixArabic(tagCompiler.Compile(text));
        }

        protected virtual string FixArabic (string value)
        {
            if (!FixArabicText || string.IsNullOrWhiteSpace(value)) return value;
            arabicBuilder.Clear();
            RTLSupport.FixRTL(value, arabicBuilder, fixArabicFarsi, fixArabicTextTags, fixArabicPreserveNumbers);
            arabicBuilder.Reverse();
            return arabicBuilder.ToString();
        }

        /// <summary>
        /// Whether character with the specified index is inside a ruby tag.
        /// </summary>
        protected virtual bool IsRuby (int charIndex)
        {
            for (int i = 0; i < textInfo.linkCount; i++)
                if (IsRubyLinkAndContainsChar(charIndex, textInfo.linkInfo[i]))
                    return true;
            return false;

            static bool IsRubyLinkAndContainsChar (int charIndex, TMP_LinkInfo link) =>
                link.hashCode == rubyLinkHash &&
                charIndex >= link.linkTextfirstCharacterIndex &&
                charIndex < link.linkTextfirstCharacterIndex + link.linkTextLength;
        }

        protected virtual TextTagCompiler CreateTagCompiler () => new(this, new() {
            TipTemplate = TipTemplate,
            OnTip = UnlockTipsOnPrint ? id => Engine.GetService<IUnlockableManager>()?.UnlockItem($"Tips/{id}") : null,
            OnEvent = e => eventByCharIdx[e.Index] = e.Body,
            OnWaitInput = i => eventByCharIdx[i] = "-",
            OnExpression = e => ExpressionEvaluator.Evaluate<string>(e, new() { OnError = e => Engine.Err(e) }),
            OnSelect = e => ExpressionEvaluator.Evaluate<string>(e, new() { OnError = e => Engine.Err(e) }),
            LinkTemplate = LinkTemplate,
            RubyVerticalOffset = RubyVerticalOffset,
            RubySizeScale = RubySizeScale
        });
    }
}

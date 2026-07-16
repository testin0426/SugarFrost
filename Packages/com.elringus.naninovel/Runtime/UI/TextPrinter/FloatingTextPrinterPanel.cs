using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// A <see cref="UITextPrinterPanel"/> implementation with a spatial behaviour.
    /// </summary>
    public class FloatingTextPrinterPanel : RevealableTextPrinterPanel
    {
        public enum YGrowDir { None, Top, Bottom }
        public enum XGrowDir { None, Left, Right }

        protected virtual XGrowDir XGrow => xGrow;
        protected virtual YGrowDir YGrow => yGrow;
        protected virtual IReadOnlyCollection<RectTransform> FlipContent => flipContent;
        protected virtual bool FlipToFit => flipToFit;
        protected virtual bool HideWithAuthor => hideWithAuthor;
        protected virtual bool HideOnAuthorMove => hideOnAuthorMove;
        protected virtual string AnchorToAuthor => anchorToAuthor;
        protected virtual bool LookWithAuthor => lookWithAuthor;

        protected virtual bool ContentDirty { get; set; }
        protected virtual bool LookFlipped { get; private set; }
        protected virtual bool FlippedX => flipContent?.Length > 0 && flipContent[0].localScale.x < 0;
        protected virtual bool FlippedY => flipContent?.Length > 0 && flipContent[0].localScale.y < 0;
        [CanBeNull] protected virtual IActorAnchor Anchor { get; private set; }

        protected virtual string TopLeftAnchorId { get; private set; }
        protected virtual string TopAnchorId { get; private set; }
        protected virtual string TopRightAnchorId { get; private set; }
        protected virtual string LeftAnchorId { get; private set; }
        protected virtual string RightAnchorId { get; private set; }
        protected virtual string BottomLeftAnchorId { get; private set; }
        protected virtual string BottomAnchorId { get; private set; }
        protected virtual string BottomRightAnchorId { get; private set; }
        protected virtual string FallbackAnchorId => AnchorToAuthor;

        [Header("Floating Printer Setup")]
        [Tooltip("The content of the printer, which will be subject to flipping (mirroring) due to screen fitting or author look direction alignment.")]
        [SerializeField] private RectTransform[] flipContent;
        [Tooltip("The direction of the printer content expansion over the X-axis. Affects horizontal fit and author look alignment behaviour.")]
        [SerializeField] private XGrowDir xGrow = XGrowDir.Right;
        [Tooltip("The direction of the printer content expansion over the Y-axis (vertically). Affects vertical fit behaviour.")]
        [SerializeField] private YGrowDir yGrow = YGrowDir.Top;
        [Header("Floating Printer Behaviour")]
        [Tooltip("Whether to flip the 'Flip Content' when the content is overflowing the UI canvas rectangle over the expanding sides. The sides are resolved from the 'X/Y Grow' properties.")]
        [SerializeField] private bool flipToFit = true;
        [Tooltip("Whether to hide the printer when the author actor is hidden.")]
        [SerializeField] private bool hideWithAuthor = true;
        [Tooltip("Whether to hide the printer when the author actor is moved.")]
        [SerializeField] private bool hideOnAuthorMove = true;
        [Tooltip("Specify an anchor ID to keep the printer position in sync with the author actor.")]
        [SerializeField] private string anchorToAuthor = "Bubble";
        [Tooltip("Whether to keep the printer in sync with the author look direction. When the author's and printer look direction are not equal, it'll flip. Printer's 'look direction' is resolved from the 'X Grow' property.")]
        [SerializeField] private bool lookWithAuthor = true;

        private readonly Vector3[] corners = new Vector3[4];
        private Vector3 lastContentPosition;
        private Vector2 lastContentSize;

        protected override void Awake ()
        {
            base.Awake();
            TopLeftAnchorId = $"{AnchorToAuthor}/TopLeft";
            TopAnchorId = $"{AnchorToAuthor}/Top";
            TopRightAnchorId = $"{AnchorToAuthor}/TopRight";
            LeftAnchorId = $"{AnchorToAuthor}/Left";
            RightAnchorId = $"{AnchorToAuthor}/Right";
            BottomLeftAnchorId = $"{AnchorToAuthor}/BottomLeft";
            BottomAnchorId = $"{AnchorToAuthor}/Bottom";
            BottomRightAnchorId = $"{AnchorToAuthor}/BottomRight";
        }

        protected virtual void LateUpdate ()
        {
            if (!Visible) return;
            TrackContentChanges();
            if (ContentDirty) HandleContentDirty();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();
            if (Anchor != null) Anchor.OnPositionChanged -= HandleAnchorPositionChanged;
            if (AuthorChara is { } author)
            {
                author.OnVisibilityChanged -= HandleAuthorVisibilityChanged;
                author.OnPositionChanged -= HandleAuthorPositionChanged;
                author.OnLookDirectionChanged -= HandleAuthorLookDirectionChanged;
            }
        }

        protected override void SetMessageAuthor (MessageAuthor author)
        {
            var prevAuthorId = Author.Id;

            if (AuthorChara is { } prevChara)
            {
                prevChara.OnVisibilityChanged -= HandleAuthorVisibilityChanged;
                prevChara.OnPositionChanged -= HandleAuthorPositionChanged;
                prevChara.OnLookDirectionChanged -= HandleAuthorLookDirectionChanged;
            }

            base.SetMessageAuthor(author);

            if (AuthorChara != null)
            {
                AuthorChara.OnVisibilityChanged += HandleAuthorVisibilityChanged;
                AuthorChara.OnPositionChanged += HandleAuthorPositionChanged;
                AuthorChara.OnLookDirectionChanged += HandleAuthorLookDirectionChanged;
                HandleAuthorLookDirectionChanged(AuthorChara.LookDirection);
            }

            if (prevAuthorId != Author.Id)
                ContentDirty = true;
        }

        protected virtual void TrackContentChanges ()
        {
            var position = Content.localPosition;
            var size = Content.sizeDelta;
            if (position != lastContentPosition || size != lastContentSize)
                ContentDirty = true;
            lastContentPosition = position;
            lastContentSize = size;
        }

        protected virtual void HandleAuthorVisibilityChanged (bool visible)
        {
            if (!visible && HideWithAuthor) Hide();
        }

        protected virtual void HandleAuthorPositionChanged (Vector3 position)
        {
            if (HideOnAuthorMove) Hide();
        }

        protected virtual void HandleAuthorLookDirectionChanged (CharacterLookDirection dir)
        {
            if (XGrow == XGrowDir.None || FlipContent == null) return;
            var flipped = dir != CharacterLookDirection.Center &&
                          (dir == CharacterLookDirection.Left && XGrow == XGrowDir.Right ||
                           dir == CharacterLookDirection.Right && XGrow == XGrowDir.Left);
            var changed = flipped != LookFlipped;
            LookFlipped = flipped;
            if (changed) ContentDirty = true;
        }

        protected virtual void HandleAnchorPositionChanged (Vector3 position)
        {
            if (AnchoringAllowed)
                Content.position = position;
        }

        protected virtual void HandleContentDirty ()
        {
            GetPreferredFlip(out var flipX, out var flipY);
            SetContentFlip(flipX, flipY);
            SetAnchor(GetPreferredAnchor(flipX, flipY));
            ContentDirty = false;
        }

        protected virtual void SetAnchor ([CanBeNull] IActorAnchor anchor)
        {
            if (Anchor != null) Anchor.OnPositionChanged -= HandleAnchorPositionChanged;
            if ((Anchor = anchor) != null)
            {
                Anchor.OnPositionChanged += HandleAnchorPositionChanged;
                HandleAnchorPositionChanged(Anchor.Position);
            }
        }

        [CanBeNull]
        protected virtual IActorAnchor GetPreferredAnchor (bool flippedX, bool flippedY)
        {
            if (string.IsNullOrWhiteSpace(AnchorToAuthor) || string.IsNullOrEmpty(Author.Id)) return null;
            if (XGrow == XGrowDir.None && YGrow == YGrowDir.None) return Get(AnchorToAuthor);

            var top = YGrow == YGrowDir.Top && !flippedY || YGrow == YGrowDir.Bottom && flippedY;
            var bottom = YGrow == YGrowDir.Bottom && !flippedY || YGrow == YGrowDir.Top && flippedY;
            var left = XGrow == XGrowDir.Left && !flippedX || XGrow == XGrowDir.Right && flippedX;
            var right = XGrow == XGrowDir.Right && !flippedX || XGrow == XGrowDir.Left && flippedX;
            var anchor = default(IActorAnchor);

            if (top && left) anchor = Get(TopLeftAnchorId) ?? Get(TopAnchorId) ?? Get(LeftAnchorId);
            else if (top && right) anchor = Get(TopRightAnchorId) ?? Get(TopAnchorId) ?? Get(RightAnchorId);
            else if (top) anchor = Get(TopAnchorId) ?? Get(TopRightAnchorId) ?? Get(TopLeftAnchorId);
            else if (bottom && left) anchor = Get(BottomLeftAnchorId) ?? Get(BottomAnchorId) ?? Get(LeftAnchorId);
            else if (bottom && right) anchor = Get(BottomRightAnchorId) ?? Get(BottomAnchorId) ?? Get(RightAnchorId);
            else if (bottom) anchor = Get(BottomAnchorId) ?? Get(BottomRightAnchorId) ?? Get(BottomLeftAnchorId);
            else if (left) anchor = Get(LeftAnchorId) ?? Get(TopLeftAnchorId) ?? Get(BottomLeftAnchorId);
            else if (right) anchor = Get(RightAnchorId) ?? Get(TopRightAnchorId) ?? Get(BottomRightAnchorId);
            return anchor ?? Get(FallbackAnchorId);

            IActorAnchor Get (string anchorId) => ActorAnchors.Get(Author.Id, anchorId);
        }

        protected virtual void GetPreferredFlip (out bool flipX, out bool flipY)
        {
            flipX = false;
            flipY = false;

            if (FlipToFit)
            {
                GetContentOverflow(false, false, out var overLeft, out var overRight, out var overBottom, out var overTop);
                flipX = FlipToFit && (XGrow == XGrowDir.Left && overLeft || XGrow == XGrowDir.Right && overRight);
                flipY = FlipToFit && (YGrow == YGrowDir.Bottom && overBottom || YGrow == YGrowDir.Top && overTop);
            }

            if (!flipX && LookFlipped)
                if (FlipToFit) // flip not required for fit, but requested by look — check if it'll fit
                {
                    GetContentOverflow(true, false, out var overFlipLeft, out var overFlipRight, out _, out _);
                    flipX = XGrow == XGrowDir.Left && !overFlipRight || XGrow == XGrowDir.Right && !overFlipLeft;
                }
                else flipY = true;
        }

        /// <remarks>The overflow checked as if the content is not flipped, unless flipX/flipY enabled.</remarks>
        protected virtual void GetContentOverflow (
            bool flipX, bool flipY,
            out bool overLeft, out bool overRight,
            out bool overBottom, out bool overTop)
        {
            RectTransform.GetWorldCorners(corners);
            var canvasMin = corners[0];
            var canvasMax = corners[0];
            for (var i = 1; i < 4; i++)
            {
                canvasMin = Vector3.Min(canvasMin, corners[i]);
                canvasMax = Vector3.Max(canvasMax, corners[i]);
            }

            var offset = Vector3.zero; // required to predict the anchored position associated with the flip state
            if (Anchor != null)
                if (GetPreferredAnchor(flipX, flipY) is { } targetAnchor)
                    offset = Anchor.Position - targetAnchor.Position;

            var contentMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0f);
            var contentMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0f);
            foreach (var content in FlipContent) // don't just check the Content, as we're flipping the children
            {
                content.GetWorldCorners(corners);
                var pivot = content.position - offset;
                for (var i = 0; i < 4; i++)
                {
                    var corner = corners[i] - offset;
                    if (FlippedX ^ flipX) corner.x = 2f * pivot.x - corner.x;
                    if (FlippedY ^ flipY) corner.y = 2f * pivot.y - corner.y;
                    contentMin = Vector3.Min(contentMin, corner);
                    contentMax = Vector3.Max(contentMax, corner);
                }
            }

            overLeft = contentMin.x < canvasMin.x;
            overRight = contentMax.x > canvasMax.x;
            overBottom = contentMin.y < canvasMin.y;
            overTop = contentMax.y > canvasMax.y;
        }

        protected virtual void SetContentFlip (bool? flipX = null, bool? flipY = null)
        {
            if (FlipContent == null) return;
            if ((!flipX.HasValue || flipX == FlippedX) && (!flipY.HasValue || flipY == FlippedY)) return;
            var negateX = flipX ?? FlippedX;
            var negateY = flipY ?? FlippedY;
            var scale = new Vector3(negateX ? -1 : 1, negateY ? -1 : 1, 1);
            foreach (var trs in FlipContent)
                trs.localScale = scale;
        }
    }
}

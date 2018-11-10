using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FreeArrangePanel.Controls;
using FreeArrangePanel.Helpers;

namespace FreeArrangePanel.Adorners
{
    /// <summary>
    ///     Specifies visual element render mode.
    /// </summary>
    public enum RenderMode
    {
        /// <summary>
        ///     Does not render the visual element.
        /// </summary>
        None,

        /// <summary>
        ///     Renders the visual element INSIDE AdornedElement's bounds.
        /// </summary>
        Inside,

        /// <summary>
        ///     Renders the visual element OUTSIDE AdornedElement's bounds.
        /// </summary>
        Outside
    }

    /// <inheritdoc />
    /// <summary>
    ///     Adorner used to render element selection and resize handles.
    /// </summary>
    public class ArrangeAdorner : Adorner
    {
        #region Public

        #region Constructors

        public ArrangeAdorner(UIElement adornedElement) : base(adornedElement)
        {
            SelectionRenderMode = RenderMode.None;
            ResizeHandleRenderMode = RenderMode.None;
            SelectionFill = null;
            SelectionStroke = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF));
            SelectionStrokeThickness = 2.0;
            HandleFill = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            HandleStroke = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
            HandleStrokeThickness = 1.0;
            HandleSize = 10;
            mVisualChildren = new VisualCollection(this);
            mSelectionOverlay = new SelectionOverlay(this);
            mVisualChildren.Add(mSelectionOverlay);
            InitializeResizeHandles();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the <see cref="RenderMode" /> used for rendering selection rectangle.
        /// </summary>
        public RenderMode SelectionRenderMode
        {
            get => mSelectionRenderMode;
            set
            {
                mSelectionRenderMode = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets the <see cref="RenderMode" /> used for rendering resize handles.
        /// </summary>
        public RenderMode ResizeHandleRenderMode
        {
            get => mResizeHandleRenderMode;
            set
            {
                mResizeHandleRenderMode = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="Brush" /> that paints the interior area of the selection rectangle.
        /// </summary>
        public Brush SelectionFill
        {
            get => mSelectionFill;
            set
            {
                mSelectionFill = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="Brush" /> that specifies how the selection rectangle outline is painted.
        /// </summary>
        public Brush SelectionStroke
        {
            get => mSelectionStroke;
            set
            {
                mSelectionStroke = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="double" /> that specifies the width of the selection rectangle outline.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double SelectionStrokeThickness
        {
            get => mSelectionStrokeThickness;
            set
            {
                mSelectionStrokeThickness = value > 0 ? value : 0;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="Brush" /> that paints the interior area of the resize handle.
        /// </summary>
        public Brush HandleFill
        {
            get => mHandleFill;
            set
            {
                mHandleFill = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="Brush" /> that specifies how the resize handle outline is painted.
        /// </summary>
        public Brush HandleStroke
        {
            get => mHandleStroke;
            set
            {
                mHandleStroke = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="double" /> that specifies the width of the resize handle outline.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double HandleStrokeThickness
        {
            get => mHandleStrokeThickness;
            set
            {
                mHandleStrokeThickness = value > 0 ? value : 0;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="double" /> that specifies the resize handle size.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double HandleSize
        {
            get => mHandleSize;
            set
            {
                mHandleSize = value > 0 ? value : 0;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        #endregion

        #endregion

        #region Protected

        protected override Size ArrangeOverride(Size finalSize)
        {
            var desiredWidth = AdornedElement.DesiredSize.Width;
            var desiredHeight = AdornedElement.DesiredSize.Height;

            var arrangeMode = Controls.FreeArrangePanel.GetArrangeMode(AdornedElement);

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);
            var strokeThickness = SelectionStrokeThickness * scaleFactor;
            var handleSize = HandleSize * scaleFactor;

            var rectSelection = new Rect();
            var rectLeft = new Rect();
            var rectRight = new Rect();
            var rectTop = new Rect();
            var rectBottom = new Rect();
            var rectTopLeft = new Rect();
            var rectTopRight = new Rect();
            var rectBottomLeft = new Rect();
            var rectBottomRight = new Rect();

            if (SelectionRenderMode == RenderMode.Inside)
                rectSelection = new Rect(0, 0, desiredWidth, desiredHeight);
            else if (SelectionRenderMode == RenderMode.Outside)
                rectSelection = new Rect(-strokeThickness, -strokeThickness,
                    desiredWidth + strokeThickness * 2, desiredHeight + strokeThickness * 2);

            if ((arrangeMode & ~ArrangeMode.MoveOnly) != ArrangeMode.None)
            {
                if (ResizeHandleRenderMode == RenderMode.Inside)
                {
                    if ((arrangeMode & ArrangeMode.ResizeHorizontal) != 0)
                    {
                        rectLeft = new Rect(0, handleSize, handleSize, desiredHeight - 2 * handleSize);
                        rectRight = new Rect(desiredWidth - handleSize, handleSize,
                            handleSize, desiredHeight - 2 * handleSize);
                    }

                    if ((arrangeMode & ArrangeMode.ResizeVertical) != 0)
                    {
                        rectTop = new Rect(handleSize, 0, desiredWidth - 2 * handleSize, handleSize);
                        rectBottom = new Rect(handleSize, desiredHeight - handleSize,
                            desiredWidth - 2 * handleSize, handleSize);
                    }

                    if ((arrangeMode & ArrangeMode.ResizeNWSE) != 0)
                    {
                        rectTopLeft = new Rect(0, 0, handleSize, handleSize);
                        rectBottomRight = new Rect(desiredWidth - handleSize, desiredHeight - handleSize,
                            handleSize, handleSize);
                    }

                    if ((arrangeMode & ArrangeMode.ResizeNESW) != 0)
                    {
                        rectTopRight = new Rect(desiredWidth - handleSize, 0, handleSize, handleSize);
                        rectBottomLeft = new Rect(0, desiredHeight - handleSize, handleSize, handleSize);
                    }
                }
                else if (ResizeHandleRenderMode == RenderMode.Outside)
                {
                    if (SelectionRenderMode != RenderMode.None)
                    {
                        var offset = (handleSize + strokeThickness) / 2;
                        rectSelection = new Rect(-offset, -offset,
                            desiredWidth + offset * 2, desiredHeight + offset * 2);
                    }

                    if ((arrangeMode & ArrangeMode.ResizeHorizontal) != 0)
                    {
                        rectLeft = new Rect(-handleSize, (desiredHeight - handleSize) / 2, handleSize, handleSize);
                        rectRight = new Rect(desiredWidth, (desiredHeight - handleSize) / 2, handleSize, handleSize);
                    }

                    if ((arrangeMode & ArrangeMode.ResizeVertical) != 0)
                    {
                        rectTop = new Rect((desiredWidth - handleSize) / 2, -handleSize, handleSize, handleSize);
                        rectBottom = new Rect((desiredWidth - handleSize) / 2, desiredHeight, handleSize, handleSize);
                    }

                    if ((arrangeMode & ArrangeMode.ResizeNWSE) != 0)
                    {
                        rectTopLeft = new Rect(-handleSize, -handleSize, handleSize, handleSize);
                        rectBottomRight = new Rect(desiredWidth, desiredHeight, handleSize, handleSize);
                    }

                    if ((arrangeMode & ArrangeMode.ResizeNESW) != 0)
                    {
                        rectTopRight = new Rect(desiredWidth, -handleSize, handleSize, handleSize);
                        rectBottomLeft = new Rect(-handleSize, desiredHeight, handleSize, handleSize);
                    }
                }
            }

            mSelectionOverlay.Arrange(rectSelection);
            mHandles.Left.Arrange(rectLeft);
            mHandles.Right.Arrange(rectRight);
            mHandles.Top.Arrange(rectTop);
            mHandles.Bottom.Arrange(rectBottom);
            mHandles.TopLeft.Arrange(rectTopLeft);
            mHandles.TopRight.Arrange(rectTopRight);
            mHandles.BottomLeft.Arrange(rectBottomLeft);
            mHandles.BottomRight.Arrange(rectBottomRight);

            return finalSize;
        }

        protected override int VisualChildrenCount => mVisualChildren?.Count ?? 0;

        protected override Visual GetVisualChild(int index)
        {
            return mVisualChildren?[index];
        }

        #endregion

        #region Private

        #region Types

        /// <inheritdoc />
        /// <summary>
        ///     A custom <see cref="FrameworkElement" /> solely used for rendering the selection overlay.
        /// </summary>
        private class SelectionOverlay : FrameworkElement
        {
            private readonly ArrangeAdorner mParent;

            public SelectionOverlay(ArrangeAdorner parent)
            {
                IsHitTestVisible = false;
                mParent = parent;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                if (ActualWidth < double.Epsilon || ActualHeight < double.Epsilon) return;

                var scaleFactor =
                    1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

                var strokeThickness =
                    mParent.SelectionStroke != null ? mParent.SelectionStrokeThickness * scaleFactor : 0;

                var pen = strokeThickness > 0 ? new Pen(mParent.SelectionStroke, strokeThickness) : null;

                var rect = new Rect(strokeThickness / 2, strokeThickness / 2,
                    ActualWidth - strokeThickness, ActualHeight - strokeThickness);

                DrawingHelper.DrawRectangle(drawingContext, mParent.SelectionFill, pen, rect);
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     A <see cref="T:System.Windows.Controls.Primitives.Thumb" /> with custom rendering used as a resize handle.
        /// </summary>
        private class ResizeHandle : Thumb
        {
            private readonly ArrangeAdorner mParent;

            public ResizeHandle(ArrangeAdorner parent)
            {
                Template = null;
                mParent = parent;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                if (ActualWidth < double.Epsilon || ActualHeight < double.Epsilon) return;

                var scaleFactor =
                    1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

                var strokeThickness = mParent.HandleStroke != null ? mParent.HandleStrokeThickness * scaleFactor : 0;

                var pen = strokeThickness > 0 ? new Pen(mParent.HandleStroke, strokeThickness) : null;

                var rect = new Rect(strokeThickness / 2, strokeThickness / 2,
                    ActualWidth - strokeThickness, ActualHeight - strokeThickness);

                DrawingHelper.DrawRectangle(drawingContext, mParent.HandleFill, pen, rect);
            }
        }

        /// <summary>
        ///     A custom struct used as a container for resize handles.
        /// </summary>
        private struct ResizeHandles
        {
            public ResizeHandle Left;
            public ResizeHandle Right;
            public ResizeHandle Top;
            public ResizeHandle Bottom;
            public ResizeHandle TopLeft;
            public ResizeHandle TopRight;
            public ResizeHandle BottomLeft;
            public ResizeHandle BottomRight;
        }

        #endregion

        #region Fields

        /// <summary>
        ///     A visual collection that is used to draw the visuals on this adorner.
        /// </summary>
        private readonly VisualCollection mVisualChildren;

        /// <summary>
        ///     A custom UIElement used to render the overlay so that we can set hit test visibility set to true on the
        ///     adorner to allow resize handles to register events, while still passing events through the selection overlay.
        /// </summary>
        private readonly SelectionOverlay mSelectionOverlay;

        /// <summary>
        ///     A struct containing all the resize handles.
        /// </summary>
        private ResizeHandles mHandles;

        private RenderMode mSelectionRenderMode, mResizeHandleRenderMode;
        private Brush mSelectionFill, mSelectionStroke, mHandleFill, mHandleStroke;
        private double mSelectionStrokeThickness, mHandleStrokeThickness, mHandleSize;

        #endregion

        #region Helper Methods

        /// <summary>
        ///     Initializes resize handles for this <see cref="ArrangeAdorner" />.
        /// </summary>
        private void InitializeResizeHandles()
        {
            mHandles.Left = new ResizeHandle(this) {Cursor = Cursors.SizeWE};
            mHandles.Right = new ResizeHandle(this) {Cursor = Cursors.SizeWE};
            mHandles.Top = new ResizeHandle(this) {Cursor = Cursors.SizeNS};
            mHandles.Bottom = new ResizeHandle(this) {Cursor = Cursors.SizeNS};
            mHandles.TopLeft = new ResizeHandle(this) {Cursor = Cursors.SizeNWSE};
            mHandles.TopRight = new ResizeHandle(this) {Cursor = Cursors.SizeNESW};
            mHandles.BottomLeft = new ResizeHandle(this) {Cursor = Cursors.SizeNESW};
            mHandles.BottomRight = new ResizeHandle(this) {Cursor = Cursors.SizeNWSE};

            mHandles.Left.DragDelta += (sender, args) => Resize(Handle.Left, new Vector(args.HorizontalChange, 0));
            mHandles.Right.DragDelta += (sender, args) => Resize(Handle.Right, new Vector(args.HorizontalChange, 0));
            mHandles.Top.DragDelta += (sender, args) => Resize(Handle.Top, new Vector(0, args.VerticalChange));
            mHandles.Bottom.DragDelta += (sender, args) => Resize(Handle.Bottom, new Vector(0, args.VerticalChange));
            mHandles.TopLeft.DragDelta += (sender, args) =>
                Resize(Handle.TopLeft, new Vector(args.HorizontalChange, args.VerticalChange));
            mHandles.TopRight.DragDelta += (sender, args) =>
                Resize(Handle.TopRight, new Vector(args.HorizontalChange, args.VerticalChange));
            mHandles.BottomLeft.DragDelta += (sender, args) =>
                Resize(Handle.BottomLeft, new Vector(args.HorizontalChange, args.VerticalChange));
            mHandles.BottomRight.DragDelta += (sender, args) =>
                Resize(Handle.BottomRight, new Vector(args.HorizontalChange, args.VerticalChange));

            mVisualChildren.Add(mHandles.Left);
            mVisualChildren.Add(mHandles.Right);
            mVisualChildren.Add(mHandles.Top);
            mVisualChildren.Add(mHandles.Bottom);
            mVisualChildren.Add(mHandles.TopLeft);
            mVisualChildren.Add(mHandles.TopRight);
            mVisualChildren.Add(mHandles.BottomLeft);
            mVisualChildren.Add(mHandles.BottomRight);
        }

        /// <summary>
        ///     Resizes the <see cref="Adorner.AdornedElement" /> according to the specified handle and delta.
        /// </summary>
        /// <param name="handle">The <see cref="Handle" /> that is being dragged.</param>
        /// <param name="delta">The drag delta <see cref="Vector" />.</param>
        private void Resize(Handle handle, Vector delta)
        {
            if (!(AdornedElement is FrameworkElement element)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);
            var minValidSize = (ResizeHandleRenderMode == RenderMode.Inside ? 3 : 1) * HandleSize * scaleFactor;

            var minSize = new Size(minValidSize > element.MinWidth ? minValidSize : element.MinWidth,
                minValidSize > element.MinHeight ? minValidSize : element.MinHeight);

            var maxSize = new Size(element.MaxWidth, element.MaxHeight);

            ArrangeHelper.ResizeElement(element, handle, delta, minSize, maxSize);
        }

        #endregion

        #endregion
    }
}
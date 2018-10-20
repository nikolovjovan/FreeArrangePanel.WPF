using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FreeArrangePanel.Helpers;

namespace FreeArrangePanel.Adorners
{
    /// <summary>
    ///     Specifies visual element render mode.
    /// </summary>
    public enum RenderMode
    {
        None, // Does not render the visual element
        Inside, // Renders the visual element INSIDE AdornedElement's bounds
        Outside // Renders the visual element OUTSIDE AdornedElement's bounds
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
            ThumbFill = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            ThumbStroke = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
            ThumbStrokeThickness = 1.0;
            ThumbWidth = 10;
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
                mSelectionOverlay?.InvalidateVisual();
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
                mSelectionOverlay?.InvalidateVisual();
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
                mSelectionOverlay?.InvalidateVisual();
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
                mSelectionOverlay?.InvalidateVisual();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="Brush" /> that paints the interior area of the resize handle thumb.
        /// </summary>
        public Brush ThumbFill
        {
            get => mThumbFill;
            set
            {
                mThumbFill = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="Brush" /> that specifies how the resize handle thumb outline is painted.
        /// </summary>
        public Brush ThumbStroke
        {
            get => mThumbStroke;
            set
            {
                mThumbStroke = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="double" /> that specifies the width of the selection rectangle outline.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double ThumbStrokeThickness
        {
            get => mThumbStrokeThickness;
            set
            {
                mThumbStrokeThickness = value > 0 ? value : 0;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets a <see cref="double" /> that specifies the thumb width used for rendering resize handles.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double ThumbWidth
        {
            get => mThumbWidth;
            set
            {
                mThumbWidth = value > 0 ? value : 0;
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

            mSelectionOverlay.Arrange(new Rect(0, 0, desiredWidth, desiredHeight));

            if (ResizeHandleRenderMode == RenderMode.None)
            {
                mHandles.Left.Arrange(new Rect());
                mHandles.Right.Arrange(new Rect());
                mHandles.Top.Arrange(new Rect());
                mHandles.Bottom.Arrange(new Rect());
                mHandles.TopLeft.Arrange(new Rect());
                mHandles.TopRight.Arrange(new Rect());
                mHandles.BottomLeft.Arrange(new Rect());
                mHandles.BottomRight.Arrange(new Rect());
                return finalSize;
            }

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            if (ResizeHandleRenderMode == RenderMode.Inside)
            {
                mHandles.Left.Arrange(new Rect(0, thumbWidth, thumbWidth, desiredHeight - 2 * thumbWidth));
                mHandles.Right.Arrange(new Rect(desiredWidth - thumbWidth, thumbWidth, thumbWidth,
                    desiredHeight - 2 * thumbWidth));
                mHandles.Top.Arrange(new Rect(thumbWidth, 0, desiredWidth - 2 * thumbWidth, thumbWidth));
                mHandles.Bottom.Arrange(new Rect(thumbWidth, desiredHeight - thumbWidth, desiredWidth - 2 * thumbWidth,
                    thumbWidth));
                mHandles.TopLeft.Arrange(new Rect(0, 0, thumbWidth, thumbWidth));
                mHandles.TopRight.Arrange(new Rect(desiredWidth - thumbWidth, 0, thumbWidth, thumbWidth));
                mHandles.BottomLeft.Arrange(new Rect(0, desiredHeight - thumbWidth, thumbWidth, thumbWidth));
                mHandles.BottomRight.Arrange(new Rect(desiredWidth - thumbWidth, desiredHeight - thumbWidth, thumbWidth,
                    thumbWidth));
            }
            else
            {
                mHandles.Left.Arrange(new Rect(-thumbWidth, (desiredHeight - thumbWidth) / 2, thumbWidth, thumbWidth));
                mHandles.Right.Arrange(new Rect(desiredWidth, (desiredHeight - thumbWidth) / 2, thumbWidth,
                    thumbWidth));
                mHandles.Top.Arrange(new Rect((desiredWidth - thumbWidth) / 2, -thumbWidth, thumbWidth, thumbWidth));
                mHandles.Bottom.Arrange(
                    new Rect((desiredWidth - thumbWidth) / 2, desiredHeight, thumbWidth, thumbWidth));
                mHandles.TopLeft.Arrange(new Rect(-thumbWidth, -thumbWidth, thumbWidth, thumbWidth));
                mHandles.TopRight.Arrange(new Rect(desiredWidth, -thumbWidth, thumbWidth, thumbWidth));
                mHandles.BottomLeft.Arrange(new Rect(-thumbWidth, desiredHeight, thumbWidth, thumbWidth));
                mHandles.BottomRight.Arrange(new Rect(desiredWidth, desiredHeight, thumbWidth, thumbWidth));
            }

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
        ///     A custom <see cref="UIElement" /> solely used for rendering the selection overlay.
        /// </summary>
        private class SelectionOverlay : UIElement
        {
            private readonly ArrangeAdorner mParent;

            public SelectionOverlay(ArrangeAdorner parent)
            {
                IsHitTestVisible = false;
                mParent = parent;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                mParent.RenderSelectionOverlay(drawingContext);
            }
        }

        /// <summary>
        ///     A <see cref="Thumb" /> with custom rendering used as a resize handle.
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
                var scaleFactor =
                    1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

                if (ActualWidth < double.Epsilon || ActualHeight < double.Epsilon) return;

                var strokeThickness = mParent.ThumbStroke != null ? mParent.ThumbStrokeThickness * scaleFactor : 0;

                var pen = strokeThickness > 0 ? new Pen(mParent.ThumbStroke, strokeThickness) : null;

                var rect = new Rect(strokeThickness / 2, strokeThickness / 2,
                    ActualWidth - strokeThickness, ActualHeight - strokeThickness);

                DrawingHelper.DrawRectangle(drawingContext, mParent.ThumbFill, pen, rect);
            }
        }

        /// <summary>
        ///     A custom struct used as a container resize handle thumbs.
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

        private readonly VisualCollection mVisualChildren;
        private readonly SelectionOverlay mSelectionOverlay;
        private ResizeHandles mHandles;

        private RenderMode mSelectionRenderMode, mResizeHandleRenderMode;
        private Brush mSelectionFill, mSelectionStroke, mThumbFill, mThumbStroke;
        private double mSelectionStrokeThickness, mThumbStrokeThickness, mThumbWidth;

        #endregion

        #region Resize Handlers

        /// <summary>
        ///     Handles resizing from the left resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleLeft(object sender, DragDeltaEventArgs args)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            var drag = new Vector(args.HorizontalChange, 0);

            if (drag.X < 0) AdjustDragDelta(ref drag, RectEdge.Left);

            var newWidth = CalculateNewWidth(-drag.X, thumbWidth);

            Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) - (newWidth - adornedElement.Width));
            adornedElement.Width = newWidth;
        }

        /// <summary>
        ///     Handles resizing from the right resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleRight(object sender, DragDeltaEventArgs args)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            var drag = new Vector(args.HorizontalChange, 0);

            if (drag.X > 0) AdjustDragDelta(ref drag, RectEdge.Right);

            var newWidth = CalculateNewWidth(drag.X, thumbWidth);

            adornedElement.Width = newWidth;
        }

        /// <summary>
        ///     Handles resizing from the top resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleTop(object sender, DragDeltaEventArgs args)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            var drag = new Vector(0, args.VerticalChange);

            if (drag.Y < 0) AdjustDragDelta(ref drag, RectEdge.Top);

            var newHeight = CalculateNewHeight(-drag.Y, thumbWidth);

            Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) - (newHeight - adornedElement.Height));
            adornedElement.Height = newHeight;
        }

        /// <summary>
        ///     Handles resizing from the bottom resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleBottom(object sender, DragDeltaEventArgs args)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            var drag = new Vector(0, args.VerticalChange);

            if (drag.Y > 0) AdjustDragDelta(ref drag, RectEdge.Bottom);

            var newHeight = CalculateNewHeight(drag.Y, thumbWidth);

            adornedElement.Height = newHeight;
        }

        /// <summary>
        ///     Handles resizing from the top left resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleTopLeft(object sender, DragDeltaEventArgs args)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            var newWidth = CalculateNewWidth(-args.HorizontalChange, thumbWidth);
            var newHeight = CalculateNewHeight(-args.VerticalChange, thumbWidth);

            var drag = new Vector(adornedElement.Width - newWidth, adornedElement.Height - newHeight);

            if (drag.X > 0)
            {
                Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) - (newWidth - adornedElement.Width));
                adornedElement.Width = newWidth;
                drag.X = 0;
            }

            if (drag.Y > 0)
            {
                Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) - (newHeight - adornedElement.Height));
                adornedElement.Height = newHeight;
                drag.Y = 0;
            }

            if (drag.X < 0 || drag.Y < 0)
            {
                AdjustDragDelta(ref drag, (drag.X < 0 ? RectEdge.Left : 0) | (drag.Y < 0 ? RectEdge.Top : 0));

                if (drag.X < 0)
                {
                    newWidth = CalculateNewWidth(-drag.X, thumbWidth);
                    Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) - (newWidth - adornedElement.Width));
                    adornedElement.Width = newWidth;
                }

                if (drag.Y < 0)
                {
                    newHeight = CalculateNewHeight(-drag.Y, thumbWidth);
                    Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) - (newHeight - adornedElement.Height));
                    adornedElement.Height = newHeight;
                }
            }
        }

        /// <summary>
        ///     Handles resizing from the top right resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleTopRight(object sender, DragDeltaEventArgs args)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            var newWidth = CalculateNewWidth(args.HorizontalChange, thumbWidth);
            var newHeight = CalculateNewHeight(-args.VerticalChange, thumbWidth);

            var drag = new Vector(newWidth - adornedElement.Width, adornedElement.Height - newHeight);

            if (drag.X < 0)
            {
                adornedElement.Width = newWidth;
                drag.X = 0;
            }

            if (drag.Y > 0)
            {
                Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) - (newHeight - adornedElement.Height));
                adornedElement.Height = newHeight;
                drag.Y = 0;
            }

            if (drag.X > 0 || drag.Y < 0)
            {
                AdjustDragDelta(ref drag, (drag.X > 0 ? RectEdge.Right : 0) | (drag.Y < 0 ? RectEdge.Top : 0));

                if (drag.X > 0)
                {
                    newWidth = CalculateNewWidth(drag.X, thumbWidth);
                    adornedElement.Width = newWidth;
                }

                if (drag.Y < 0)
                {
                    newHeight = CalculateNewHeight(-drag.Y, thumbWidth);
                    Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) - (newHeight - adornedElement.Height));
                    adornedElement.Height = newHeight;
                }
            }
        }

        /// <summary>
        ///     Handles resizing from the bottom left resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            var newWidth = CalculateNewWidth(-args.HorizontalChange, thumbWidth);
            var newHeight = CalculateNewHeight(args.VerticalChange, thumbWidth);

            var drag = new Vector(adornedElement.Width - newWidth, newHeight - adornedElement.Height);

            if (drag.X > 0)
            {
                Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) - (newWidth - adornedElement.Width));
                adornedElement.Width = newWidth;
                drag.X = 0;
            }

            if (drag.Y < 0)
            {
                adornedElement.Height = newHeight;
                drag.Y = 0;
            }

            if (drag.X < 0 || drag.Y > 0)
            {
                AdjustDragDelta(ref drag, (drag.X < 0 ? RectEdge.Left : 0) | (drag.Y > 0 ? RectEdge.Bottom : 0));

                if (drag.X < 0)
                {
                    newWidth = CalculateNewWidth(-drag.X, thumbWidth);
                    Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) - (newWidth - adornedElement.Width));
                    adornedElement.Width = newWidth;
                }

                if (drag.Y > 0)
                {
                    newHeight = CalculateNewHeight(drag.Y, thumbWidth);
                    adornedElement.Height = newHeight;
                }
            }
        }

        /// <summary>
        ///     Handles resizing from the bottom right resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleBottomRight(object sender, DragDeltaEventArgs args)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var thumbWidth = ThumbWidth * scaleFactor;

            var newWidth = CalculateNewWidth(args.HorizontalChange, thumbWidth);
            var newHeight = CalculateNewHeight(args.VerticalChange, thumbWidth);

            var drag = new Vector(newWidth - adornedElement.Width, newHeight - adornedElement.Height);

            if (drag.X < 0)
            {
                adornedElement.Width = newWidth;
                drag.X = 0;
            }

            if (drag.Y < 0)
            {
                adornedElement.Height = newHeight;
                drag.Y = 0;
            }

            if (drag.X > 0 || drag.Y > 0)
            {
                AdjustDragDelta(ref drag, (drag.X > 0 ? RectEdge.Right : 0) | (drag.Y > 0 ? RectEdge.Bottom : 0));

                if (drag.X > 0)
                {
                    newWidth = CalculateNewWidth(drag.X, thumbWidth);
                    adornedElement.Width = newWidth;
                }

                if (drag.Y > 0)
                {
                    newHeight = CalculateNewHeight(drag.Y, thumbWidth);
                    adornedElement.Height = newHeight;
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        ///     Initializes resize handle thumb and adds it to the visual tree.
        /// </summary>
        /// <param name="resizeHandle">A <see cref="ResizeHandle" /> that needs to be initialized.</param>
        /// <param name="cursor">A <see cref="Cursor" /> that specifies the mouse cursor that will be shown on mouse hover.</param>
        /// <param name="visualCollection">A <see cref="VisualCollection" /> this thumb needs to be added to.</param>
        private void InitializeResizeHandle(ref ResizeHandle resizeHandle, Cursor cursor,
            VisualCollection visualCollection)
        {
            if (resizeHandle != null) return;
            resizeHandle = new ResizeHandle(this) {Cursor = cursor};
            visualCollection.Add(resizeHandle);
        }

        /// <summary>
        ///     Initializes resize handles for this <see cref="ArrangeAdorner" />.
        /// </summary>
        private void InitializeResizeHandles()
        {
            InitializeResizeHandle(ref mHandles.Left, Cursors.SizeWE, mVisualChildren);
            InitializeResizeHandle(ref mHandles.Right, Cursors.SizeWE, mVisualChildren);
            InitializeResizeHandle(ref mHandles.Top, Cursors.SizeNS, mVisualChildren);
            InitializeResizeHandle(ref mHandles.Bottom, Cursors.SizeNS, mVisualChildren);
            InitializeResizeHandle(ref mHandles.TopLeft, Cursors.SizeNWSE, mVisualChildren);
            InitializeResizeHandle(ref mHandles.TopRight, Cursors.SizeNESW, mVisualChildren);
            InitializeResizeHandle(ref mHandles.BottomLeft, Cursors.SizeNESW, mVisualChildren);
            InitializeResizeHandle(ref mHandles.BottomRight, Cursors.SizeNWSE, mVisualChildren);

            mHandles.Left.DragDelta += HandleLeft;
            mHandles.Right.DragDelta += HandleRight;
            mHandles.Top.DragDelta += HandleTop;
            mHandles.Bottom.DragDelta += HandleBottom;
            mHandles.TopLeft.DragDelta += HandleTopLeft;
            mHandles.TopRight.DragDelta += HandleTopRight;
            mHandles.BottomLeft.DragDelta += HandleBottomLeft;
            mHandles.BottomRight.DragDelta += HandleBottomRight;
        }

        /// <summary>
        ///     Renders the selection overlay on the <see cref="mSelectionOverlay" /> element.
        /// </summary>
        /// <param name="drawingContext">A <see cref="DrawingContext" /> object from the <see cref="mSelectionOverlay" /> element.</param>
        /// <remarks>
        ///     The reason we are using a custom UIElement to render the overlay is so we can have hit test visibility set to
        ///     true on the adorner to allow thumbs to register events, while still passing events through the selection overlay.
        /// </remarks>
        private void RenderSelectionOverlay(DrawingContext drawingContext)
        {
            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            if (SelectionRenderMode == RenderMode.None) return;

            var strokeThickness = ThumbStroke != null ? SelectionStrokeThickness * scaleFactor : 0;

            var pen = strokeThickness > 0 ? new Pen(SelectionStroke, strokeThickness) : null;

            Rect rect;
            if (SelectionRenderMode == RenderMode.Inside)
                rect = new Rect(strokeThickness / 2, strokeThickness / 2,
                    AdornedElement.RenderSize.Width - strokeThickness,
                    AdornedElement.RenderSize.Height - strokeThickness);
            else
                rect = new Rect(-strokeThickness / 2, -strokeThickness / 2,
                    AdornedElement.RenderSize.Width + strokeThickness,
                    AdornedElement.RenderSize.Height + strokeThickness);

            DrawingHelper.DrawRectangle(drawingContext, SelectionFill, pen, rect);
        }

        /// <summary>
        ///     Calls <see cref="OverlapHelper.AdjustDragDelta" /> with the specified params.
        /// </summary>
        /// <param name="drag">The drag <see cref="Vector" /> that needs adjusting.</param>
        /// <param name="edge">If moving <see cref="RectEdge.None" />, otherwise specifies a rect edge for resizing.</param>
        private void AdjustDragDelta(ref Vector drag, RectEdge edge)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;
            if (!(adornedElement.Parent is Controls.FreeArrangePanel panel)) return;
            panel.GenerateElementRects(out var selectedRects, out var staticRects);
            OverlapHelper.AdjustDragDelta(ref drag, edge, selectedRects, staticRects,
                panel.LimitMovementToPanel ? panel.RenderSize : Size.Empty);
        }

        /// <summary>
        ///     Calculates new width of the <see cref="Adorner.AdornedElement" />.
        /// </summary>
        /// <param name="change">A <see cref="double" /> specifying the increase/decrease in width.</param>
        /// <param name="thumbWidth">A <see cref="double" /> specifying the thumb width.</param>
        /// <returns>The new width of the <see cref="Adorner.AdornedElement" />.</returns>
        private double CalculateNewWidth(double change, double thumbWidth)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return 0;
            return Math.Max(adornedElement.Width + change, Math.Max(adornedElement.MinWidth,
                (ResizeHandleRenderMode == RenderMode.Inside ? 3 : 1) * thumbWidth));
        }

        /// <summary>
        ///     Calculates new height of the <see cref="Adorner.AdornedElement" />.
        /// </summary>
        /// <param name="change">A <see cref="double" /> specifying the increase/decrease in height.</param>
        /// <param name="thumbHeight">A <see cref="double" /> specifying the thumb height.</param>
        /// <returns>The new height of the <see cref="Adorner.AdornedElement" />.</returns>
        private double CalculateNewHeight(double change, double thumbHeight)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return 0;
            return Math.Max(adornedElement.Height + change, Math.Max(adornedElement.MinHeight,
                (ResizeHandleRenderMode == RenderMode.Inside ? 3 : 1) * thumbHeight));
        }

        #endregion

        #endregion
    }
}
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

            var handleSize = HandleSize * scaleFactor;

            if (ResizeHandleRenderMode == RenderMode.Inside)
            {
                mHandles.Left.Arrange(new Rect(0, handleSize, handleSize, desiredHeight - 2 * handleSize));
                mHandles.Right.Arrange(new Rect(desiredWidth - handleSize, handleSize, handleSize,
                    desiredHeight - 2 * handleSize));
                mHandles.Top.Arrange(new Rect(handleSize, 0, desiredWidth - 2 * handleSize, handleSize));
                mHandles.Bottom.Arrange(new Rect(handleSize, desiredHeight - handleSize, desiredWidth - 2 * handleSize,
                    handleSize));
                mHandles.TopLeft.Arrange(new Rect(0, 0, handleSize, handleSize));
                mHandles.TopRight.Arrange(new Rect(desiredWidth - handleSize, 0, handleSize, handleSize));
                mHandles.BottomLeft.Arrange(new Rect(0, desiredHeight - handleSize, handleSize, handleSize));
                mHandles.BottomRight.Arrange(new Rect(desiredWidth - handleSize, desiredHeight - handleSize, handleSize,
                    handleSize));
            }
            else
            {
                mHandles.Left.Arrange(new Rect(-handleSize, (desiredHeight - handleSize) / 2, handleSize, handleSize));
                mHandles.Right.Arrange(new Rect(desiredWidth, (desiredHeight - handleSize) / 2, handleSize,
                    handleSize));
                mHandles.Top.Arrange(new Rect((desiredWidth - handleSize) / 2, -handleSize, handleSize, handleSize));
                mHandles.Bottom.Arrange(
                    new Rect((desiredWidth - handleSize) / 2, desiredHeight, handleSize, handleSize));
                mHandles.TopLeft.Arrange(new Rect(-handleSize, -handleSize, handleSize, handleSize));
                mHandles.TopRight.Arrange(new Rect(desiredWidth, -handleSize, handleSize, handleSize));
                mHandles.BottomLeft.Arrange(new Rect(-handleSize, desiredHeight, handleSize, handleSize));
                mHandles.BottomRight.Arrange(new Rect(desiredWidth, desiredHeight, handleSize, handleSize));
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
            public SelectionOverlay(ArrangeAdorner parent)
            {
                IsHitTestVisible = false;
                mParent = parent;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                var scaleFactor =
                    1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

                if (mParent.SelectionRenderMode == RenderMode.None) return;

                var strokeThickness = mParent.HandleStroke != null ? mParent.SelectionStrokeThickness * scaleFactor : 0;

                var pen = strokeThickness > 0 ? new Pen(mParent.SelectionStroke, strokeThickness) : null;

                Rect rect;
                if (mParent.SelectionRenderMode == RenderMode.Inside)
                    rect = new Rect(strokeThickness / 2, strokeThickness / 2,
                        mParent.AdornedElement.RenderSize.Width - strokeThickness,
                        mParent.AdornedElement.RenderSize.Height - strokeThickness);
                else
                    rect = new Rect(-strokeThickness / 2, -strokeThickness / 2,
                        mParent.AdornedElement.RenderSize.Width + strokeThickness,
                        mParent.AdornedElement.RenderSize.Height + strokeThickness);

                DrawingHelper.DrawRectangle(drawingContext, mParent.SelectionFill, pen, rect);
            }

            private readonly ArrangeAdorner mParent;
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
                var scaleFactor =
                    1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

                if (ActualWidth < double.Epsilon || ActualHeight < double.Epsilon) return;

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

        private readonly VisualCollection mVisualChildren;
        /// <remarks>
        ///     The reason we are using a custom UIElement to render the overlay is so we can have hit test visibility set to true
        ///     on
        ///     the adorner to allow resize handles to register events, while still passing events through the selection overlay.
        /// </remarks>
        private readonly SelectionOverlay mSelectionOverlay;
        /// <remarks>
        ///     The reason we are using a custom UIElement to render the overlay is so we can have hit test visibility set to true
        ///     on
        ///     the adorner to allow resize handles to register events, while still passing events through the selection overlay.
        /// </remarks>
        private ResizeHandles mHandles;

        private RenderMode mSelectionRenderMode, mResizeHandleRenderMode;
        private Brush mSelectionFill, mSelectionStroke, mHandleFill, mHandleStroke;
        private double mSelectionStrokeThickness, mHandleStrokeThickness, mHandleSize;

        #endregion

        #region Resize Handlers

        /// <summary>
        ///     Handles resizing from the left resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleLeft(object sender, DragDeltaEventArgs args)
        {
            Resize(DraggedHandle.Left, new Vector(args.HorizontalChange, 0));
        }

        /// <summary>
        ///     Handles resizing from the right resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleRight(object sender, DragDeltaEventArgs args)
        {
            Resize(DraggedHandle.Right, new Vector(args.HorizontalChange, 0));
        }

        /// <summary>
        ///     Handles resizing from the top resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleTop(object sender, DragDeltaEventArgs args)
        {
            Resize(DraggedHandle.Top, new Vector(0, args.VerticalChange));
        }

        /// <summary>
        ///     Handles resizing from the bottom resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleBottom(object sender, DragDeltaEventArgs args)
        {
            Resize(DraggedHandle.Bottom, new Vector(0, args.VerticalChange));
        }

        /// <summary>
        ///     Handles resizing from the top left resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleTopLeft(object sender, DragDeltaEventArgs args)
        {
            Resize(DraggedHandle.Top | DraggedHandle.Left, new Vector(args.HorizontalChange, args.VerticalChange));
        }

        /// <summary>
        ///     Handles resizing from the top right resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleTopRight(object sender, DragDeltaEventArgs args)
        {
            Resize(DraggedHandle.Top | DraggedHandle.Right, new Vector(args.HorizontalChange, args.VerticalChange));
        }

        /// <summary>
        ///     Handles resizing from the bottom left resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
        {
            Resize(DraggedHandle.Bottom | DraggedHandle.Left, new Vector(args.HorizontalChange, args.VerticalChange));
        }

        /// <summary>
        ///     Handles resizing from the bottom right resize handle.
        /// </summary>
        /// <param name="sender">An <see cref="object" /> that has raised this event.</param>
        /// <param name="args">A <see cref="DragDeltaEventArgs" /> object that specifies event arguments.</param>
        private void HandleBottomRight(object sender, DragDeltaEventArgs args)
        {
            Resize(DraggedHandle.Bottom | DraggedHandle.Right, new Vector(args.HorizontalChange, args.VerticalChange));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        ///     Initializes resize handle and adds it to the visual tree.
        /// </summary>
        /// <param name="resizeHandle">A <see cref="ResizeHandle" /> that needs to be initialized.</param>
        /// <param name="cursor">A <see cref="Cursor" /> that specifies the mouse cursor that will be shown on mouse hover.</param>
        /// <param name="visualCollection">A <see cref="VisualCollection" /> this resize handle needs to be added to.</param>
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
        ///     Resizes the <see cref="Adorner.AdornedElement" /> according to the specified handle and drag.
        /// </summary>
        /// <param name="handle">A <see cref="DraggedHandle" /> that specifies resize side/corner.</param>
        /// <param name="drag">The drag <see cref="Vector" /> that specifies mouse drag of the specified resize handle.</param>
        private void Resize(DraggedHandle handle, Vector drag)
        {
            if (!(AdornedElement is FrameworkElement adornedElement)) return;
            if (!(adornedElement.Parent is Controls.FreeArrangePanel panel)) return;

            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);
            var minSize = (ResizeHandleRenderMode == RenderMode.Inside ? 3 : 1) * HandleSize * scaleFactor;

            var oldWidth = adornedElement.Width;
            var oldHeight = adornedElement.Height;

            var newWidth = handle.HasFlag(DraggedHandle.Left) || handle.HasFlag(DraggedHandle.Right)
                ? Math.Max(oldWidth + (handle.HasFlag(DraggedHandle.Left) ? -drag.X : drag.X),
                    Math.Max(adornedElement.MinWidth, minSize))
                : oldWidth;
            var newHeight = handle.HasFlag(DraggedHandle.Top) || handle.HasFlag(DraggedHandle.Bottom)
                ? Math.Max(oldHeight + (handle.HasFlag(DraggedHandle.Top) ? -drag.Y : drag.Y),
                    Math.Max(adornedElement.MinHeight, minSize))
                : oldHeight;

            var diff = new Vector(newWidth - oldWidth, newHeight - oldHeight);

            if (diff.X <= -OverlapHelper.Epsilon)
            {
                if (handle.HasFlag(DraggedHandle.Left))
                    Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) - diff.X);
                adornedElement.Width = newWidth;
            }

            if (diff.Y <= -OverlapHelper.Epsilon)
            {
                if (handle.HasFlag(DraggedHandle.Top))
                    Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) - diff.Y);
                adornedElement.Height = newHeight;
            }

            if (diff.X < OverlapHelper.Epsilon)
            {
                handle &= DraggedHandle.Top | DraggedHandle.Bottom;
                drag.X = 0;
            }

            if (diff.Y < OverlapHelper.Epsilon)
            {
                handle &= DraggedHandle.Left | DraggedHandle.Right;
                drag.Y = 0;
            }

            if (handle == DraggedHandle.None) return;

            panel.GenerateElementRects(out var selectedRects, out var staticRects);
            OverlapHelper.AdjustDragDelta(ref drag, handle, selectedRects, staticRects,
                panel.LimitMovementToPanel ? panel.RenderSize : Size.Empty);

            if (Math.Abs(drag.X) > OverlapHelper.Epsilon)
            {
                if (handle.HasFlag(DraggedHandle.Left))
                    Canvas.SetLeft(adornedElement, Canvas.GetLeft(adornedElement) + drag.X);
                adornedElement.Width += handle.HasFlag(DraggedHandle.Left) ? -drag.X : drag.X;
            }

            if (Math.Abs(drag.Y) > OverlapHelper.Epsilon)
            {
                if (handle.HasFlag(DraggedHandle.Top))
                    Canvas.SetTop(adornedElement, Canvas.GetTop(adornedElement) + drag.Y);
                adornedElement.Height += handle.HasFlag(DraggedHandle.Top) ? -drag.Y : drag.Y;
            }
        }

        #endregion

        #endregion
    }
}
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
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
            IsHitTestVisible = false;
            SelectionRenderMode = RenderMode.None;
            ResizeHandleRenderMode = RenderMode.None;
            SelectionFill = null;
            SelectionStroke = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF));
            SelectionStrokeThickness = 2.0;
            //ThumbFill = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF));
            //ThumbStroke = null;
            ThumbFill = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0xFF));
            ThumbStroke = Brushes.Black;
            ThumbStrokeThickness = 1.0;
            ThumbSize = 10;
            mVisualChildren = new VisualCollection(this);
            mHandles = new ResizeHandles(mVisualChildren);
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
        public double ThumbSize
        {
            get => mThumbSize;
            set
            {
                mThumbSize = value > 0 ? value : 0;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        #endregion

        #endregion

        #region Protected

        protected override void OnRender(DrawingContext drawingContext)
        {
            var scaleFactor =
                1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            if (SelectionRenderMode != RenderMode.None)
            {
                var strokeThickness = ThumbStroke != null ? SelectionStrokeThickness * scaleFactor : 0;

                var pen = strokeThickness > 0 ? new Pen(SelectionStroke, strokeThickness) : null;

                Rect rect;
                if (SelectionRenderMode != RenderMode.Inside)
                    rect = new Rect(strokeThickness / 2, strokeThickness / 2,
                        AdornedElement.RenderSize.Width - strokeThickness,
                        AdornedElement.RenderSize.Height - strokeThickness);
                else
                    rect = new Rect(-strokeThickness / 2, -strokeThickness / 2,
                        AdornedElement.RenderSize.Width + strokeThickness,
                        AdornedElement.RenderSize.Height + strokeThickness);

                DrawingHelper.DrawRectangle(drawingContext, SelectionFill, pen, rect);
            }

            if (ResizeHandleRenderMode != RenderMode.None)
            {
                var thumbSize = ThumbSize * scaleFactor;
                var strokeThickness = ThumbStroke != null ? ThumbStrokeThickness * scaleFactor : 0;

                var pen = strokeThickness > 0 ? new Pen(ThumbStroke, strokeThickness) : null;

                var rect = new Rect((AdornedElement.RenderSize.Width - thumbSize + strokeThickness) / 2,
                    (AdornedElement.RenderSize.Height - thumbSize + strokeThickness) / 2,
                    thumbSize - strokeThickness, thumbSize - strokeThickness);

                DrawingHelper.DrawRectangle(drawingContext, ThumbFill, pen, rect);
            }
        }

        #endregion

        #region Private

        #region Fields

        private readonly VisualCollection mVisualChildren;
        private ResizeHandles mHandles;

        private RenderMode mSelectionRenderMode, mResizeHandleRenderMode;
        private Brush mSelectionFill, mSelectionStroke, mThumbFill, mThumbStroke;
        private double mSelectionStrokeThickness, mThumbStrokeThickness, mThumbSize;

        #endregion

        #endregion
    }
}
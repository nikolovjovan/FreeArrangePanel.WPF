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
            ThumbFill = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF));
            ThumbStroke = null;
            ThumbStrokeThickness = 1.0;
            ThumbSize = 10;
            mVisualChildren = new VisualCollection(this);
            mHandles = new ResizeHandles(mVisualChildren);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets a value that specifies the <see cref="RenderMode" /> used for rendering selection rectangle.
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
        ///     Gets or sets a value that specifies the <see cref="RenderMode" /> used for rendering resize handles.
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
        ///     Gets or sets the <see cref="Brush" /> that paints the interior area of the selection rectangle.
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
        ///     Gets or sets the <see cref="Brush" /> that specifies how the selection rectangle outline is painted.
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
        ///     Gets or sets the width of the selection rectangle outline.
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
        ///     Gets or sets the <see cref="Brush" /> that paints the interior area of the resize handle thumb.
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
        ///     Gets or sets the <see cref="Brush" /> that specifies how the resize handle thumb outline is painted.
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
        ///     Gets or sets the width of the selection rectangle outline.
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
        ///     Gets or sets a value that specifies the thumb width used for rendering resize handles.
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
            var dpiFactor = 1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            if (SelectionRenderMode != RenderMode.None)
            {
                double strokeThickness = 0;
                if (SelectionStroke != null) strokeThickness = SelectionStrokeThickness * dpiFactor;

                Pen drawingPen = null;
                if (strokeThickness > 0) drawingPen = new Pen(SelectionStroke, strokeThickness);

                Rect drawingRect;
                if (SelectionRenderMode == RenderMode.Inside)
                    drawingRect = new Rect(strokeThickness / 2, strokeThickness / 2,
                        AdornedElement.RenderSize.Width - strokeThickness,
                        AdornedElement.RenderSize.Height - strokeThickness);
                else
                    drawingRect = new Rect(-strokeThickness / 2, -strokeThickness / 2,
                        AdornedElement.RenderSize.Width + strokeThickness,
                        AdornedElement.RenderSize.Height + strokeThickness);

                var correction = strokeThickness / 2;
                var guidelines = new GuidelineSet();

                guidelines.GuidelinesX.Add(drawingRect.Left + correction);
                guidelines.GuidelinesX.Add(drawingRect.Right + correction);
                guidelines.GuidelinesY.Add(drawingRect.Top + correction);
                guidelines.GuidelinesY.Add(drawingRect.Bottom + correction);

                drawingContext.PushGuidelineSet(guidelines);
                drawingContext.DrawRectangle(SelectionFill, drawingPen, drawingRect);
                drawingContext.Pop();
            }

            if (ResizeHandleRenderMode == RenderMode.Outside)
            {
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
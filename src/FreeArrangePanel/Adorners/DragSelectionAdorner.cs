using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace FreeArrangePanel.Adorners
{
    /// <inheritdoc />
    /// <summary>
    ///     Adorner used to render drag selection rectangle.
    /// </summary>
    public class DragSelectionAdorner : Adorner
    {
        #region Public

        #region Constructors

        public DragSelectionAdorner(UIElement adornedElement, Point? start = null, Point? end = null,
            Brush fill = null, Brush stroke = null, double strokeThickness = 1.0) : base(adornedElement)
        {
            StartPoint = start ?? new Point();
            EndPoint = end ?? new Point();
            Fill = fill ?? new SolidColorBrush(Color.FromArgb(0x40, 0x0, 0x0, 0xFF));
            Stroke = stroke ?? new SolidColorBrush(Color.FromRgb(0x0, 0x0, 0xFF));
            StrokeThickness = strokeThickness;
            IsHitTestVisible = false;
            Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the first point for the drag selection rectangle.
        /// </summary>
        public Point StartPoint
        {
            get => mStartPoint;
            set
            {
                mStartPoint = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets the second point for the drag selection rectangle.
        /// </summary>
        public Point EndPoint
        {
            get => mEndPoint;
            set
            {
                mEndPoint = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets the <see cref="Brush"/> that paints the interior area of the drag selection rectangle.
        /// </summary>
        public Brush Fill
        {
            get => mFill;
            set
            {
                mFill = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets the <see cref="Brush"/> that specifies how the drag selection rectangle outline is painted.
        /// </summary>
        public Brush Stroke
        {
            get => mStroke;
            set
            {
                mStroke = value;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        /// <summary>
        ///     Gets or sets the width of the drag selection rectangle outline.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double StrokeThickness
        {
            get => mStrokeThickness;
            set
            {
                mStrokeThickness = value > 0 ? value : 0;
                AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
            }
        }

        #endregion

        #endregion

        #region Protected

        protected override void OnRender(DrawingContext drawingContext)
        {
            var dpiFactor = 1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var rect = new Rect(StartPoint, EndPoint);
            var drawingPen = new Pen(Stroke, StrokeThickness * dpiFactor);

            var halfWidth = drawingPen.Thickness / 2;
            var guidelines = new GuidelineSet();

            guidelines.GuidelinesX.Add(rect.Left + halfWidth);
            guidelines.GuidelinesX.Add(rect.Right + halfWidth);
            guidelines.GuidelinesY.Add(rect.Top + halfWidth);
            guidelines.GuidelinesY.Add(rect.Bottom + halfWidth);

            drawingContext.PushGuidelineSet(guidelines);
            drawingContext.DrawRectangle(Fill, drawingPen, rect);
            drawingContext.Pop();
        }

        #endregion

        #region Private

        #region Fields

        private Point mStartPoint, mEndPoint;
        private Brush mFill, mStroke;
        private double mStrokeThickness;

        #endregion

        #endregion
    }
}
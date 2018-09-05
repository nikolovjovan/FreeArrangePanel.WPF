using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace FreeArrangePanel.Adorners
{
    internal class DragSelectionAdorner : Adorner
    {
        public DragSelectionAdorner(UIElement adornedElement, double borderWidth = 1.0,
            Color? color = null, Point? start = null, Point? end = null) : base(adornedElement)
        {
            BorderWidth = borderWidth;
            Color = color ?? Color.FromArgb(0x40, 0x00, 0x00, 0xFF);
            StartPoint = start ?? new Point();
            EndPoint = end ?? new Point();
            Visibility = Visibility.Collapsed;
        }

        public double BorderWidth { get; set; }
        public Color Color { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var dpiFactor = 1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);

            var rect = new Rect(StartPoint, EndPoint);
            var drawingBrush = new SolidColorBrush(Color);
            var drawingPen = new Pen(new SolidColorBrush(
                    Color.FromArgb(255, Color.R, Color.G, Color.B)), BorderWidth * dpiFactor);

            var halfWidth = drawingPen.Thickness / 2;
            var guidelines = new GuidelineSet();

            guidelines.GuidelinesX.Add(rect.Left + halfWidth);
            guidelines.GuidelinesX.Add(rect.Right + halfWidth);
            guidelines.GuidelinesY.Add(rect.Top + halfWidth);
            guidelines.GuidelinesY.Add(rect.Bottom + halfWidth);

            drawingContext.PushGuidelineSet(guidelines);
            drawingContext.DrawRectangle(drawingBrush, drawingPen, rect);
            drawingContext.Pop();
        }
    }
}
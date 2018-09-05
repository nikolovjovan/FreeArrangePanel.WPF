using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace FreeArrangePanel.Adorners
{
    internal class ArrangeAdorner : Adorner
    {
        public ArrangeAdorner(UIElement adornedElement, double thumbSize = 2.0,
            Color? color = null) : base(adornedElement)
        {
            ThumbSize = thumbSize;
            Color = color ?? Color.FromRgb(0x00, 0x00, 0xFF);
            Visibility = Visibility.Collapsed;
        }

        public double ThumbSize { get; set; }
        public Color Color { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var dpiFactor = 1 / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);
            var adaptedThumbSize = ThumbSize * dpiFactor;

            var rect = new Rect(-adaptedThumbSize / 2, -adaptedThumbSize / 2,
                AdornedElement.RenderSize.Width + adaptedThumbSize,
                AdornedElement.RenderSize.Height + adaptedThumbSize);
            var drawingPen = new Pen(new SolidColorBrush(Color), adaptedThumbSize);

            var halfWidth = drawingPen.Thickness / 2;
            var guidelines = new GuidelineSet();

            guidelines.GuidelinesX.Add(rect.Left + halfWidth);
            guidelines.GuidelinesX.Add(rect.Right + halfWidth);
            guidelines.GuidelinesY.Add(rect.Top + halfWidth);
            guidelines.GuidelinesY.Add(rect.Bottom + halfWidth);

            drawingContext.PushGuidelineSet(guidelines);
            drawingContext.DrawRectangle(null, drawingPen, rect);
            drawingContext.Pop();
        }
    }
}
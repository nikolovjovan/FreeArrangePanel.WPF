using System.Windows;
using System.Windows.Media;

namespace FreeArrangePanel.Helpers
{
    /// <summary>
    ///     A helper class used for pixel perfect drawing on adorner layer.
    /// </summary>
    internal static class DrawingHelper
    {
        /// <summary>
        ///     Draws a rectangle with the specified <see cref="Brush" /> and <see cref="Pen" />. The pen and the brush can be
        ///     <see langword="null" />.
        /// </summary>
        /// <param name="context">The drawing context with which to draw the rectangle.</param>
        /// <param name="brush">
        ///     The brush with which to fill the rectangle.  This is optional, and can be <see langword="null" />.
        ///     If the brush is <see langword="null" />, no fill is drawn.
        /// </param>
        /// <param name="pen">
        ///     The pen with which to stroke the rectangle.  This is optional, and can be <see langword="null" />. If
        ///     the pen is <see langword="null" />, no stroke is drawn.
        /// </param>
        /// <param name="rectangle">The rectangle to draw.</param>
        public static void DrawRectangle(DrawingContext context, Brush brush, Pen pen, Rect rectangle)
        {
            if (context == null) return;

            var guidelines = new GuidelineSet();

            var correction = pen?.Thickness / 2 ?? 0;

            guidelines.GuidelinesX.Add(rectangle.Left + correction);
            guidelines.GuidelinesX.Add(rectangle.Right + correction);
            guidelines.GuidelinesY.Add(rectangle.Top + correction);
            guidelines.GuidelinesY.Add(rectangle.Bottom + correction);

            context.PushGuidelineSet(guidelines);
            context.DrawRectangle(brush, pen, rectangle);
            context.Pop();
        }
    }
}
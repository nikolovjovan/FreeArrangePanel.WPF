using System;
using System.Collections.Generic;
using System.Windows;

namespace FreeArrangePanel.Helpers
{
    /// <summary>
    ///     Specifies the side of a rectangle.
    /// </summary>
    [Flags]
    internal enum RectEdge
    {
        None = 0x0,
        Left = 0x1,
        Right = 0x2,
        Top = 0x4,
        Bottom = 0x8
    }

    /// <summary>
    ///     A helper class used for fixing element overlap.
    /// </summary>
    internal static class OverlapHelper
    {
        public const double Epsilon = 0.01;

        /// <summary>
        ///     Adjusts the drag delta <see cref="Vector" /> to prevent overlap and movement outside the panel.
        /// </summary>
        /// <param name="drag">The drag <see cref="Vector" /> that needs adjusting.</param>
        /// <param name="edge">If moving <see cref="RectEdge.None" />, otherwise specifies a rect edge for resizing.</param>
        /// <param name="selectedRects">A <see cref="IList{Rect}" /> of selected elements rects.</param>
        /// <param name="staticRects">A <see cref="IList{Rect}" /> of non selected static elements rects.</param>
        /// <param name="bounds">If the elements need to be limited to panel a non empty <see cref="Size" />.</param>
        public static void AdjustDragDelta(ref Vector drag, RectEdge edge,
            IList<Rect> selectedRects, IList<Rect> staticRects, Size bounds)
        {
            if (selectedRects == null || selectedRects.Count == 0) return;

            var limit = new Vector(
                drag.X < 0 ? double.MinValue : double.MaxValue,
                drag.Y < 0 ? double.MinValue : double.MaxValue
            );

            foreach (var r1 in selectedRects)
            {
                // If bounds is not empty, we need to limit the rect r1 to panel bounds.

                if (!bounds.IsEmpty)
                {
                    if (limit.X < 0 && r1.Left < -limit.X) limit.X = -r1.Left;
                    if (limit.X > 0 && bounds.Width - r1.Right < limit.X) limit.X = bounds.Width - r1.Right;
                    if (limit.Y < 0 && r1.Top < -limit.Y) limit.Y = -r1.Top;
                    if (limit.Y > 0 && bounds.Height - r1.Bottom < limit.Y) limit.Y = bounds.Height - r1.Bottom;
                }

                // And then iteratively apply drag delta limits. 

                if (Math.Abs(drag.X) > Math.Abs(limit.X)) drag.X = limit.X;
                if (Math.Abs(drag.Y) > Math.Abs(limit.Y)) drag.Y = limit.Y;

                // If staticRects is null, we skip overlap checking.

                if (staticRects == null) continue;

                foreach (var r2 in staticRects)
                {
                    var broadRect = GetBroadRect(r1, drag, edge);

                    var intersection = Rect.Intersect(r2, broadRect);

                    // If the intersection is empty there was no intersection. On the other hand,
                    // if the rectangle is not empty, it could still be a false positive, i.e.
                    // since we are working with floating point numbers, bad rounding can occur.
                    // The most common case of this is when the two rects are touching.

                    if (intersection.IsEmpty || intersection.Width < Epsilon || intersection.Height < Epsilon) continue;

                    // Once we have passed the broad phase, we need to test with sweep AABB.
                    // So we get the edge where the two rects touch and adjust the limits.

                    var collisionEdge = GetCollisionEdge(r1, r2, drag);

                    // If no edge has been hit, then there was no collision.

                    if (collisionEdge == RectEdge.None) continue;

                    // Since the RectEdge enum is a flag enum, we can split up the sides and correct
                    // them individually. Therefore, this works for both corners and sides!

                    if ((collisionEdge & RectEdge.Left) != 0) limit.X = r2.Left - r1.Right;
                    if ((collisionEdge & RectEdge.Right) != 0) limit.X = r2.Right - r1.Left;
                    if ((collisionEdge & RectEdge.Top) != 0) limit.Y = r2.Top - r1.Bottom;
                    if ((collisionEdge & RectEdge.Bottom) != 0) limit.Y = r2.Bottom - r1.Top;

                    // Again, we iteratively apply drag delta limits.

                    if (Math.Abs(drag.X) > Math.Abs(limit.X)) drag.X = limit.X;
                    if (Math.Abs(drag.Y) > Math.Abs(limit.Y)) drag.Y = limit.Y;
                }
            }
        }

        /// <summary>
        ///     Creates the broad phase testing rect.
        /// </summary>
        /// <param name="rect">The original <see cref="Rect" /> before editing.</param>
        /// <param name="drag">The drag <see cref="Vector" /> that needs adjusting.</param>
        /// <param name="edge">If moving <see cref="RectEdge.None" />, otherwise specifies a rect edge for resizing.</param>
        /// <returns>The broad phase testing <see cref="Rect" />.</returns>
        public static Rect GetBroadRect(Rect rect, Vector drag, RectEdge edge)
        {
            if (edge == RectEdge.None)
            {
                if (drag.X < 0) rect.X += drag.X;
                if (drag.Y < 0) rect.Y += drag.Y;
                rect.Width += Math.Abs(drag.X);
                rect.Height += Math.Abs(drag.Y);
            }
            if ((edge & RectEdge.Left) != 0)
            {
                rect.X += drag.X;
                rect.Width -= drag.X;
            }
            if ((edge & RectEdge.Top) != 0)
            {
                rect.Y += drag.Y;
                rect.Height -= drag.Y;
            }
            if ((edge & RectEdge.Right) != 0) rect.Width += drag.X;
            if ((edge & RectEdge.Bottom) != 0) rect.Height += drag.Y;

            return rect;
        }

        /// <summary>
        ///     Calculates the collision edge/corner of the two rects with the specified drag.
        /// </summary>
        /// <param name="drag">The movement <see cref="Vector" /> of the first rect.</param>
        /// <param name="r1">The <see cref="Rect" /> that is being moved.</param>
        /// <param name="r2">The stationary <see cref="Rect" />.</param>
        /// <returns>The edge/corner of the rect r2 that the rect r1 first hits/touches.</returns>
        public static RectEdge GetCollisionEdge(Rect r1, Rect r2, Vector drag)
        {
            var entryPoint = new Point(
                drag.X < 0 ? r2.Right - r1.Left : r2.Left - r1.Right,
                drag.Y < 0 ? r2.Bottom - r1.Top : r2.Top - r1.Bottom
            );

            var exitPoint = new Point(
                drag.X < 0 ? r2.Left - r1.Right : r2.Right - r1.Left,
                drag.Y < 0 ? r2.Top - r1.Bottom : r2.Bottom - r1.Top
            );

            var axialEntryTime = new Vector(
                Math.Abs(drag.X) < Epsilon ? double.MinValue : entryPoint.X / drag.X,
                Math.Abs(drag.Y) < Epsilon ? double.MinValue : entryPoint.Y / drag.Y
            );

            var axialExitTime = new Vector(
                Math.Abs(drag.X) < Epsilon ? double.MaxValue : exitPoint.X / drag.X,
                Math.Abs(drag.Y) < Epsilon ? double.MaxValue : exitPoint.Y / drag.Y
            );

            var entryTime = Math.Max(axialEntryTime.X, axialEntryTime.Y);
            var exitTime = Math.Min(axialExitTime.X, axialExitTime.Y);

            if (entryTime > exitTime || entryTime < 0 || entryTime > 1)
                return RectEdge.None;

            if (Math.Abs(axialEntryTime.X - axialEntryTime.Y) < Epsilon)
                return (drag.X < 0 ? RectEdge.Right : RectEdge.Left) |
                       (drag.Y < 0 ? RectEdge.Bottom : RectEdge.Top);
            if (axialEntryTime.X > axialEntryTime.Y)
                return drag.X < 0 ? RectEdge.Right : RectEdge.Left;
            return drag.Y < 0 ? RectEdge.Bottom : RectEdge.Top;
        }
    }
}
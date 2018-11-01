using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FreeArrangePanel.Controls;

namespace FreeArrangePanel.Helpers
{
    /// <summary>
    ///     Specifies the resize handle that is being dragged or <see cref="None"/> if the element is being moved.
    /// </summary>
    [Flags]
    internal enum Handle
    {
        /// <summary>
        ///     The element is being moved.
        /// </summary>
        None = 0x0,
        /// <summary>
        ///     The element is being resized from the left.
        /// </summary>
        Left = 0x1,
        /// <summary>
        ///     The element is being resized from the right.
        /// </summary>
        Right = 0x2,
        /// <summary>
        ///     The element is being resized from the top.
        /// </summary>
        Top = 0x4,
        /// <summary>
        ///     The element is being resized from the bottom.
        /// </summary>
        Bottom = 0x8,
        /// <summary>
        ///     The element is being resized from the top left.
        /// </summary>
        TopLeft = Top | Left,
        /// <summary>
        ///     The element is being resized from the top right.
        /// </summary>
        TopRight = Top | Right,
        /// <summary>
        ///     The element is being resized from the bottom left.
        /// </summary>
        BottomLeft = Bottom | Left,
        /// <summary>
        ///     The element is being resized from the bottom right.
        /// </summary>
        BottomRight = Bottom | Right
    }

    /// <summary>
    ///     A helper class used for various arranging calculations.
    /// </summary>
    internal static class ArrangeHelper
    {
        #region Public

        /// <summary>
        ///     Moves the selected elements on the specified <see cref="Controls.FreeArrangePanel"/>.
        /// </summary>
        /// <param name="panel">The <see cref="Controls.FreeArrangePanel"/> whose selected elements should be moved.</param>
        /// <param name="delta">The drag delta <see cref="Vector" />.</param>
        public static void MoveSelectedElements(Controls.FreeArrangePanel panel, Vector delta)
        {
            if (panel == null) return;

            if (!(Math.Abs(delta.X) > Epsilon) && !(Math.Abs(delta.Y) > Epsilon)) return;

            panel.GenerateElementRects(out var selectedRects, out var staticRects);
            AdjustDragDelta(ref delta, Handle.None, selectedRects, staticRects,
                panel.LimitMovementToPanel ? panel.RenderSize : Size.Empty);

            if (!(Math.Abs(delta.X) > Epsilon) && !(Math.Abs(delta.Y) > Epsilon)) return;

            foreach (var element in panel.SelectedElements)
                if (Controls.FreeArrangePanel.GetArrangeMode(element).HasFlag(ArrangeMode.MoveOnly))
                {
                    Canvas.SetLeft(element, Canvas.GetLeft(element) + delta.X);
                    Canvas.SetTop(element, Canvas.GetTop(element) + delta.Y);
                }
        }

        /// <summary>
        ///     Resizes the selected element on the specified <see cref="Controls.FreeArrangePanel"/>.
        /// </summary>
        /// <param name="panel">The <see cref="Controls.FreeArrangePanel"/> whose selected elements should be moved.</param>
        /// <param name="handle">The <see cref="Handle" /> that is being dragged.</param>
        /// <param name="delta">The drag delta <see cref="Vector" />.</param>
        /// <param name="minSize">The minimum width and minimum height of the selected element.</param>
        public static void ResizeSelectedElement(Controls.FreeArrangePanel panel, Handle handle, Vector delta, double minSize)
        {
            if (panel == null) return;
            if (panel.SelectedElements.Count == 0) return;
            if (!(panel.SelectedElements[0] is FrameworkElement element)) return;

            var oldWidth = element.Width;
            var oldHeight = element.Height;

            var newWidth = handle.HasFlag(Handle.Left) || handle.HasFlag(Handle.Right)
                ? Math.Max(oldWidth + (handle.HasFlag(Handle.Left) ? -delta.X : delta.X),
                    Math.Max(element.MinWidth, minSize))
                : oldWidth;
            var newHeight = handle.HasFlag(Handle.Top) || handle.HasFlag(Handle.Bottom)
                ? Math.Max(oldHeight + (handle.HasFlag(Handle.Top) ? -delta.Y : delta.Y),
                    Math.Max(element.MinHeight, minSize))
                : oldHeight;

            var diff = new Vector(newWidth - oldWidth, newHeight - oldHeight);

            if (diff.X <= -Epsilon)
            {
                if (handle.HasFlag(Handle.Left))
                    Canvas.SetLeft(element, Canvas.GetLeft(element) - diff.X);
                element.Width = newWidth;
            }

            if (diff.Y <= -Epsilon)
            {
                if (handle.HasFlag(Handle.Top))
                    Canvas.SetTop(element, Canvas.GetTop(element) - diff.Y);
                element.Height = newHeight;
            }

            if (diff.X < Epsilon)
            {
                handle &= Handle.Top | Handle.Bottom;
                delta.X = 0;
            }

            if (diff.Y < Epsilon)
            {
                handle &= Handle.Left | Handle.Right;
                delta.Y = 0;
            }

            if (handle == Handle.None) return;

            panel.GenerateElementRects(out var selectedRects, out var staticRects);
            AdjustDragDelta(ref delta, handle, selectedRects, staticRects,
                panel.LimitMovementToPanel ? panel.RenderSize : Size.Empty);

            if (Math.Abs(delta.X) > Epsilon)
            {
                if (handle.HasFlag(Handle.Left))
                    Canvas.SetLeft(element, Canvas.GetLeft(element) + delta.X);
                element.Width += handle.HasFlag(Handle.Left) ? -delta.X : delta.X;
            }

            if (Math.Abs(delta.Y) > Epsilon)
            {
                if (handle.HasFlag(Handle.Top))
                    Canvas.SetTop(element, Canvas.GetTop(element) + delta.Y);
                element.Height += handle.HasFlag(Handle.Top) ? -delta.Y : delta.Y;
            }
        }

        #endregion

        #region Private

        /// <summary>
        ///     The smallest drag delta that is not ignored.
        /// </summary>
        private const double Epsilon = 0.01;

        /// <summary>
        ///     Adjusts the drag delta <see cref="Vector" /> to prevent overlap and movement outside the panel.
        /// </summary>
        /// <param name="drag">The drag <see cref="Vector" /> that needs adjusting.</param>
        /// <param name="handle">If moving <see cref="Handle.None" />, otherwise specifies a rect handle for resizing.</param>
        /// <param name="selectedRects">A <see cref="ICollection{Rect}" /> of selected elements rects.</param>
        /// <param name="staticRects">A <see cref="ICollection{Rect}" /> of non selected static elements rects.</param>
        /// <param name="bounds">If the elements need to be limited to panel a non empty <see cref="Size" />.</param>
        private static void AdjustDragDelta(ref Vector drag, Handle handle,
            ICollection<Rect> selectedRects, ICollection<Rect> staticRects, Size bounds)
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
                    var broadRect = GetBroadRect(r1, drag, handle);

                    var intersection = Rect.Intersect(r2, broadRect);

                    // If the intersection is empty there was no intersection. On the other hand,
                    // if the rectangle is not empty, it could still be a false positive, i.e.
                    // since we are working with floating point numbers, bad rounding can occur.
                    // The most common case of this is when the two rects are touching.

                    if (intersection.IsEmpty || intersection.Width < Epsilon || intersection.Height < Epsilon) continue;

                    // Once we have passed the broad phase, we need to test with sweep AABB.
                    // So we get the normal vector of the two rects and adjust the limits.

                    var normal = GetNormalVector(r1, r2, drag);

                    // If no edge has been hit, then there was no collision.

                    if (normal.Length <= 0) continue;

                    // Otherwise, use the normal vector to adjust the limits.

                    if (normal.X < 0) limit.X = r2.Left - r1.Right;
                    if (normal.X > 0) limit.X = r2.Right - r1.Left;
                    if (normal.Y < 0) limit.Y = r2.Top - r1.Bottom;
                    if (normal.Y > 0) limit.Y = r2.Bottom - r1.Top;

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
        /// <param name="handle">If moving <see cref="Handle.None" />, otherwise specifies a rect handle for resizing.</param>
        /// <returns>The broad phase testing <see cref="Rect" />.</returns>
        private static Rect GetBroadRect(Rect rect, Vector drag, Handle handle)
        {
            if (handle == Handle.None)
            {
                if (drag.X < 0) rect.X += drag.X;
                if (drag.Y < 0) rect.Y += drag.Y;
                rect.Width += Math.Abs(drag.X);
                rect.Height += Math.Abs(drag.Y);
            }
            if ((handle & Handle.Left) != 0)
            {
                rect.X += drag.X;
                rect.Width -= drag.X;
            }
            if ((handle & Handle.Top) != 0)
            {
                rect.Y += drag.Y;
                rect.Height -= drag.Y;
            }
            if ((handle & Handle.Right) != 0) rect.Width += drag.X;
            if ((handle & Handle.Bottom) != 0) rect.Height += drag.Y;

            return rect;
        }

        /// <summary>
        ///     Calculates the collision normal vector of the two rects with the specified drag.
        /// </summary>
        /// <param name="r1">The <see cref="Rect" /> that is being moved.</param>
        /// <param name="r2">The stationary <see cref="Rect" />.</param>
        /// <param name="drag">The movement <see cref="Vector" /> of the first rect.</param>
        /// <returns>The collision normal <see cref="Vector"/> of the rect r2 that the rect r1 first hits.</returns>
        private static Vector GetNormalVector(Rect r1, Rect r2, Vector drag)
        {
            var entryPoint = new Point(
                drag.X < 0 ? r2.Right - r1.Left : r2.Left - r1.Right,
                drag.Y < 0 ? r2.Bottom - r1.Top : r2.Top - r1.Bottom
            );

            var axialEntryTime = new Vector(
                Math.Abs(drag.X) < Epsilon ? double.MinValue : entryPoint.X / drag.X,
                Math.Abs(drag.Y) < Epsilon ? double.MinValue : entryPoint.Y / drag.Y
            );

            var entryTime = Math.Max(axialEntryTime.X, axialEntryTime.Y);

            var normal = new Vector();

            if (entryTime < 0 || entryTime > 1) return normal;

            var hitCorner = Math.Abs(axialEntryTime.X - axialEntryTime.Y) < Epsilon;

            if (hitCorner || axialEntryTime.X > axialEntryTime.Y) normal.X = drag.X < 0 ? 1 : -1;
            if (hitCorner || axialEntryTime.X < axialEntryTime.Y) normal.Y = drag.Y < 0 ? 1 : -1;

            return normal;
        }

        #endregion
    }
}
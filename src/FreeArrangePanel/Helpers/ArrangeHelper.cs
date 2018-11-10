using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FreeArrangePanel.Helpers
{
    /// <summary>
    ///     Specifies the resize handle that is being dragged or <see cref="None" /> if the element is being moved.
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
        ///     Moves the selected elements on the specified <see cref="Controls.FreeArrangePanel" />.
        /// </summary>
        /// <param name="panel">The <see cref="Controls.FreeArrangePanel" /> whose selected elements should be moved.</param>
        /// <param name="delta">The drag delta <see cref="Vector" />.</param>
        public static void MoveSelectedElements(Controls.FreeArrangePanel panel, Vector delta)
        {
            if (panel == null) return;
            if (panel.SelectedElements.Count == 0) return;

            if (Math.Abs(delta.X) < Epsilon && Math.Abs(delta.Y) < Epsilon) return;

            var move = delta;

            if (panel.LimitMovementToPanel || panel.PreventOverlap)
            {
                var selectedRects = new List<Rect>();
                foreach (var element in panel.SelectedElements)
                    selectedRects.Add(GetElementRect((FrameworkElement) element));

                IList<Rect> staticRects = null;
                if (panel.PreventOverlap)
                {
                    staticRects = new List<Rect>();
                    foreach (UIElement child in panel.Children)
                        if (!Controls.FreeArrangePanel.GetSelected(child) &&
                            !Controls.FreeArrangePanel.GetIsOverlappable(child) &&
                            child.Visibility == Visibility.Visible)
                            staticRects.Add(GetElementRect((FrameworkElement) child));
                }

                move = GetMoveVector(selectedRects, staticRects,
                    panel.LimitMovementToPanel ? panel.RenderSize : Size.Empty, delta);

                if (Math.Abs(move.X) <= Epsilon && Math.Abs(move.Y) <= Epsilon) return;
            }

            foreach (var element in panel.SelectedElements)
            {
                Canvas.SetLeft(element, Canvas.GetLeft(element) + move.X);
                Canvas.SetTop(element, Canvas.GetTop(element) + move.Y);
            }
        }

        /// <summary>
        ///     Resizes the element if it is a selected child of a <see cref="FreeArrangePanel" />.
        /// </summary>
        /// <param name="element">The <see cref="FrameworkElement" /> that should be resized.</param>
        /// <param name="handle">The <see cref="Handle" /> that is being dragged.</param>
        /// <param name="delta">The drag delta <see cref="Vector" />.</param>
        /// <param name="minSize">The minimal <see cref="Size" /> of the selected element.</param>
        /// <param name="maxSize">The maximal <see cref="Size" /> of the selected element.</param>
        public static void ResizeElement(FrameworkElement element, Handle handle, Vector delta, Size minSize,
            Size maxSize)
        {
            if (element?.Parent == null || !(element.Parent is Controls.FreeArrangePanel panel)) return;
            if (panel.SelectedElements.Count > 1) return;
            if (Controls.FreeArrangePanel.GetSelected(element) == false) return;

            if (Math.Abs(delta.X) <= Epsilon && Math.Abs(delta.Y) <= Epsilon) return;

            var stretch = Stretch.Fill;

            PropertyInfo stretchProperty = null;

            foreach (var property in element.GetType().GetProperties())
                if (property.PropertyType == typeof(Stretch))
                    stretchProperty = property;

            if (stretchProperty != null)
                stretch = (Stretch) stretchProperty.GetValue(element);

            if (stretch == Stretch.None) return;

            var rect = GetElementRect(element);

            if (stretch == Stretch.Uniform)
            {
                // Fix aspect ratio for minSize and maxSize
                var aspectRatio = rect.Height > 0 ? rect.Width / rect.Height : 1;
                if (minSize.Width / aspectRatio > minSize.Height) minSize.Height = minSize.Width / aspectRatio;
                else if (minSize.Height * aspectRatio > minSize.Width) minSize.Width = minSize.Height * aspectRatio;
                if (!double.IsInfinity(maxSize.Width) && !double.IsInfinity(maxSize.Height))
                {
                    if (maxSize.Width / aspectRatio < maxSize.Height) maxSize.Height = maxSize.Width / aspectRatio;
                    else if (maxSize.Height * aspectRatio < maxSize.Width) maxSize.Width = maxSize.Height * aspectRatio;
                }
            }

            IList<Rect> staticRects = null;
            if (panel.PreventOverlap)
            {
                staticRects = new List<Rect>();
                foreach (UIElement child in panel.Children)
                    if (!Controls.FreeArrangePanel.GetSelected(child) &&
                        !Controls.FreeArrangePanel.GetIsOverlappable(child) &&
                        child.Visibility == Visibility.Visible)
                        staticRects.Add(GetElementRect((FrameworkElement) child));
            }

            var resizedRect = GetResizedRect(rect, staticRects, minSize, maxSize,
                panel.LimitMovementToPanel ? panel.RenderSize : Size.Empty,
                handle, delta, stretch);

            if (resizedRect.IsEmpty || resizedRect == rect) return;

            Canvas.SetLeft(element, resizedRect.Left);
            Canvas.SetTop(element, resizedRect.Top);
            element.Width = resizedRect.Width;
            element.Height = resizedRect.Height;

            // For some stupid reason the adorner layer is not updating when Stretch == Stretch.UniformToFill but
            // only in certain instances and that would cause stupid behaviour... damn was this hard to track...
            // Maybe there is a better solution but for the life of me I could not find it...
            // Just in case, I will always explicitly call this independant of the Stretch property...
            AdornerLayer.GetAdornerLayer(element)?.Update();
        }

        #endregion

        #region Private

        /// <summary>
        ///     The smallest drag delta that is not ignored.
        /// </summary>
        private const double Epsilon = 0.01;

        /// <summary>
        ///     Returns the specified element's rect.
        /// </summary>
        /// <param name="element">The <see cref="FrameworkElement" /> whose rect should be returned.</param>
        /// <returns>The specified element's rect.</returns>
        private static Rect GetElementRect(FrameworkElement element)
        {
            return new Rect(Canvas.GetLeft(element), Canvas.GetTop(element),
                !double.IsNaN(element.Width) ? element.Width : element.ActualWidth,
                !double.IsNaN(element.Height) ? element.Height : element.ActualHeight);
        }

        /// <summary>
        ///     Returns the move <see cref="Vector" /> for the selected elements.
        /// </summary>
        /// <param name="selectedRects">A <see cref="ICollection{Rect}" /> of selected elements rects.</param>
        /// <param name="staticRects">A <see cref="ICollection{Rect}" /> of non selected static elements rects.</param>
        /// <param name="panelSize">If the elements need to be limited to panel a non empty <see cref="Size" />.</param>
        /// <param name="delta">The mouse drag <see cref="Vector" />.</param>
        private static Vector GetMoveVector(ICollection<Rect> selectedRects, ICollection<Rect> staticRects,
            Size panelSize, Vector delta)
        {
            if (selectedRects == null || selectedRects.Count == 0) return new Vector();

            var limit = new Vector(
                delta.X < 0 ? double.MinValue : double.MaxValue,
                delta.Y < 0 ? double.MinValue : double.MaxValue
            );

            foreach (var r1 in selectedRects)
            {
                // If panelSize is not empty, we need to limit the rect r1 to panel bounds.

                if (!panelSize.IsEmpty)
                {
                    if (limit.X < 0 && r1.Left < -limit.X) limit.X = -r1.Left;
                    if (limit.X > 0 && panelSize.Width - r1.Right < limit.X) limit.X = panelSize.Width - r1.Right;
                    if (limit.Y < 0 && r1.Top < -limit.Y) limit.Y = -r1.Top;
                    if (limit.Y > 0 && panelSize.Height - r1.Bottom < limit.Y) limit.Y = panelSize.Height - r1.Bottom;
                }

                // And then iteratively apply drag delta limits.

                if (Math.Abs(delta.X) > Math.Abs(limit.X)) delta.X = limit.X;
                if (Math.Abs(delta.Y) > Math.Abs(limit.Y)) delta.Y = limit.Y;

                // If staticRects is null, we skip overlap checking.

                if (staticRects == null) continue;

                foreach (var r2 in staticRects)
                {
                    var broadRect = new Rect(
                        r1.X + (delta.X < 0 ? delta.X : 0),
                        r1.Y + (delta.Y < 0 ? delta.Y : 0),
                        r1.Width + Math.Abs(delta.X),
                        r1.Height + Math.Abs(delta.Y)
                    );

                    var intersection = Rect.Intersect(r2, broadRect);

                    // If the intersection is empty there was no intersection. On the other hand,
                    // if the rectangle is not empty, it could still be a false positive, i.e.
                    // since we are working with floating point numbers, bad rounding can occur.
                    // The most common case of this is when the two rects are touching.

                    if (intersection.IsEmpty || intersection.Width < Epsilon || intersection.Height < Epsilon) continue;

                    // Once we have passed the broad phase, we need to test with sweep AABB.
                    // So we get the normal vector of the two rects and adjust the limits.

                    var normal = GetNormalVector(r1, r2, delta);

                    // If no edge has been hit, then there was no collision.

                    if (normal.Length <= 0) continue;

                    // Otherwise, use the normal vector to adjust the limits.

                    if (normal.X > 0) limit.X = r2.Right - r1.Left;
                    if (normal.X < 0) limit.X = r2.Left - r1.Right;
                    if (normal.Y > 0) limit.Y = r2.Bottom - r1.Top;
                    if (normal.Y < 0) limit.Y = r2.Top - r1.Bottom;

                    // Again, we iteratively apply drag delta limits.

                    if (Math.Abs(delta.X) > Math.Abs(limit.X)) delta.X = limit.X;
                    if (Math.Abs(delta.Y) > Math.Abs(limit.Y)) delta.Y = limit.Y;
                }
            }

            return delta;
        }

        /// <summary>
        ///     Calculates the collision normal vector of the two rects with the specified drag.
        /// </summary>
        /// <param name="r1">The <see cref="Rect" /> that is being moved.</param>
        /// <param name="r2">The stationary <see cref="Rect" />.</param>
        /// <param name="drag">The movement <see cref="Vector" /> of the first rect.</param>
        /// <returns>The collision normal <see cref="Vector" /> of the rect r2 that the rect r1 first hits.</returns>
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

            // I hate floating points from the bottom of my heart...
            if (entryTime < 0 - Epsilon || entryTime > 1 + Epsilon) return normal;

            var hitCorner = Math.Abs(axialEntryTime.X - axialEntryTime.Y) < Epsilon;

            if (hitCorner || axialEntryTime.X > axialEntryTime.Y) normal.X = drag.X < 0 ? 1 : -1;
            if (hitCorner || axialEntryTime.X < axialEntryTime.Y) normal.Y = drag.Y < 0 ? 1 : -1;

            return normal;
        }

        /// <summary>
        ///     Returns the resized <see cref="Rect" />.
        /// </summary>
        /// <param name="rect">The original <see cref="Rect" /> before editing.</param>
        /// <param name="staticRects">A <see cref="ICollection{Rect}" /> of non selected static elements rects.</param>
        /// <param name="minSize">The minimal <see cref="Size" /> of the selected element.</param>
        /// <param name="maxSize">The maximal <see cref="Size" /> of the selected element.</param>
        /// <param name="panelSize">
        ///     The <see cref="Size" /> of the parent panel if limited to panel, <see cref="Size.Empty" />
        ///     otherwise.
        /// </param>
        /// <param name="handle">The <see cref="Handle" /> that is being dragged.</param>
        /// <param name="delta">The mouse drag <see cref="Vector" />.</param>
        /// <param name="stretch">The stretch property of the specified element.</param>
        /// <returns>The resized <see cref="Rect" />.</returns>
        private static Rect GetResizedRect(Rect rect, ICollection<Rect> staticRects, Size minSize,
            Size maxSize, Size panelSize, Handle handle, Vector delta, Stretch stretch)
        {
            if (rect.IsEmpty || handle == Handle.None) return Rect.Empty;

            var scale = GetScale(rect.Size, minSize, maxSize, handle, delta, stretch);
            var scaledRect = GetScaledRect(rect, handle, scale);

            if (scale.X <= 1 && scale.Y <= 1) return scaledRect;

            var isUniform = stretch == Stretch.Uniform;

            if (!panelSize.IsEmpty)
            {
                // Adjust the scale vector for all four sides.

                if (scaledRect.Left < 0)
                    AdjustLeft(ref scale, 0, rect, handle, isUniform);
                if (scaledRect.Right > panelSize.Width)
                    AdjustRight(ref scale, panelSize.Width, rect, handle, isUniform);
                if (scaledRect.Top < 0)
                    AdjustTop(ref scale, 0, rect, handle, isUniform);
                if (scaledRect.Bottom > panelSize.Height)
                    AdjustBottom(ref scale, panelSize.Height, rect, handle, isUniform);

                // Update the scaledRect.

                scaledRect = GetScaledRect(rect, handle, scale);
            }

            if (staticRects == null) return scaledRect;

            foreach (var staticRect in staticRects)
            {
                var intersection = Rect.Intersect(staticRect, scaledRect);

                // If the intersection is empty there was no intersection. On the other hand,
                // if the rectangle is not empty, it could still be a false positive, i.e.
                // since we are working with floating point numbers, bad rounding can occur.
                // The most common case of this is when the two rects are touching.

                if (intersection.IsEmpty || intersection.Width < Epsilon || intersection.Height < Epsilon) continue;

                // Once we have passed the broad phase, we need to test with sweep AABB.
                // So we get the normal vector of the two rects and adjust the limits.

                var drag = GetDragVector(rect, staticRect, scaledRect, handle, isUniform);
                var movingRect = new Rect(
                    scaledRect.Width > rect.Width ? scaledRect.X - drag.X : rect.X,
                    scaledRect.Height > rect.Height ? scaledRect.Y - drag.Y : rect.Y,
                    scaledRect.Width > rect.Width ? scaledRect.Width : rect.Width,
                    scaledRect.Height > rect.Height ? scaledRect.Height : rect.Height
                );
                var normal = GetNormalVector(movingRect, staticRect, drag);

                // If no edge has been hit, then there was no collision.

                if (normal.Length < 1) continue;

                // Otherwise, use the normal vector to adjust the limits.

                if (normal.X > 0) AdjustLeft(ref scale, staticRect.Right, rect, handle, isUniform);
                if (normal.X < 0) AdjustRight(ref scale, staticRect.Left, rect, handle, isUniform);
                if (normal.Y > 0) AdjustTop(ref scale, staticRect.Bottom, rect, handle, isUniform);
                if (normal.Y < 0) AdjustBottom(ref scale, staticRect.Top, rect, handle, isUniform);

                // Again, update the scaledRect.

                scaledRect = GetScaledRect(rect, handle, scale);
            }

            return scaledRect;
        }

        private static Vector GetDragVector(Rect rect, Rect staticRect, Rect scaledRect, Handle handle, bool isUniform)
        {
            var drag = new Vector();

            if (isUniform && (handle & (handle - 1)) == 0)
            {
                if ((handle & (Handle.Left | Handle.Right)) != 0)
                {
                    if (rect.Top >= staticRect.Bottom) handle |= Handle.Top;
                    if (rect.Bottom <= staticRect.Top) handle |= Handle.Bottom;
                }

                if ((handle & (Handle.Top | Handle.Bottom)) != 0)
                {
                    if (rect.Left >= staticRect.Right) handle |= Handle.Left;
                    if (rect.Right <= staticRect.Left) handle |= Handle.Right;
                }
            }

            if (handle.HasFlag(Handle.Left)) drag.X = scaledRect.Left - rect.Left;
            if (handle.HasFlag(Handle.Right)) drag.X = scaledRect.Right - rect.Right;
            if (handle.HasFlag(Handle.Top)) drag.Y = scaledRect.Top - rect.Top;
            if (handle.HasFlag(Handle.Bottom)) drag.Y = scaledRect.Bottom - rect.Bottom;

            return drag;
        }

        private static Vector GetScale(Size rectSize, Size minSize, Size maxSize, Handle handle, Vector delta,
            Stretch stretch)
        {
            var newSize = rectSize;

            var newWidth = rectSize.Width + (handle.HasFlag(Handle.Left) ? -delta.X : delta.X);
            var newHeight = rectSize.Height + (handle.HasFlag(Handle.Top) ? -delta.Y : delta.Y);
            if (newWidth < minSize.Width) newWidth = minSize.Width;
            if (newWidth > maxSize.Width) newWidth = maxSize.Width;
            if (newHeight < minSize.Height) newHeight = minSize.Height;
            if (newHeight > maxSize.Height) newHeight = maxSize.Height;

            newSize.Width = newWidth;
            newSize.Height = newHeight;

            var scale = new Vector(
                rectSize.Width > 0 ? newSize.Width / rectSize.Width : double.PositiveInfinity,
                rectSize.Height > 0 ? newSize.Height / rectSize.Height : double.PositiveInfinity
            );

            if (stretch == Stretch.Uniform)
            {
                var resizeHorizontal = (handle & (Handle.Left | Handle.Right)) != 0;
                var resizeVertical = (handle & (Handle.Top | Handle.Bottom)) != 0;

                if (resizeHorizontal && resizeVertical)
                {
                    if (scale.X < scale.Y) scale.Y = scale.X;
                    else scale.X = scale.Y;
                }
                else if (resizeHorizontal)
                {
                    scale.Y = scale.X;
                }
                else if (resizeVertical)
                {
                    scale.X = scale.Y;
                }
            }

            return scale;
        }

        private static Rect GetScaledRect(Rect rect, Handle handle, Vector scale)
        {
            var location = rect.Location;
            var size = new Size(rect.Width * scale.X, rect.Height * scale.Y);

            var resizeHorizontal = (handle & (Handle.Left | Handle.Right)) != 0;
            var resizeVertical = (handle & (Handle.Top | Handle.Bottom)) != 0;

            if (resizeHorizontal && !resizeVertical) location.Y -= (size.Height - rect.Height) / 2;
            else if (resizeVertical && !resizeHorizontal) location.X -= (size.Width - rect.Width) / 2;

            if (handle.HasFlag(Handle.Left)) location.X -= size.Width - rect.Width;
            if (handle.HasFlag(Handle.Top)) location.Y -= size.Height - rect.Height;

            return new Rect(location, size);
        }

        private static void AdjustLeft(ref Vector scale, double limit, Rect rect, Handle handle, bool isUniform)
        {
            scale.X = !isUniform || (handle & (handle - 1)) != 0 || handle == Handle.Left
                ? (rect.Right - limit) / rect.Width
                : (rect.Left + rect.Right - 2 * limit) / rect.Width;
            if (isUniform) scale.X = scale.Y = scale.X < scale.Y ? scale.X : scale.Y;
        }

        private static void AdjustRight(ref Vector scale, double limit, Rect rect, Handle handle, bool isUniform)
        {
            scale.X = !isUniform || (handle & (handle - 1)) != 0 || handle == Handle.Right
                ? (limit - rect.Left) / rect.Width
                : (2 * limit - (rect.Left + rect.Right)) / rect.Width;
            if (isUniform) scale.X = scale.Y = scale.X < scale.Y ? scale.X : scale.Y;
        }

        private static void AdjustTop(ref Vector scale, double limit, Rect rect, Handle handle, bool isUniform)
        {
            scale.Y = !isUniform || (handle & (handle - 1)) != 0 || handle == Handle.Top
                ? (rect.Bottom - limit) / rect.Height
                : (rect.Top + rect.Bottom - 2 * limit) / rect.Height;
            if (isUniform) scale.X = scale.Y = scale.X < scale.Y ? scale.X : scale.Y;
        }

        private static void AdjustBottom(ref Vector scale, double limit, Rect rect, Handle handle, bool isUniform)
        {
            scale.Y = !isUniform || (handle & (handle - 1)) != 0 || handle == Handle.Bottom
                ? (limit - rect.Top) / rect.Height
                : (2 * limit - (rect.Top + rect.Bottom)) / rect.Height;
            if (isUniform) scale.X = scale.Y = scale.X < scale.Y ? scale.X : scale.Y;
        }

        #endregion
    }
}
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
    public enum Handle
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
    ///     A stretchable <see cref="Rect" /> used for resizing elements.
    /// </summary>
    internal class StretchRect
    {
        private readonly Rect mOriginal;
        private Rect mStretched;

        public StretchRect(Rect rect, Size min, Size max, Handle handle, bool uniform, Vector delta)
        {
            mOriginal = rect;
            mStretched = new Rect(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, rect.Width, rect.Height);

            Handle = handle;
            Uniform = uniform;

            if (handle.HasFlag(Handle.Left)) mStretched.X = rect.Right;
            if (handle.HasFlag(Handle.Right)) mStretched.X = rect.Left;
            if (handle.HasFlag(Handle.Top)) mStretched.Y = rect.Bottom;
            if (handle.HasFlag(Handle.Bottom)) mStretched.Y = rect.Top;

            var width = rect.Width + (handle.HasFlag(Handle.Left) ? -delta.X : delta.X);
            var height = rect.Height + (handle.HasFlag(Handle.Top) ? -delta.Y : delta.Y);

            if (width < min.Width) width = min.Width;
            if (width > max.Width) width = max.Width;

            if (height < min.Height) height = min.Height;
            if (height > max.Height) height = max.Height;

            if (uniform)
            {
                var scale = new Vector(
                    rect.Width > 0 ? width / rect.Width : double.PositiveInfinity,
                    rect.Height > 0 ? height / rect.Height : double.PositiveInfinity
                );

                var resizeH = (handle & (Handle.Left | Handle.Right)) != 0;
                var resizeV = (handle & (Handle.Top | Handle.Bottom)) != 0;

                if (resizeH && (!resizeV || scale.X < scale.Y)) height = rect.Height * scale.X;
                if (resizeV && (!resizeH || scale.Y < scale.X)) width = rect.Width * scale.Y;
            }

            mStretched.Width = width;
            mStretched.Height = height;
        }

        /// <summary>
        ///     The Width component of <see cref="StretchRect" />. Depending on the <see cref="Uniform" /> property, it will be
        ///     automatically updated when <see cref="Height" /> is changed. Cannot be negative.
        /// </summary>
        public double Width
        {
            get => mStretched.Width;
            set
            {
                if (value < 0) throw new ArgumentException();
                mStretched.Width = value;
                if (Uniform && mOriginal.Width > 0)
                    mStretched.Height = mOriginal.Height * value / mOriginal.Width;
            }
        }

        /// <summary>
        ///     The Height component of <see cref="StretchRect" />. Depending on the <see cref="Uniform" /> property, it will be
        ///     automatically updated when <see cref="Width" /> is changed. Cannot be negative.
        /// </summary>
        public double Height
        {
            get => mStretched.Height;
            set
            {
                if (value < 0) throw new ArgumentException();
                mStretched.Height = value;
                if (Uniform && mOriginal.Height > 0)
                    mStretched.Width = mOriginal.Width * value / mOriginal.Height;
            }
        }

        /// <summary>
        ///     The Left side of <see cref="StretchRect" />. Depends on the <see cref="Handle" /> property.
        /// </summary>
        public double Left
        {
            get
            {
                if (Handle.HasFlag(Handle.Left)) return mStretched.X - mStretched.Width;
                if (Handle.HasFlag(Handle.Right)) return mStretched.X;
                return mStretched.X - mStretched.Width / 2;
            }
            set
            {
                if (Handle.HasFlag(Handle.Left)) Width = Right - value;
                else if (Handle.HasFlag(Handle.Right)) mStretched.X = value;
                else Width = 2 * (mStretched.X - value);
            }
        }

        /// <summary>
        ///     The Top side of <see cref="StretchRect" />. Depends on the <see cref="Handle" /> property.
        /// </summary>
        public double Top
        {
            get
            {
                if (Handle.HasFlag(Handle.Top)) return mStretched.Y - mStretched.Height;
                if (Handle.HasFlag(Handle.Bottom)) return mStretched.Y;
                return mStretched.Y - mStretched.Height / 2;
            }
            set
            {
                if (Handle.HasFlag(Handle.Top)) Height = Bottom - value;
                else if (Handle.HasFlag(Handle.Bottom)) mStretched.Y = value;
                else Height = 2 * (mStretched.Y - value);
            }
        }

        /// <summary>
        ///     The Right side of <see cref="StretchRect" />. Depends on the <see cref="Handle" /> property.
        /// </summary>
        public double Right
        {
            get
            {
                if (Handle.HasFlag(Handle.Left)) return mStretched.X;
                if (Handle.HasFlag(Handle.Right)) return mStretched.X + mStretched.Width;
                return mStretched.X + mStretched.Width / 2;
            }
            set
            {
                if (Handle.HasFlag(Handle.Left)) mStretched.X = value;
                else if (Handle.HasFlag(Handle.Right)) Width = value - Left;
                else Width = 2 * (value - mStretched.X);
            }
        }

        /// <summary>
        ///     The Bottom side of <see cref="StretchRect" />. Depends on the <see cref="Handle" /> property.
        /// </summary>
        public double Bottom
        {
            get
            {
                if (Handle.HasFlag(Handle.Top)) return mStretched.Y;
                if (Handle.HasFlag(Handle.Bottom)) return mStretched.Y + mStretched.Height;
                return mStretched.Y + mStretched.Height / 2;
            }
            set
            {
                if (Handle.HasFlag(Handle.Top)) mStretched.Y = value;
                else if (Handle.HasFlag(Handle.Bottom)) Height = value - Top;
                else Height = 2 * (value - mStretched.Y);
            }
        }

        /// <summary>
        ///     The <see cref="System.Windows.Rect" /> that corresponds to this <see cref="StretchRect" />.
        /// </summary>
        public Rect Rect => new Rect(Left, Top, Width, Height);

        /// <summary>
        ///     The <see cref="Helpers.Handle" /> that is being dragged to strech this rect.
        /// </summary>
        public Handle Handle { get; }

        /// <summary>
        ///     Specifies whether this rect should be streched uniformly or not.
        /// </summary>
        public bool Uniform { get; }

        /// <summary>
        ///     Specifies whether this rect is empty or not.
        /// </summary>
        public bool IsEmpty => mStretched.Width < ArrangeHelper.Epsilon || mStretched.Height < ArrangeHelper.Epsilon;

        /// <summary>
        ///     Returns True if the specified <see cref="System.Windows.Rect" /> intersects with this <see cref="StretchRect" />.
        ///     Returns false otherwise. Note that if one edge is coincident, this is NOT considered an intersection.
        /// </summary>
        /// <param name="rect">A <see cref="System.Windows.Rect" /> that is being checked for intersection.</param>
        /// <returns>
        ///     Returns True if the specified <see cref="System.Windows.Rect" /> intersects with this
        ///     <see cref="StretchRect" />. Returns false otherwise.
        /// </returns>
        public bool IntersectsWith(Rect rect)
        {
            if (IsEmpty || rect.IsEmpty) return false;

            return rect.Left <= Right - ArrangeHelper.Epsilon &&
                   rect.Right >= Left + ArrangeHelper.Epsilon &&
                   rect.Top <= Bottom - ArrangeHelper.Epsilon &&
                   rect.Bottom >= Top + ArrangeHelper.Epsilon;
        }
    }

    /// <summary>
    ///     A helper class used for various arranging calculations.
    /// </summary>
    public static class ArrangeHelper
    {
        #region Public

        /// <summary>
        ///     The smallest double value that is not ignored.
        /// </summary>
        public const double Epsilon = 0.01;

        /// <summary>
        ///     Moves the selected elements on the specified <see cref="Controls.FreeArrangePanel" />.
        /// </summary>
        /// <param name="panel">The <see cref="Controls.FreeArrangePanel" /> whose selected elements should be moved.</param>
        /// <param name="drag">The drag delta <see cref="Vector" />.</param>
        public static void MoveSelectedElements(Controls.FreeArrangePanel panel, Vector drag)
        {
            if (panel == null || panel.SelectedElements.Count == 0) return;

            if (Math.Abs(drag.X) < Epsilon && Math.Abs(drag.Y) < Epsilon) return;

            var move = drag;

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
                    panel.LimitMovementToPanel ? panel.RenderSize : Size.Empty, drag);

                if (Math.Abs(move.X) < Epsilon && Math.Abs(move.Y) < Epsilon) return;
            }

            foreach (var element in panel.SelectedElements)
            {
                Canvas.SetLeft(element, Canvas.GetLeft(element) + move.X);
                Canvas.SetTop(element, Canvas.GetTop(element) + move.Y);
            }
        }

        /// <summary>
        ///     Resizes the element if it is a selected child of a <see cref="FreeArrangePanel" /> element.
        /// </summary>
        /// <param name="element">The <see cref="FrameworkElement" /> that should be resized.</param>
        /// <param name="handle">The <see cref="Handle" /> that is being dragged.</param>
        /// <param name="drag">The drag delta <see cref="Vector" />.</param>
        /// <param name="min">The minimal <see cref="Size" /> of the selected element.</param>
        /// <param name="max">The maximal <see cref="Size" /> of the selected element.</param>
        public static void ResizeElement(FrameworkElement element, Handle handle, Vector drag, Size min, Size max)
        {
            if (element?.Parent == null || !(element.Parent is Controls.FreeArrangePanel panel)) return;
            if (panel.SelectedElements.Count != 1 || !Controls.FreeArrangePanel.GetSelected(element)) return;

            if (Math.Abs(drag.X) < Epsilon && Math.Abs(drag.Y) < Epsilon) return;

            var stretch = Stretch.Fill;

            PropertyInfo stretchProperty = null;

            foreach (var property in element.GetType().GetProperties())
                if (property.PropertyType == typeof(Stretch))
                    stretchProperty = property;

            if (stretchProperty != null) stretch = (Stretch) stretchProperty.GetValue(element);

            if (stretch == Stretch.None) return;

            var rect = GetElementRect(element);

            if (stretch == Stretch.Uniform)
            {
                // Fix aspect ratio for min and max
                var aspectRatio = rect.Height > 0 ? rect.Width / rect.Height : 1;
                if (min.Width / aspectRatio > min.Height) min.Height = min.Width / aspectRatio;
                else if (min.Height * aspectRatio > min.Width) min.Width = min.Height * aspectRatio;
                if (!double.IsInfinity(max.Width) && !double.IsInfinity(max.Height))
                {
                    if (max.Width / aspectRatio < max.Height) max.Height = max.Width / aspectRatio;
                    else if (max.Height * aspectRatio < max.Width) max.Width = max.Height * aspectRatio;
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

            var resizedRect = GetResizedRect(rect, staticRects, min, max,
                panel.LimitMovementToPanel ? panel.RenderSize : Size.Empty, handle, drag, stretch);

            if (resizedRect.IsEmpty || resizedRect == rect) return;

            Canvas.SetLeft(element, resizedRect.Left);
            Canvas.SetTop(element, resizedRect.Top);
            element.Width = resizedRect.Width;
            element.Height = resizedRect.Height;

            // For some stupid reason the adorner layer is not updating when Stretch == Stretch.UniformToFill but
            // only in certain instances and that would cause stupid behaviour. Damn was this hard to track down.
            // Maybe there is a better solution but for the life of me I could not find it. Just in case, I will
            // always explicitly call this independant of the Stretch property...
            AdornerLayer.GetAdornerLayer(element)?.Update();
        }

        #endregion

        #region Private

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

                    // If the intersection is empty there was no intersection. On the other hand, if the rectangle is not
                    // empty, it could still be a false positive, i.e. since we are working with floating point numbers,
                    // bad rounding can occur. The most common case of this is when the two rects are touching.

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
        ///     Returns the resized <see cref="Rect" />.
        /// </summary>
        /// <param name="rect">The original <see cref="Rect" /> before editing.</param>
        /// <param name="staticRects">A <see cref="ICollection{Rect}" /> of non selected static elements rects.</param>
        /// <param name="min">The minimal <see cref="Size" /> of the selected element.</param>
        /// <param name="max">The maximal <see cref="Size" /> of the selected element.</param>
        /// <param name="panelSize">
        ///     The <see cref="Size" /> of the parent panel if limited to panel, <see cref="Size.Empty" />
        ///     otherwise.
        /// </param>
        /// <param name="handle">The <see cref="Handle" /> that is being dragged.</param>
        /// <param name="drag">The mouse drag <see cref="Vector" />.</param>
        /// <param name="stretch">The stretch property of the specified element.</param>
        /// <returns>The resized <see cref="Rect" />.</returns>
        private static Rect GetResizedRect(Rect rect, ICollection<Rect> staticRects, Size min,
            Size max, Size panelSize, Handle handle, Vector drag, Stretch stretch)
        {
            if (rect.IsEmpty || handle == Handle.None) return Rect.Empty;

            var stretchRect = new StretchRect(rect, min, max, handle, stretch == Stretch.Uniform, drag);

            if (stretchRect.Width <= rect.Width && stretchRect.Height <= rect.Height) return stretchRect.Rect;

            // If panelSize is not empty, we need to adjust the size of the stretchRect accordingly.

            if (!panelSize.IsEmpty)
            {
                if (stretchRect.Left < 0) stretchRect.Left = 0;
                if (stretchRect.Right > panelSize.Width) stretchRect.Right = panelSize.Width;
                if (stretchRect.Top < 0) stretchRect.Top = 0;
                if (stretchRect.Bottom > panelSize.Height) stretchRect.Bottom = panelSize.Height;
            }

            if (staticRects == null) return stretchRect.Rect;

            foreach (var staticRect in staticRects)
            {
                if (!stretchRect.IntersectsWith(staticRect)) continue;

                // Since the current stretchRect intersects with staticRect, we need to find "which side it hits first".
                // To do that, we will treat this like a move operation but with the biggest possible rect and use the
                // previously described GetNormalVector() function to fix it. We reset the drag vector since we will
                // use it for swept AABB collision detection.

                drag = new Vector();

                // If the rect is being stretched uniformly and the sides are being dragged (not corners), there are now
                // three sides of the strechRect that can collide with their surroundings (the one being dragged, the one
                // east of it and the one west of it). Therefore, I have come up with a way to test collision even in this
                // case. The key is that only two sides can touch the staticRect at any given point (draw it and see).
                // For ex. we are dragging the LEFT handle. That means we need to check for the LEFT side but also TOP and
                // BOTTOM side! TOP and BOTTOM side CANNOT hit the staticRect AT THE SAME TIME! Therefore, we can drag the
                // movingRect (see below) either to LEFT ONLY, DIAGONALLY to BOTTOM LEFT or DIAGONALLY to TOP LEFT.
                // (For further explaination please read the code bellow and draw the rects on a piece of paper...)

                if (stretchRect.Uniform && (handle & (handle - 1)) == 0)
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

                // Now depending on the handle, calculate the drag vector.

                if (handle.HasFlag(Handle.Left)) drag.X = stretchRect.Left - rect.Left;
                if (handle.HasFlag(Handle.Right)) drag.X = stretchRect.Right - rect.Right;
                if (handle.HasFlag(Handle.Top)) drag.Y = stretchRect.Top - rect.Top;
                if (handle.HasFlag(Handle.Bottom)) drag.Y = stretchRect.Bottom - rect.Bottom;

                // Reset the handle in case we have modified it above...

                handle = stretchRect.Handle;

                // Using the drag vector, calculate the movingRect. It will be the biggest rect that will move on the path
                // of the drag vector (the path mouse makes when dragging the resize handle adjusted for collisions).

                var movingRect = new Rect(
                    stretchRect.Width > rect.Width ? stretchRect.Left - drag.X : rect.Left,
                    stretchRect.Height > rect.Height ? stretchRect.Top - drag.Y : rect.Top,
                    stretchRect.Width > rect.Width ? stretchRect.Width : rect.Width,
                    stretchRect.Height > rect.Height ? stretchRect.Height : rect.Height
                );

                // Now, using the calculated drag vector and movingRect, we get the normal vector of the collision.

                var normal = GetNormalVector(movingRect, staticRect, drag);

                // If no edge has been hit, then there was no collision.

                if (normal.Length < 1) continue;

                // Otherwise, use the normal vector to adjust stretchRect.

                if (normal.X > 0) stretchRect.Left = staticRect.Right;
                if (normal.X < 0) stretchRect.Right = staticRect.Left;
                if (normal.Y > 0) stretchRect.Top = staticRect.Bottom;
                if (normal.Y < 0) stretchRect.Bottom = staticRect.Top;
            }

            return stretchRect.Rect;
        }

        #endregion
    }
}
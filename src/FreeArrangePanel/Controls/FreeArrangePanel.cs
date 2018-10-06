using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FreeArrangePanel.Adorners;
using FreeArrangePanel.Helpers;

namespace FreeArrangePanel.Controls
{
    public class FreeArrangePanel : Canvas
    {
        #region Public

        #region Constructors

        public FreeArrangePanel()
        {
            mDragSelectionAdorner = new DragSelectionAdorner(this);
            Background = Brushes.Transparent; // Mouse hit testing does not work when Background is null!
            DragThreshold = 5.0; // Set default drag threshold
            SelectionThreshold = 0.5; // Set default selection threshold
            SelectedElements = new List<UIElement>();
            Focusable = true; // Allow the panel to get keyboard focus
            Loaded += OnPanelLoaded; // Add drag selection adorner on load
            Unloaded += OnPanelUnloaded; // Remove drag selection adorner on unload
        }

        static FreeArrangePanel()
        {
            IsOverlappableProperty = DependencyProperty.RegisterAttached("IsOverlappable", typeof(bool),
                typeof(FreeArrangePanel), new PropertyMetadata(false));
            SelectedProperty = DependencyProperty.RegisterAttached("Selected", typeof(bool),
                typeof(FreeArrangePanel), new PropertyMetadata(false));
            RenderSelectionProperty = DependencyProperty.RegisterAttached("RenderSelection", typeof(bool),
                typeof(FreeArrangePanel), new PropertyMetadata(false));
            ArrangeAdornerProperty = DependencyProperty.RegisterAttached("ArrangeAdorner", typeof(ArrangeAdorner),
                typeof(FreeArrangePanel), new PropertyMetadata(null));
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets a <see cref="T:System.Windows.Media.Brush" /> that is used to fill the area between the borders of a
        ///     <see cref="FreeArrangePanel" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Windows.Media.Brush" />. This default value is <see langword="transparent" />.
        /// </returns>
        public new Brush Background
        {
            get => base.Background;
            set
            {
                if (value == null) value = Brushes.Transparent;
                base.Background = value;
            }
        }

        /// <summary>
        ///     Distance the mouse cursor needs to drag a selection or an element in order to start a drag.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Double" />. This default value is <see langword="5.0" />.
        /// </returns>
        public double DragThreshold
        {
            get => mDragThreshold;
            set
            {
                if (value < 0.5) value = 0.5;
                mDragThreshold = value;
            }
        }

        /// <summary>
        ///     Percentage of the control that the drag selection rectangle needs to be over in order to select the element.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Double" />. This default value is <see langword="0.5" />.
        /// </returns>
        public double SelectionThreshold
        {
            get => mSelectionThreshold;
            set
            {
                if (value < 0.0) value = 0.0;
                else if (value > 1.0) value = 1.0;
                mSelectionThreshold = value;
            }
        }

        /// <summary>
        ///     Specifies whether to forward mouse events to the underlying controls.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Boolean" />. This default value is <see langword="false" />.
        /// </returns>
        public bool ForwardMouseEvents { get; set; } = false;

        /// <summary>
        ///     Specifies whether to limit control movement to the panel bounds.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Boolean" />. This default value is <see langword="true" />.
        /// </returns>
        public bool LimitMovementToPanel { get; set; } = true;

        /// <summary>
        ///     Specifies whether to prevent control overlap on controls with <see cref="IsOverlappableProperty" /> property set to
        ///     false.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Boolean" />. This default value is <see langword="true" />.
        /// </returns>
        public bool PreventOverlap { get; set; } = true;

        /// <summary>
        ///     List of elements currently selected.
        /// </summary>
        public IList<UIElement> SelectedElements { get; }

        /// <summary>
        ///     Specifies whether this child is overlappable or not.
        /// </summary>
        public static readonly DependencyProperty IsOverlappableProperty;

        public static void SetIsOverlappable(DependencyObject element, bool value)
        {
            if (element == null) throw new ArgumentException(nameof(element));
            element.SetValue(IsOverlappableProperty, value);
        }

        public static bool GetIsOverlappable(DependencyObject element)
        {
            if (element == null) throw new ArgumentException(nameof(element));
            return (bool) element.GetValue(IsOverlappableProperty);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Selects all children.
        /// </summary>
        public void SelectAll()
        {
            if (SelectedElements.Count == Children.Count) return;
            foreach (UIElement child in Children)
                SetSelected(child, true);
        }

        /// <summary>
        ///     Deselects all children.
        /// </summary>
        public void DeselectAll()
        {
            if (SelectedElements.Count == 0) return;
            foreach (var selectedElement in SelectedElements)
                SetSelected(selectedElement, false, false);
            SelectedElements.Clear();
        }

        #endregion

        #endregion

        #region Protected

        #region Load Handlers

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualRemoved != null)
            {
                var element = (UIElement) visualRemoved;
                SetArrangeAdorner(element, null);
                UpdateZOrder(element, true);
            }

            if (visualAdded != null)
            {
                var element = (FrameworkElement) visualAdded;
                element.Loaded += OnElementLoaded; // Add ArrangeAdorner once element AdornerLayer is loaded
                SetZIndex(element, Children.Count - 1); // Initialize ZIndex of the element
            }
        }

        protected static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement) sender;
            element.Loaded -= OnElementLoaded;
            SetArrangeAdorner(element, new ArrangeAdorner(element));
        }

        protected static void OnPanelLoaded(object sender, RoutedEventArgs e)
        {
            var panel = (FreeArrangePanel) sender;
            panel.Loaded -= OnPanelLoaded;
            AdornerLayer.GetAdornerLayer(panel).Add(panel.mDragSelectionAdorner);
        }

        protected static void OnPanelUnloaded(object sender, RoutedEventArgs e)
        {
            var panel = (FreeArrangePanel) sender;
            panel.Unloaded -= OnPanelUnloaded;
            AdornerLayer.GetAdornerLayer(panel).Remove(panel.mDragSelectionAdorner);
        }

        #endregion

        #region Selection and Drag Logic

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            mMouseLeftDown = true;
            mControlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

            if (!ReferenceEquals(e.Source, this))
            {
                var element = (UIElement) e.Source;

                mMouseDownPosition = e.GetPosition(element);

                if (!mControlDown && !GetSelected(element))
                {
                    DeselectAll();
                    SetSelected(element, true);
                }

                element.CaptureMouse();

                if (!ForwardMouseEvents) e.Handled = true;
                return;
            }

            mDragSelectionAdorner.StartPoint = mDragSelectionAdorner.EndPoint = e.GetPosition(this);

            if (!mControlDown) DeselectAll();

            CaptureMouse();
            Focus();

            e.Handled = true;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            mMouseLeftDown = false;

            if (!ReferenceEquals(e.Source, this))
            {
                var element = (UIElement) e.Source;

                if (!mMovingElements)
                {
                    if (mControlDown)
                    {
                        SetSelected(element, !GetSelected(element));
                    }
                    else if (!(SelectedElements.Count == 1 && GetSelected(element)))
                    {
                        DeselectAll();
                        SetSelected(element, true);
                    }
                }

                mMovingElements = false;
                foreach (var selectedElement in SelectedElements) SetRenderSelection(selectedElement, true);

                element.ReleaseMouseCapture();

                if (!ForwardMouseEvents) e.Handled = true;

                return;
            }

            if (mDragSelecting)
            {
                SetDragSelecting(false);
                foreach (UIElement child in Children)
                    SetSelected(child, GetRenderSelection(child));
            }

            mMovingElements = false;

            ReleaseMouseCapture();

            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var source = (UIElement) e.Source;
            var cursorPosition = e.GetPosition(source);

            if (ReferenceEquals(source, this))
            {
                if (mDragSelecting)
                {
                    mDragSelectionAdorner.EndPoint = cursorPosition;
                    AdornerLayer.GetAdornerLayer(this).Update();

                    mControlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                    var dragRect = new Rect(mDragSelectionAdorner.StartPoint, mDragSelectionAdorner.EndPoint);
                    foreach (UIElement child in Children)
                    {
                        var childRect = new Rect(new Point(GetLeft(child), GetTop(child)), child.RenderSize);
                        var intersection = Rect.Intersect(dragRect, childRect);
                        var percentage = intersection.IsEmpty
                            ? 0.0
                            : intersection.Width * intersection.Height / (childRect.Width * childRect.Height);
                        var value = mControlDown && GetSelected(child);
                        if (percentage >= SelectionThreshold) SetRenderSelection(child, !value);
                        else SetRenderSelection(child, value);
                    }
                }
                else if (mMouseLeftDown)
                {
                    var dragDelta = (cursorPosition - mDragSelectionAdorner.StartPoint).Length;
                    if (dragDelta > DragThreshold)
                    {
                        SetDragSelecting(true);
                        mDragSelectionAdorner.EndPoint = cursorPosition;
                        AdornerLayer.GetAdornerLayer(this).Update();
                    }
                }
            }
            else
            {
                if (mMovingElements)
                {
                    var dragDelta = GetDragDelta(cursorPosition);
                    if (dragDelta.X < 0 || dragDelta.X > 0 || dragDelta.Y < 0 || dragDelta.Y > 0)
                        foreach (var element in SelectedElements)
                        {
                            SetLeft(element, GetLeft(element) + dragDelta.X);
                            SetTop(element, GetTop(element) + dragDelta.Y);
                        }
                }
                else if (mMouseLeftDown && GetSelected(source))
                {
                    var dragDelta = (cursorPosition - mMouseDownPosition).Length;
                    if (dragDelta > DragThreshold)
                    {
                        mMovingElements = true;
                        foreach (var selectedElement in SelectedElements) SetRenderSelection(selectedElement, false);
                    }
                }
            }

            e.Handled = true;
        }

        #endregion

        #region Keyboard Shortcuts

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            var movement = new Point(0, 0);

            switch (e.Key)
            {
                case Key.A:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) SelectAll();
                    break;
                case Key.NumPad8:
                    movement = new Point(0, -1);
                    break;
                case Key.NumPad2:
                    movement = new Point(0, 1);
                    break;
                case Key.NumPad4:
                    movement = new Point(-1, 0);
                    break;
                case Key.NumPad6:
                    movement = new Point(1, 0);
                    break;
                case Key.NumPad7:
                    movement = new Point(-1, -1);
                    break;
                case Key.NumPad9:
                    movement = new Point(1, -1);
                    break;
                case Key.NumPad1:
                    movement = new Point(-1, 1);
                    break;
                case Key.NumPad3:
                    movement = new Point(1, 1);
                    break;
                case Key.Up: goto case Key.NumPad8;
                case Key.Down: goto case Key.NumPad2;
                case Key.Left: goto case Key.NumPad4;
                case Key.Right: goto case Key.NumPad6;
            }

            if (movement != new Point(0, 0))
                foreach (var element in SelectedElements)
                {
                    SetLeft(element, GetLeft(element) + movement.X);
                    SetTop(element, GetTop(element) + movement.Y);
                }

            e.Handled = true;
        }

        #endregion

        #endregion

        #region Private

        #region Fields 

        /// <summary>
        ///     Used to specify whether a control is selected or not.
        /// </summary>
        private static readonly DependencyProperty SelectedProperty;

        /// <summary>
        ///     Used to specify whether the selection is being rendered or not.
        /// </summary>
        private static readonly DependencyProperty RenderSelectionProperty;

        /// <summary>
        ///     Stores a reference to the ArrangeAdorner of an element.
        /// </summary>
        private static readonly DependencyProperty ArrangeAdornerProperty;

        /// <summary>
        ///     Stores a reference to the DragSelectionAdorner of this panel.
        /// </summary>
        private readonly DragSelectionAdorner mDragSelectionAdorner;

        /// <summary>
        ///     Specifies the distance the mouse needs to travel to start dragging.
        /// </summary>
        private double mDragThreshold = 5.0;

        /// <summary>
        ///     Specifies the percentage of the control that the drag rectangle needs to be over in order to select it.
        /// </summary>
        private double mSelectionThreshold = 0.5;

        /// <summary>
        ///     Stores the position of the mouse on MouseDown event.
        /// </summary>
        private Point mMouseDownPosition;

        /// <summary>
        ///     Specifies whether the left mouse button is down.
        /// </summary>
        private bool mMouseLeftDown;

        /// <summary>
        ///     Specifies whether the control button is down.
        /// </summary>
        private bool mControlDown;

        /// <summary>
        ///     Specifies whether the user is drag selecting.
        /// </summary>
        private bool mDragSelecting;

        /// <summary>
        ///     Specifies whether the selected elements are being moved.
        /// </summary>
        private bool mMovingElements;

        #endregion

        #region Dependency Properties Methods

        /// <summary>
        ///     Sets the arrange adorner for the specified element.
        /// </summary>
        /// <param name="element">Child element of this panel.</param>
        /// <param name="value">ArrangeAdorner object. If null, removes it from the element.</param>
        private static void SetArrangeAdorner(UIElement element, ArrangeAdorner value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var oldValue = (ArrangeAdorner) element.GetValue(ArrangeAdornerProperty);
            if (ReferenceEquals(value, oldValue)) return;
            element.SetValue(ArrangeAdornerProperty, value);
            if (value != null) AdornerLayer.GetAdornerLayer(element).Add(value);
            else AdornerLayer.GetAdornerLayer(element).Remove(oldValue);
        }

        private static ArrangeAdorner GetArrangeAdorner(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (ArrangeAdorner) element.GetValue(ArrangeAdornerProperty);
        }

        /// <summary>
        ///     Selects or deselects the specified element.
        /// </summary>
        /// <param name="element">Child element of this panel.</param>
        /// <param name="value">True to select, False to deselect.</param>
        /// <param name="modifyList">Specifies whether to modify the <see cref="SelectedElements"/> list.</param>
        private void SetSelected(UIElement element, bool value, bool modifyList = true)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (value == (bool) element.GetValue(SelectedProperty)) return;
            element.SetValue(SelectedProperty, value);
            SetRenderSelection(element, value);
            UpdateZOrder(element);
            if (!modifyList) return;
            if (value) SelectedElements.Add(element);
            else SelectedElements.Remove(element);
        }

        private static bool GetSelected(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (bool) element.GetValue(SelectedProperty);
        }

        /// <summary>
        ///     Sets the ArrangeAdorner visibility.
        /// </summary>
        /// <param name="element">Child element of this panel.</param>
        /// <param name="value">True for Visible, False for Collapsed.</param>
        private static void SetRenderSelection(UIElement element, bool value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(RenderSelectionProperty, value);
            GetArrangeAdorner(element).Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool GetRenderSelection(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (bool) element.GetValue(RenderSelectionProperty);
        }

        #endregion

        #region Helper Methods

        private void SetDragSelecting(bool value)
        {
            mDragSelecting = value;
            mDragSelectionAdorner.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateZOrder(UIElement element, bool remove = false)
        {
            var elementZIndex = GetZIndex(element);

            if (remove || GetSelected(element))
            {
                // Decrement the ZIndices of elements with higher ZIndex than this.
                foreach (UIElement child in Children)
                {
                    var childZIndex = GetZIndex(child);
                    if (childZIndex > elementZIndex) SetZIndex(child, childZIndex - 1);
                }

                // If the element is being removed, we are done.
                if (remove) return;
                // Else the element is being selected, so we need to push it to the top.
                SetZIndex(element, Children.Count - 1);
                return;
            }

            // Since the element is being deselected, we need to push it down below all
            // selected elements but still above all deselected elements.
            // So, we increment the ZIndices of elements with lower ZIndex than this,
            // and then set the ZIndex of this element to the lowest ZIndex of the
            // selected elements.

            var newZIndex = int.MaxValue;

            foreach (var selectedElement in SelectedElements)
            {
                var childZIndex = GetZIndex(selectedElement);
                if (childZIndex < elementZIndex) SetZIndex(selectedElement, childZIndex + 1);
                if (childZIndex < newZIndex) newZIndex = childZIndex;
            }

            SetZIndex(element, newZIndex);
        }

        private Vector GetDragDelta(Point cursorPosition)
        {
            var dragDelta = cursorPosition - mMouseDownPosition;

            var limit = new DeltaLimit();

            foreach (var selectedElement in SelectedElements)
            {
                var rect = new Rect(GetLeft(selectedElement), GetTop(selectedElement),
                    selectedElement.RenderSize.Width, selectedElement.RenderSize.Height);

                if (LimitMovementToPanel)
                {
                    if (rect.Left < limit.Left) limit.Left = rect.Left;
                    if (ActualWidth - rect.Right < limit.Right) limit.Right = ActualWidth - rect.Right;
                    if (rect.Top < limit.Top) limit.Top = rect.Top;
                    if (ActualHeight - rect.Bottom < limit.Bottom) limit.Bottom = ActualHeight - rect.Bottom;
                }

                if (!PreventOverlap) continue;

                foreach (UIElement child in Children)
                {
                    if (GetSelected(child) || GetIsOverlappable(child) ||
                        child.Visibility != Visibility.Visible) continue;

                    var childRect = new Rect(GetLeft(child), GetTop(child),
                        child.RenderSize.Width, child.RenderSize.Height);

                    var delta = new DeltaLimit
                    {
                        Left = rect.Left - childRect.Right,
                        Right = childRect.Left - rect.Right,
                        Top = rect.Top - childRect.Bottom,
                        Bottom = childRect.Top - rect.Bottom
                    };

                    if (delta.Left < 0 && delta.Right < 0) // NS
                    {
                        if (delta.Top >= 0 && delta.Top < limit.Top) limit.Top = delta.Top;
                        if (delta.Bottom >= 0 && delta.Bottom < limit.Bottom) limit.Bottom = delta.Bottom;
                    }

                    if (delta.Top < 0 && delta.Bottom < 0) // WE
                    {
                        if (delta.Left >= 0 && delta.Left < limit.Left) limit.Left = delta.Left;
                        if (delta.Right >= 0 && delta.Right < limit.Right) limit.Right = delta.Right;
                    }

                    // Since we have not handled NESW and NWSE, we must try to move
                    // the element and if it overlaps nudge it out of the way.
                    // So, we apply preliminary limits and try to move the element.

                    if (dragDelta.X > 0 && dragDelta.X > limit.Right) dragDelta.X = limit.Right;
                    if (dragDelta.X < 0 && -dragDelta.X > limit.Left) dragDelta.X = -limit.Left;
                    if (dragDelta.Y > 0 && dragDelta.Y > limit.Bottom) dragDelta.Y = limit.Bottom;
                    if (dragDelta.Y < 0 && -dragDelta.Y > limit.Top) dragDelta.Y = -limit.Top;

                    var movedRect = Rect.Offset(rect, dragDelta);
                    var intersection = Rect.Intersect(movedRect, childRect);

                    // Test for empty rectangle.
                    if (intersection.IsEmpty) continue;

                    // Rectangle could still be "empty" because of the rounding of
                    // floating points (two elements touching but not overlapping).
                    if (intersection.Width < 0.1 || intersection.Height < 0.1) continue;

                    // With that possibility ruled out, elements must be overlapping!
                    // We need to adjust the limits according to the axis which is
                    // overlapped the most (and according to drag direction).

                    if (intersection.Width < intersection.Height)
                    {
                        if (dragDelta.X > 0 && delta.Right < limit.Right) limit.Right = delta.Right;
                        if (dragDelta.X < 0 && delta.Left < limit.Left) limit.Left = delta.Left;
                    }
                    else
                    {
                        if (dragDelta.Y > 0 && delta.Bottom < limit.Bottom) limit.Bottom = delta.Bottom;
                        if (dragDelta.Y < 0 && delta.Top < limit.Top) limit.Top = delta.Top;
                    }
                }
            }

            // If an intersection happened for the last child we checked,
            // we need to update the dragDelta one last time!

            if (dragDelta.X > 0 && dragDelta.X > limit.Right) dragDelta.X = limit.Right;
            if (dragDelta.X < 0 && -dragDelta.X > limit.Left) dragDelta.X = -limit.Left;
            if (dragDelta.Y > 0 && dragDelta.Y > limit.Bottom) dragDelta.Y = limit.Bottom;
            if (dragDelta.Y < 0 && -dragDelta.Y > limit.Top) dragDelta.Y = -limit.Top;

            return dragDelta;
        }

        #endregion

        #endregion
    }
}
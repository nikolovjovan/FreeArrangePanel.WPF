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
    /// <summary>
    ///     Specifies the arrange mode for a child element of FreeArrangePanel.
    /// </summary>
    [Flags]
    public enum ArrangeMode
    {
        None = 0x0,
        MoveOnly = 0x1,
        ResizeHorizontal = 0x2,
        ResizeVertical = 0x4,
        ResizeNESW = 0x8,
        ResizeNWSE = 0x10,
        ResizeSides = ResizeHorizontal | ResizeVertical,
        ResizeCorners = ResizeNESW | ResizeNWSE,
        ResizeOnly = ResizeSides | ResizeCorners,
        MoveAndResizeHorizontal = MoveOnly | ResizeHorizontal,
        MoveAndResizeVertical = MoveOnly | ResizeVertical,
        MoveAndResizeNESW = MoveOnly | ResizeNESW,
        MoveAndResizeNWSE = MoveOnly | ResizeNWSE,
        MoveAndResizeSides = MoveOnly | ResizeSides,
        MoveAndResizeCorners = MoveOnly | ResizeCorners,
        MoveAndResize = MoveOnly | ResizeOnly
    }

    /// <inheritdoc />
    /// <summary>
    ///     Panel control that allows the user to move and resize child controls at runtime.
    /// </summary>
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
            // Public attached properties
            ArrangeModeProperty = DependencyProperty.RegisterAttached("ArrangeModes", typeof(ArrangeMode),
                typeof(FreeArrangePanel), new PropertyMetadata(ArrangeMode.MoveAndResize));
            IsOverlappableProperty = DependencyProperty.RegisterAttached("IsOverlappable", typeof(bool),
                typeof(FreeArrangePanel), new PropertyMetadata(false));
            // Private attached properties
            ArrangeAdornerProperty = DependencyProperty.RegisterAttached("ArrangeAdorner", typeof(ArrangeAdorner),
                typeof(FreeArrangePanel), new PropertyMetadata(null));
            DragSelectedProperty = DependencyProperty.RegisterAttached("DragSelected", typeof(bool),
                typeof(FreeArrangePanel), new PropertyMetadata(false));
            SelectedProperty = DependencyProperty.RegisterAttached("Selected", typeof(bool),
                typeof(FreeArrangePanel), new PropertyMetadata(false));
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets a <see cref="Brush" /> that is used to fill the area between the borders of a
        ///     <see cref="FreeArrangePanel" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="Brush" />. This default value is <see langword="transparent" />.
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
        ///     Gets or sets a <see cref="double" /> that specifies the distance the mouse cursor needs to drag a
        ///     selection or an element in order to start a drag.
        /// </summary>
        /// <returns>
        ///     A <see cref="double" />. This default value is <see langword="5.0" />.
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
        ///     Gets or sets a <see cref="double" /> that specifies the percentage of the control that the drag selection
        ///     rectangle needs to be over in order to select the element.
        /// </summary>
        /// <returns>
        ///     A <see cref="double" />. This default value is <see langword="0.5" />.
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
        ///     Gets or sets the selection <see cref="RenderMode" />.
        /// </summary>
        public RenderMode SelectionRenderMode { get; set; } = RenderMode.Inside;

        /// <summary>
        ///     Gets or sets the resize handle <see cref="RenderMode" />.
        /// </summary>
        public RenderMode ResizeHandleRenderMode { get; set; } = RenderMode.Outside;

        /// <summary>
        ///     Gets or sets a <see cref="bool" /> that specifies whether to forward mouse events to the underlying
        ///     controls.
        /// </summary>
        /// <returns>
        ///     A <see cref="bool" />. This default value is <see langword="false" />.
        /// </returns>
        /// <remarks>
        ///     Setting this to True won't capture mouse events so when you exit the panel the element
        ///     could still be dragged even if you released the left mouse button. If you really need to forward
        ///     mouse events to underlying controls, you should use <see cref="ArrangingEnabled" /> property to
        ///     temporarily disable arranging and then re-enable it in the control's mouse event handler.
        /// </remarks>
        public bool ForwardMouseEvents { get; set; } = false;

        /// <summary>
        ///     Gets or sets a <see cref="bool" /> that specifies whether to limit control movement to the panel
        ///     bounds.
        /// </summary>
        /// <returns>
        ///     A <see cref="bool" />. This default value is <see langword="true" />.
        /// </returns>
        public bool LimitMovementToPanel { get; set; } = true;

        /// <summary>
        ///     Gets or sets a <see cref="bool" /> that specifies whether to prevent control overlap on controls with
        ///     <see cref="IsOverlappableProperty" /> property set to false.
        /// </summary>
        /// <returns>
        ///     A <see cref="bool" />. This default value is <see langword="true" />.
        /// </returns>
        public bool PreventOverlap { get; set; } = true;

        /// <summary>
        ///     Gets or sets a <see cref="bool" /> that specifies whether to enable moving and resizing of children of this panel.
        /// </summary>
        /// <returns>
        ///     A <see cref="bool" />. This default value is <see langword="true" />.
        /// </returns>
        public bool ArrangingEnabled { get; set; } = true;

        /// <summary>
        ///     Gets or sets a <see cref="bool" /> that specifies whether to enable keyboard shortcuts for this panel.
        /// </summary>
        /// <returns>
        ///     A <see cref="bool" />. This default value is <see langword="true" />.
        /// </returns>
        public bool KeyboardShortcutsEnabled { get; set; } = true;

        /// <summary>
        ///     Gets a list of currently selected elements.
        /// </summary>
        /// <returns>A <see cref="IList{T}" /> that contains currently selected elements.</returns>
        public IList<UIElement> SelectedElements { get; }

        /// <summary>
        ///     Specifies the <see cref="ArrangeMode" /> of the child element.
        /// </summary>
        public static readonly DependencyProperty ArrangeModeProperty;

        /// <summary>
        ///     Specifies whether the child element is overlappable or not.
        /// </summary>
        public static readonly DependencyProperty IsOverlappableProperty;

        #endregion

        #region Dependency Property Methods

        /// <summary>
        ///     Sets the <see cref="ArrangeMode" /> of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <param name="value">New <see cref="ArrangeMode" /> of the specified element.</param>
        public static void SetArrangeMode(DependencyObject element, ArrangeMode value)
        {
            if (element == null) throw new ArgumentException(nameof(element));
            element.SetValue(ArrangeModeProperty, value);
        }

        /// <summary>
        ///     Gets the <see cref="ArrangeMode" /> of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <returns><see cref="ArrangeMode" /> of the specified element.</returns>
        public static ArrangeMode GetArrangeMode(DependencyObject element)
        {
            if (element == null) throw new ArgumentException(nameof(element));
            return (ArrangeMode) element.GetValue(ArrangeModeProperty);
        }

        /// <summary>
        ///     Sets a <see cref="bool" /> value that specifies whether an element is overlappable or not.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <param name="value">A <see cref="bool" /> value that specifies whether an element is overlappable or not.</param>
        public static void SetIsOverlappable(DependencyObject element, bool value)
        {
            if (element == null) throw new ArgumentException(nameof(element));
            element.SetValue(IsOverlappableProperty, value);
        }

        /// <summary>
        ///     Gets the <see cref="bool" /> value that specifies whether an element is overlappable or not.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <returns>A <see cref="bool" /> value that specifies whether an element is overlappable or not.</returns>
        public static bool GetIsOverlappable(DependencyObject element)
        {
            if (element == null) throw new ArgumentException(nameof(element));
            return (bool) element.GetValue(IsOverlappableProperty);
        }

        /// <summary>
        ///     Gets the selection of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <returns>True if selected, False if deselected.</returns>
        public static bool GetSelected(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (bool) element.GetValue(SelectedProperty);
        }

        #endregion

        #region Other Methods

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

            if (!ArrangingEnabled) return;

            mMouseLeftDown = true;
            mControlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

            if (ReferenceEquals(e.Source, this))
            {
                mDragSelectionAdorner.StartPoint = mDragSelectionAdorner.EndPoint = e.GetPosition(this);

                if (!mControlDown) DeselectAll();

                CaptureMouse();
                Focus();
            }
            else
            {
                var element = (UIElement) e.Source;

                mMouseDownPosition = e.GetPosition(element);

                if (!mControlDown && !GetSelected(element))
                {
                    DeselectAll();
                    SetSelected(element, true);
                }

                if (ForwardMouseEvents) return;

                element.CaptureMouse();
            }

            e.Handled = true;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (!ArrangingEnabled) return;

            mMouseLeftDown = false;

            if (ReferenceEquals(e.Source, this))
            {
                if (mDragSelecting)
                {
                    // Hide the drag selection overlay
                    SetDragSelectionOverlayVisibility(false);

                    // Show the resize overlay if there was only one selected element
                    if (SelectedElements.Count == 1)
                        SetResizeHandleVisibility(SelectedElements[0], true);

                    // Select the elements that were drag selected (automatically updates the overlays)
                    foreach (UIElement child in Children)
                        SetSelected(child, GetDragSelected(child));
                }

                mMovingElements = false;

                ReleaseMouseCapture();
            }
            else
            {
                var element = (UIElement) e.Source;

                if (mMovingElements) mMovingElements = false;
                else
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

                // Show the selection overlays since the move is done
                foreach (var selectedElement in SelectedElements)
                    SetSelectionOverlayVisibility(selectedElement, true);

                // Show the resize overlay if there was only one selected element
                if (SelectedElements.Count == 1)
                    SetResizeHandleVisibility(SelectedElements[0], true);

                if (ForwardMouseEvents) return;

                element.ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (!ArrangingEnabled) return;

            var source = (UIElement) e.Source;
            var cursorPosition = e.GetPosition(source);

            if (ReferenceEquals(source, this))
            {
                if (mDragSelecting)
                {
                    mControlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                    mDragSelectionAdorner.EndPoint = cursorPosition;
                    var dragRect = new Rect(mDragSelectionAdorner.StartPoint, mDragSelectionAdorner.EndPoint);
                    foreach (UIElement child in Children)
                    {
                        var childRect = new Rect(new Point(GetLeft(child), GetTop(child)), child.RenderSize);
                        var intersection = Rect.Intersect(dragRect, childRect);
                        var percentage = intersection.IsEmpty
                            ? 0.0
                            : intersection.Width * intersection.Height / (childRect.Width * childRect.Height);
                        var value = mControlDown && GetSelected(child);
                        if (percentage >= SelectionThreshold) SetDragSelected(child, !value);
                        else SetDragSelected(child, value);
                    }
                }
                else if (mMouseLeftDown)
                {
                    var dragLength = (cursorPosition - mDragSelectionAdorner.StartPoint).Length;
                    if (dragLength > DragThreshold)
                    {
                        // Show the drag selection overlay
                        SetDragSelectionOverlayVisibility(true);
                        mDragSelectionAdorner.EndPoint = cursorPosition;

                        // Hide the resize overlay if there was only one selected element
                        if (SelectedElements.Count == 1)
                            SetResizeHandleVisibility(SelectedElements[0], false);
                    }
                }
            }
            else
            {
                var dragDelta = cursorPosition - mMouseDownPosition;
                if (mMovingElements) ArrangeHelper.MoveSelectedElements(this, dragDelta);
                else if (mMouseLeftDown && mCanBeMoved && GetSelected(source) && dragDelta.Length > DragThreshold)
                {
                    mMovingElements = true;

                    // Hide the selection overlays before starting the move
                    foreach (var selectedElement in SelectedElements)
                        SetSelectionOverlayVisibility(selectedElement, false);

                    // Hide the resize overlay if there was only one selected element
                    if (SelectedElements.Count == 1)
                        SetResizeHandleVisibility(SelectedElements[0], false);
                }

                if (ForwardMouseEvents) return;
            }

            e.Handled = true;
        }

        #endregion

        #region Keyboard Shortcuts

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!ArrangingEnabled || !KeyboardShortcutsEnabled) return;

            var move = new Vector();

            switch (e.Key)
            {
                case Key.A:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) SelectAll();
                    break;
                case Key.NumPad8:
                    move.Y = -1;
                    break;
                case Key.NumPad2:
                    move.Y = 1;
                    break;
                case Key.NumPad4:
                    move.X = -1;
                    break;
                case Key.NumPad6:
                    move.X = 1;
                    break;
                case Key.NumPad7:
                    move.X = -1;
                    move.Y = -1;
                    break;
                case Key.NumPad9:
                    move.X = 1;
                    move.Y = -1;
                    break;
                case Key.NumPad1:
                    move.X = -1;
                    move.Y = 1;
                    break;
                case Key.NumPad3:
                    move.X = 1;
                    move.Y = 1;
                    break;
                case Key.Up: goto case Key.NumPad8;
                case Key.Down: goto case Key.NumPad2;
                case Key.Left: goto case Key.NumPad4;
                case Key.Right: goto case Key.NumPad6;
            }

            if (Math.Abs(move.X) > ArrangeHelper.Epsilon || Math.Abs(move.Y) > ArrangeHelper.Epsilon)
                ArrangeHelper.MoveSelectedElements(this, move);

            e.Handled = true;
        }

        #endregion

        #endregion

        #region Private

        #region Fields 

        /// <summary>
        ///     Specifies whether an element is selected or not.
        /// </summary>
        private static readonly DependencyProperty SelectedProperty;

        /// <summary>
        ///     Specifies the selection <see cref="RenderMode" /> of an element.
        /// </summary>
        private static readonly DependencyProperty DragSelectedProperty;

        /// <summary>
        ///     Stores a reference to the <see cref="ArrangeAdorner" /> of an element.
        /// </summary>
        private static readonly DependencyProperty ArrangeAdornerProperty;

        /// <summary>
        ///     Stores a reference to the <see cref="DragSelectionAdorner" /> of this panel.
        /// </summary>
        private readonly DragSelectionAdorner mDragSelectionAdorner;

        /// <summary>
        ///     Specifies the distance the mouse needs to travel to start dragging.
        /// </summary>
        private double mDragThreshold = 5.0;

        /// <summary>
        ///     Specifies the percentage of the control that the drag selection rectangle needs to be over in order to select it.
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
        ///     Specifies whether all the selected elements can be moved.
        /// </summary>
        private bool mCanBeMoved;

        /// <summary>
        ///     Specifies whether the selected elements are being moved.
        /// </summary>
        private bool mMovingElements;

        #endregion

        #region Dependency Property Methods

        /// <summary>
        ///     Sets the <see cref="ArrangeAdorner" /> of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <param name="value"><see cref="ArrangeAdorner" /> object. If null, removes it from the element.</param>
        private static void SetArrangeAdorner(UIElement element, ArrangeAdorner value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var oldValue = (ArrangeAdorner) element.GetValue(ArrangeAdornerProperty);
            if (ReferenceEquals(value, oldValue)) return;
            element.SetValue(ArrangeAdornerProperty, value);
            if (value != null) AdornerLayer.GetAdornerLayer(element).Add(value);
            else AdornerLayer.GetAdornerLayer(element).Remove(oldValue);
        }

        /// <summary>
        ///     Gets the <see cref="ArrangeAdorner" /> of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <returns><see cref="ArrangeAdorner" /> object.</returns>
        private static ArrangeAdorner GetArrangeAdorner(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (ArrangeAdorner) element.GetValue(ArrangeAdornerProperty);
        }

        /// <summary>
        ///     Sets the selection of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <param name="value">True to select, False to deselect.</param>
        /// <param name="remove">Specifies whether to remove the <see cref="SelectedElements" /> list if deselected.</param>
        private void SetSelected(UIElement element, bool value, bool remove = true)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (value == (bool) element.GetValue(SelectedProperty)) return;
            element.SetValue(SelectedProperty, value);
            UpdateZOrder(element);
            SetSelectionOverlayVisibility(element, value);
            if (value)
            {
                // Always add the element to the list if it is not already added
                SelectedElements.Add(element);
                // If this element cannot be moved then we cannot move any selected element
                if (SelectedElements.Count == 1) mCanBeMoved = true;
                mCanBeMoved &= GetArrangeMode(element).HasFlag(ArrangeMode.MoveOnly);
                // We can resize the element only if it is the only selected element
                SetResizeHandleVisibility(element, SelectedElements.Count == 1);
                // Otherwise we must disable resizing if one more element gets selected
                if (SelectedElements.Count == 2)
                    SetResizeHandleVisibility(SelectedElements[0], false);
            }
            else
            {
                // Always remove the element from the list except if we are deselecting all elements
                if (remove)
                {
                    SelectedElements.Remove(element);
                    // We must update the flag by iterating through the list since we do not know
                    // if multiple elements have blocked the movement of the group
                    mCanBeMoved = true;
                    foreach (var selectedElement in SelectedElements)
                    {
                        if (GetArrangeMode(selectedElement).HasFlag(ArrangeMode.MoveOnly)) continue;
                        mCanBeMoved = false;
                        break; // Break as soon as the flag is set to false
                    }
                }
                // Disable resizing for this element since it is deselected
                SetResizeHandleVisibility(element, false);
                // If the element was removed and there is exactly one element left, enable resizing
                if (remove && SelectedElements.Count == 1)
                    SetResizeHandleVisibility(SelectedElements[0], true);
            }
        }

        /// <summary>
        ///     Sets the selection <see cref="RenderMode" /> of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <param name="value">
        ///     True for <see cref="SelectionRenderMode" />, False for <see cref="RenderMode.None" />.
        /// </param>
        private void SetDragSelected(UIElement element, bool value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(DragSelectedProperty, value);
            SetSelectionOverlayVisibility(element, value);
        }

        /// <summary>
        ///     Gets the selection <see cref="RenderMode" /> of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <returns>Selection <see cref="RenderMode" /> of the specified element.</returns>
        private static bool GetDragSelected(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (bool) element.GetValue(DragSelectedProperty);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        ///     Sets the <see cref="DragSelectionAdorner" /> visibility and updates the <see cref="mDragSelecting" /> flag.
        /// </summary>
        /// <param name="value">True for Visible, False for Collapsed.</param>
        private void SetDragSelectionOverlayVisibility(bool value)
        {
            mDragSelecting = value;
            mDragSelectionAdorner.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        ///     Sets the selection overlay visibility of the specified element by changing its <see cref="RenderMode" />.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <param name="value">
        ///     True for <see cref="SelectionRenderMode" />, False for <see cref="RenderMode.None" />.
        /// </param>
        private void SetSelectionOverlayVisibility(UIElement element, bool value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            GetArrangeAdorner(element).SelectionRenderMode = value ? SelectionRenderMode : RenderMode.None;
        }

        /// <summary>
        ///     Sets the resize handle overlay visibility of the specified element by changing its <see cref="RenderMode" />.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <param name="value">
        ///     True for <see cref="ResizeHandleRenderMode" />, False for <see cref="RenderMode.None" />.
        /// </param>
        private void SetResizeHandleVisibility(UIElement element, bool value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            GetArrangeAdorner(element).ResizeHandleRenderMode = value ? ResizeHandleRenderMode : RenderMode.None;
        }

        /// <summary>
        ///     Updates the ZOrder of the specified element.
        /// </summary>
        /// <param name="element">A child element of this panel.</param>
        /// <param name="remove">True to remove the element from the visual tree, False otherwise.</param>
        private void UpdateZOrder(UIElement element, bool remove = false)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            var elementZIndex = GetZIndex(element);
            var elementNewZIndex = int.MaxValue;

            if (GetSelected(element) || remove)
            {
                // Decrement the ZIndices of elements with higher ZIndex than this.
                foreach (UIElement child in Children)
                {
                    var childZIndex = GetZIndex(child);
                    if (childZIndex > elementZIndex) SetZIndex(child, childZIndex - 1);
                }

                // If the element is being removed, we are done.
                if (remove) return;

                // Else the element is being selected, so we need set its ZIndex to max.
                elementNewZIndex = Children.Count - 1;
            }
            else
            {
                // Since the element is being deselected, we need to push it down below all
                // selected elements but still above all deselected elements.
                // So, we increment the ZIndices of elements with lower ZIndex than this,
                // and then set the ZIndex of this element to the lowest ZIndex of the
                // selected elements.

                foreach (var selectedElement in SelectedElements)
                {
                    var childZIndex = GetZIndex(selectedElement);
                    if (childZIndex < elementZIndex) SetZIndex(selectedElement, childZIndex + 1);
                    if (childZIndex < elementNewZIndex) elementNewZIndex = childZIndex;
                }
            }

            SetZIndex(element, elementNewZIndex);
        }

        #endregion

        #endregion
    }
}
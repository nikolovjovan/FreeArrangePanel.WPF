using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FreeArrangePanel.Adorners;

namespace FreeArrangePanel.Controls
{
    public class FreeArrangePanel : Canvas
    {
        #region Public

        #region Constructors

        public FreeArrangePanel()
        {
            mDragSelectionAdorner = new DragSelectionAdorner(this);
            SelectedElements = new List<UIElement>();
            Background = Brushes.Transparent; // Mouse hit testing does not work when Background is null!
            Focusable = true; // Allow the panel to get keyboard focus
            Loaded += OnPanelLoaded; // Add drag selection adorner on load
            Unloaded += OnPanelUnloaded; // Remove drag selection adorner on unload
        }

        static FreeArrangePanel()
        {
            SelectedProperty = DependencyProperty.RegisterAttached("Selected", typeof(bool),
                typeof(FreeArrangePanel), new PropertyMetadata(false));
            RenderSelectionProperty = DependencyProperty.RegisterAttached("RenderSelection", typeof(bool),
                typeof(FreeArrangePanel), new PropertyMetadata(false));
            ArrangeAdornerProperty = DependencyProperty.RegisterAttached("ArrangeAdorner", typeof(ArrangeAdorner),
                typeof(FreeArrangePanel), new PropertyMetadata(null));
        }

        #endregion

        #region Properties

        public new Brush Background
        {
            get => base.Background;
            set
            {
                if (value == null) value = Brushes.Transparent;
                base.Background = value;
            }
        }

        public double DragThreshold
        {
            get => mDragThreshold;
            set
            {
                if (value < 0.5) value = 0.5;
                mDragThreshold = value;
            }
        }

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

        public bool ForwardMouseEvents { get; set; }

        public IList<UIElement> SelectedElements { get; }

        #endregion

        #region Methods

        public void SelectAll()
        {
            if (SelectedElements.Count == Children.Count) return;
            foreach (UIElement child in Children)
                SetSelected(child, true);
        }

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

        protected static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement) sender;
            element.Loaded -= OnElementLoaded;
            SetArrangeAdorner(element, new ArrangeAdorner(element));
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

                mCursorStart = e.GetPosition(this);

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

            var cursorPosition = e.GetPosition(this);

            if (ReferenceEquals(e.Source, this))
            {
                if (mDragSelecting)
                {
                    mDragSelectionAdorner.EndPoint = cursorPosition;
                    AdornerLayer.GetAdornerLayer(this).Update();

                    mControlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
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
                    var dragDistance = (cursorPosition - mDragSelectionAdorner.StartPoint).Length;
                    if (dragDistance > DragThreshold)
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
                    var dragDelta = cursorPosition - mCursorStart;
                    mCursorStart = cursorPosition;

                    foreach (var element in SelectedElements)
                    {
                        SetLeft(element, GetLeft(element) + dragDelta.X);
                        SetTop(element, GetTop(element) + dragDelta.Y);
                    }
                }
                else if (mMouseLeftDown && GetSelected((UIElement) e.Source))
                {
                    var dragDelta = (cursorPosition - mCursorStart).Length;
                    if (dragDelta > DragThreshold) mMovingElements = true;
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

        private static readonly DependencyProperty SelectedProperty;
        private static readonly DependencyProperty RenderSelectionProperty;
        private static readonly DependencyProperty ArrangeAdornerProperty;

        private readonly DragSelectionAdorner mDragSelectionAdorner;

        private double mDragThreshold = 5.0;
        private double mSelectionThreshold = 0.5;

        private Point mCursorStart;

        private bool mMouseLeftDown;
        private bool mControlDown;
        private bool mDragSelecting;
        private bool mMovingElements;

        #endregion

        #region Dependency Properties Methods

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

        #endregion

        #endregion
    }
}
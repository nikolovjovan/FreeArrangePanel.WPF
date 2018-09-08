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

        public FreeArrangePanel()
        {
            mDragSelectionAdorner = new DragSelectionAdorner(this);
            SelectedElements = new List<UIElement>();
            Background = Brushes.Transparent; // Mouse hit testing does not work when Background is null!
            Focusable = true; // Allow the panel to get keyboard focus
            Loaded += OnPanelLoaded; // Add drag selection adorner on load
            Unloaded += OnPanelUnloaded; // Remove drag selection adorner on unload
        }

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
                if (value < 1.0) value = 1.0;
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

        #region Protected

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualRemoved != null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer((Visual) visualRemoved);
                var adorners = adornerLayer.GetAdorners((UIElement) visualRemoved);
                if (adorners != null)
                    foreach (var adorner in adorners)
                        adornerLayer.Remove(adorner);
            }

            if (visualAdded != null) ((FrameworkElement) visualAdded).Loaded += OnElementLoaded;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            mCtrlSelecting = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (!ReferenceEquals(e.Source, this))
            {
                var element = e.Source as UIElement;
                if (mCtrlSelecting) SetSelected(element, !GetSelected(element));
                if (!ForwardMouseEvents) e.Handled = true;
                return;
            }

            mDragSelectionAdorner.StartPoint = mDragSelectionAdorner.EndPoint = e.GetPosition(this);

            if (!mCtrlSelecting)
            {
                SelectedElements.Clear();
                foreach (UIElement child in Children) SetSelected(child, false, false);
            }

            MouseLeftDown = true;

            Focus(); // Attain keyboard focus

            e.Handled = true;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (!ReferenceEquals(e.Source, this))
            {
                if (!mCtrlSelecting)
                {
                    SelectedElements.Clear();
                    var element = e.Source as UIElement;
                    foreach (UIElement child in Children) SetSelected(child, ReferenceEquals(child, element));
                }

                if (!ForwardMouseEvents) e.Handled = true;
                return;
            }

            if (DragSelecting)
            {
                DragSelecting = false;
                foreach (UIElement child in Children) SetSelected(child, GetDragSelected(child));
            }

            MouseLeftDown = false;

            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var cursorPosition = e.GetPosition(this);

            if (DragSelecting)
            {
                // Update adorner
                mDragSelectionAdorner.EndPoint = cursorPosition;
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                adornerLayer.Update();

                // Select elements
                mCtrlSelecting = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                var dragRect = new Rect(mDragSelectionAdorner.StartPoint, mDragSelectionAdorner.EndPoint);
                foreach (UIElement child in Children)
                {
                    var childRect = new Rect(new Point(GetLeft(child), GetTop(child)), child.RenderSize);
                    var intersection = Rect.Intersect(dragRect, childRect);
                    var percentage = intersection.IsEmpty
                        ? 0.0
                        : intersection.Width * intersection.Height / (childRect.Width * childRect.Height);
                    var value = mCtrlSelecting && GetSelected(child);
                    if (percentage >= SelectionThreshold) SetDragSelected(child, !value);
                    else SetDragSelected(child, value);
                }
            }
            else if (MouseLeftDown)
            {
                var dragDistance = Math.Abs((cursorPosition - mDragSelectionAdorner.StartPoint).Length);
                if (dragDistance > DragThreshold)
                {
                    DragSelecting = true;
                    mDragSelectionAdorner.EndPoint = cursorPosition;
                    var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                    adornerLayer.Update();
                }
            }

            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key != Key.A) return;
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;
            foreach (UIElement child in Children) SetSelected(child, true);

            e.Handled = true;
        }

        #endregion

        #region Private

        private static readonly DependencyProperty SelectedProperty = DependencyProperty.RegisterAttached(
            "Selected", typeof(bool), typeof(FreeArrangePanel), new PropertyMetadata(false));

        private static readonly DependencyProperty DragSelectedProperty = DependencyProperty.RegisterAttached(
            "DragSelected", typeof(bool), typeof(FreeArrangePanel), new PropertyMetadata(false));

        private readonly DragSelectionAdorner mDragSelectionAdorner;

        private bool mMouseLeftDown;
        private bool mCtrlSelecting;
        private bool mDragSelecting;

        private double mDragThreshold = 5.0;
        private double mSelectionThreshold = 0.5;

        private bool MouseLeftDown
        {
            get => mMouseLeftDown;
            set
            {
                mMouseLeftDown = value;
                if (value) CaptureMouse();
                else ReleaseMouseCapture();
            }
        }

        private bool DragSelecting
        {
            get => mDragSelecting;
            set
            {
                mDragSelecting = value;
                mDragSelectionAdorner.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SetSelected(UIElement element, bool value, bool modifyList = true)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var oldValue = (bool) element.GetValue(SelectedProperty);
            if (value == oldValue) return;
            element.SetValue(SelectedProperty, value);
            SetDragSelected(element, value);
            if (!modifyList) return;
            if (value) SelectedElements.Add(element);
            else SelectedElements.Remove(element);
        }

        private static bool GetSelected(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (bool) element.GetValue(SelectedProperty);
        }

        private static void SetDragSelected(UIElement element, bool value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(DragSelectedProperty, value);
            var adornerLayer = AdornerLayer.GetAdornerLayer(element);
            var adorners = adornerLayer.GetAdorners(element);
            if (adorners == null) return;
            foreach (var adorner in adorners) adorner.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool GetDragSelected(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (bool) element.GetValue(DragSelectedProperty);
        }

        private void OnPanelLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnPanelLoaded;
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            adornerLayer.Add(mDragSelectionAdorner);
        }

        private void OnPanelUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnPanelUnloaded;
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            adornerLayer.Remove(mDragSelectionAdorner);
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement) sender;
            element.Loaded -= OnElementLoaded;
            var adornerLayer = AdornerLayer.GetAdornerLayer(element);
            adornerLayer.Add(new ArrangeAdorner(element));
        }
        
        #endregion
    }
}
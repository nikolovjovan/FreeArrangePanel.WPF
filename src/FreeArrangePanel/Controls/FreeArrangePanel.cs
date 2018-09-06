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
        private readonly DragSelectionAdorner mDragSelectionAdorner;
        private readonly LinkedList<UIElement> mSelectedElements;

        private bool mDragSelecting;
        private bool mMouseDown;

        private double mDragThreshold = 5.0;
        private double mSelectionThreshold = 0.5;

        public FreeArrangePanel()
        {
            mDragSelectionAdorner = new DragSelectionAdorner(this);
            mSelectedElements = new LinkedList<UIElement>();
            // Mouse hit testing does not work on null Background brush!!!
            // TODO: Try to override default null to transparent...
            Background = Brushes.Transparent;
            Focusable = true; // Allow the panel to get keyboard focus
            Loaded += OnPanelLoaded; // Add drag selection adorner on load
            Unloaded += OnPanelUnloaded; // Remove drag selection adorner on unload
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

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualRemoved != null)
            {
                // Remove all adorners from the adorner layer...
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

            if (!ReferenceEquals(e.Source, this))
            {
                var element = e.Source as UIElement;
                var ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                // TODO: Don't deselect all if the element is selected to allow moving
                //       (or use Thumb in ArrangeAdorner for moving)
                // TODO: Deselect all on PreviewMouseLeftButtonUp (like in Windows Explorer)
                if (!ctrlDown) DeselectAll();
                if (ctrlDown && mSelectedElements.Contains(element)) DeselectElement(element, true);
                else SelectElement(element);
                // TODO: Add a property to specify whether to forward mouse events or not?!?
                e.Handled = true;
                return;
            }

            // TODO: Add CTRL drag and SHIFT drag like in Windows Explorer...

            if (mDragSelecting) StopDragging();

            DeselectAll();

            mMouseDown = true;
            mDragSelectionAdorner.StartPoint = mDragSelectionAdorner.EndPoint = e.GetPosition(this);

            CaptureMouse();
            Focus(); // Attain keyboard focus

            e.Handled = true;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (!ReferenceEquals(e.OriginalSource, this)) return;

            if (mDragSelecting)
            {
                StopDragging();
                DragSelectElements();
            }

            if (!mMouseDown) return;

            mMouseDown = false;
            ReleaseMouseCapture();

            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (mDragSelecting) Drag(e.GetPosition(this));
            else if (mMouseDown)
            {
                var currentPoint = e.GetPosition(this);
                var dragDistance = Math.Abs((currentPoint - mDragSelectionAdorner.StartPoint).Length);

                if (dragDistance > mDragThreshold)
                {
                    DeselectAll();
                    mDragSelecting = true;
                    StartDragging(e.GetPosition(this));
                }
            }

            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key != Key.A) return;
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;
            SelectAll();
            e.Handled = true;
        }

        private void Drag(Point endPoint, bool starting = false)
        {
            mDragSelectionAdorner.EndPoint = endPoint;
            if (starting) mDragSelectionAdorner.Visibility = Visibility.Visible;
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            adornerLayer.Update();
            DragSelectElements();
        }

        private void StartDragging(Point endPoint)
        {
            Drag(endPoint, true);
        }

        private void StopDragging()
        {
            mDragSelecting = false;
            mDragSelectionAdorner.Visibility = Visibility.Collapsed;
        }

        private void DragSelectElements()
        {
            var dragRect = new Rect(mDragSelectionAdorner.StartPoint, mDragSelectionAdorner.EndPoint);
            foreach (UIElement child in Children)
            {
                var childRect = new Rect(new Point(GetLeft(child), GetTop(child)), child.RenderSize);
                var intersection = Rect.Intersect(dragRect, childRect);
                var percentage = intersection.IsEmpty
                    ? 0.0
                    : intersection.Width * intersection.Height / (childRect.Width * childRect.Height);
                if (percentage > mSelectionThreshold) SelectElement(child);
                else DeselectElement(child, true);
            }
        }

        private void SelectAll()
        {
            foreach (UIElement child in Children) SelectElement(child);
        }

        private void DeselectAll()
        {
            foreach (UIElement child in Children) DeselectElement(child);
            mSelectedElements.Clear();
        }

        private void SelectElement(UIElement child)
        {
            if (mSelectedElements.Contains(child)) return;
            mSelectedElements.AddLast(child);
            var adornerLayer = AdornerLayer.GetAdornerLayer(child);
            var adorners = adornerLayer.GetAdorners(child);
            if (adorners == null) return;
            foreach (var adorner in adorners) adorner.Visibility = Visibility.Visible;
        }

        private void DeselectElement(UIElement child, bool remove = false)
        {
            if (remove) mSelectedElements.Remove(child);
            var adornerLayer = AdornerLayer.GetAdornerLayer(child);
            var adorners = adornerLayer.GetAdorners(child);
            if (adorners == null) return;
            foreach (var adorner in adorners) adorner.Visibility = Visibility.Collapsed;
        }
    }
}
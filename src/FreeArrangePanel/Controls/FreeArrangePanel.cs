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
            // Try to override default null to transparent...
            Background = Brushes.Transparent;
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
            var element = (FrameworkElement)sender;
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

            if (!ReferenceEquals(e.OriginalSource, this)) return;

            if (mDragSelecting) StopDragging();

            DeselectElements();

            mMouseDown = true;
            mDragSelectionAdorner.StartPoint = mDragSelectionAdorner.EndPoint = e.GetPosition(this);

            CaptureMouse();

            e.Handled = true;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (!ReferenceEquals(e.OriginalSource, this)) return;

            if (mDragSelecting)
            {
                StopDragging();
                SelectElements();
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
                    DeselectElements();
                    mDragSelecting = true;
                    StartDragging(e.GetPosition(this));
                }
            }

            e.Handled = true;
        }

        private void Drag(Point endPoint, bool starting = false)
        {
            mDragSelectionAdorner.EndPoint = endPoint;
            if (starting) mDragSelectionAdorner.Visibility = Visibility.Visible;
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            adornerLayer.Update();
            SelectElements();
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

        private void SelectElements()
        {
            var dragRect = new Rect(mDragSelectionAdorner.StartPoint, mDragSelectionAdorner.EndPoint);
            //Console.WriteLine("Drag rect: " + dragRect);

            // DEBUG INFO, NOT IDEAL METRIC BUT GOOD ENOUGH TO STOP SPAMMING OUTPUT
            var initialCount = mSelectedElements.Count;

            foreach (UIElement child in Children)
            {
                var childRect = new Rect(new Point(GetLeft(child), GetTop(child)), child.RenderSize);
                var intersection = Rect.Intersect(dragRect, childRect);
                //Console.WriteLine("Child: " + child + " Rect: " + childRect);
                var percentage = intersection.IsEmpty
                    ? 0.0
                    : intersection.Width * intersection.Height / (childRect.Width * childRect.Height);
                //Console.WriteLine("Intersection: " + intersection + " Percentage: " + percentage);
                if (percentage > mSelectionThreshold) SelectElement(child);
                else DeselectElement(child, true);
            }

            if (mSelectedElements.Count == initialCount) return;

            Console.WriteLine("Selected elements...");
            foreach (var selectedElement in mSelectedElements) Console.WriteLine(selectedElement.ToString());
        }

        private void DeselectElements()
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
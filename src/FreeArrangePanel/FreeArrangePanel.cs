﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FreeArrangePanel
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
            Background = Brushes.Transparent;
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

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (!ReferenceEquals(e.OriginalSource, this)) return;
            if (e.ChangedButton != MouseButton.Left) return;

            if (mDragSelecting) StopDragging();

            mMouseDown = true;
            mDragSelectionAdorner.StartPoint = mDragSelectionAdorner.EndPoint = e.GetPosition(this);

            CaptureMouse();

            e.Handled = true;
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (!ReferenceEquals(e.OriginalSource, this)) return;
            if (e.ChangedButton != MouseButton.Left) return;

            if (mDragSelecting)
            {
                StopDragging();
                SelectChildren();
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
                    mSelectedElements.Clear();
                    mDragSelecting = true;
                    StartDragging(e.GetPosition(this));
                }
            }

            e.Handled = true;
        }

        private void Drag(Point endPoint, bool starting = false)
        {
            mDragSelectionAdorner.EndPoint = endPoint;
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (starting) adornerLayer?.Add(mDragSelectionAdorner);
            adornerLayer?.Update();
        }

        private void StartDragging(Point endPoint)
        {
            Drag(endPoint, true);
        }

        private void StopDragging()
        {
            mDragSelecting = false;
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            adornerLayer?.Remove(mDragSelectionAdorner);
        }

        private void SelectChildren()
        {
            var dragRect = new Rect(mDragSelectionAdorner.StartPoint, mDragSelectionAdorner.EndPoint);
            Console.WriteLine("Drag rect: " + dragRect);

            foreach (UIElement child in Children)
            {
                var childRect = new Rect(new Point(GetLeft(child), GetTop(child)), child.RenderSize);
                var intersection = Rect.Intersect(dragRect, childRect);
                Console.WriteLine("Child: " + child + " Rect: " + childRect);
                var percentage = intersection.IsEmpty
                    ? 0.0
                    : intersection.Width * intersection.Height / (childRect.Width * childRect.Height);
                Console.WriteLine("Intersection: " + intersection + " Percentage: " + percentage);
                if (percentage > mSelectionThreshold) mSelectedElements.AddLast(child);
            }

            Console.WriteLine("Selected elements...");
            foreach (var selectedElement in mSelectedElements) Console.WriteLine(selectedElement.ToString());
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Sonosk.Wpf
{
    public static class SliderExtensions
    {
        public static bool GetEnableMouseWheel(DependencyObject obj)
            => (bool)obj.GetValue(EnableMouseWheelProperty);

        public static void SetEnableMouseWheel(DependencyObject obj, bool value)
            => obj.SetValue(EnableMouseWheelProperty, value);

        public static readonly DependencyProperty EnableMouseWheelProperty =
            DependencyProperty.RegisterAttached(
                "EnableMouseWheel",
                typeof(bool),
                typeof(SliderExtensions),
                new UIPropertyMetadata(false, OnEnableMouseWheelChanged));

        private static void OnEnableMouseWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement uielement)
            {
                if ((bool)e.NewValue)
                    uielement.PreviewMouseWheel += Slider_MouseWheel;
                else
                    uielement.PreviewMouseWheel -= Slider_MouseWheel;
            }
        }

        private static void Slider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is RangeBase rangeBase)
            {
                double change = e.Delta * rangeBase.SmallChange;
                rangeBase.Value = Math.Max(rangeBase.Minimum, Math.Min(rangeBase.Maximum, rangeBase.Value + change));
                e.Handled = true;
            }
        }

        public static bool GetEnableDragging(DependencyObject obj)
                   => (bool)obj.GetValue(EnableDraggingProperty);

        public static void SetEnableDragging(DependencyObject obj, bool value)
            => obj.SetValue(EnableDraggingProperty, value);

        public static readonly DependencyProperty EnableDraggingProperty =
            DependencyProperty.RegisterAttached(
                "EnableDragging",
                typeof(bool),
                typeof(SliderExtensions),
                new UIPropertyMetadata(false, OnEnableDraggingChanged));

        private static void OnEnableDraggingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressBar progressBar)
            {
                if ((bool)e.NewValue)
                {
                    progressBar.MouseLeftButtonDown += ProgressBar_MouseLeftButtonDown;
                    progressBar.MouseMove += ProgressBar_MouseMove;
                    progressBar.MouseLeftButtonUp += ProgressBar_MouseLeftButtonUp;
                }
                else
                {
                    progressBar.MouseLeftButtonDown -= ProgressBar_MouseLeftButtonDown;
                    progressBar.MouseMove -= ProgressBar_MouseMove;
                    progressBar.MouseLeftButtonUp -= ProgressBar_MouseLeftButtonUp;
                }
            }
        }

        private static bool isDragging = false;

        private static void ProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            ((UIElement)sender).CaptureMouse();
        }

        private static void ProgressBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ProgressBar progressBar && isDragging)
            {
                var pos = e.GetPosition(progressBar);
                var v = (int)(pos.X / progressBar.ActualWidth * 100);
                progressBar.Value = v;
                e.Handled = true;
            }
        }

        private static void ProgressBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }
    }
}

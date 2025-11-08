using System.Windows;
using System.Windows.Input;

namespace Sonosk.Wpf
{
    public static class MouseWheelBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(MouseWheelBehavior),
                new PropertyMetadata(null, OnCommandChanged));

        public static void SetCommand(UIElement element, ICommand value)
            => element.SetValue(CommandProperty, value);

        public static ICommand GetCommand(UIElement element)
            => (ICommand)element.GetValue(CommandProperty);

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.PreviewMouseWheel -= Element_PreviewMouseWheel;
                if (e.NewValue != null)
                    element.PreviewMouseWheel += Element_PreviewMouseWheel;
            }
        }

        private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var command = GetCommand((UIElement)sender);
            if (command?.CanExecute(e.Delta) == true)
                command.Execute(e.Delta);
        }
    }
}

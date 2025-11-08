using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Sonosk.Wpf
{
    public class SelectedStyleMultiConverter : IMultiValueConverter
    {
        public Style? NormalStyle { get; set; }
        public Style? SelectedStyle { get; set; }

        private readonly Style DefaultStyle = new Style();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = current item (Device or Group)
            // values[1] = SelectedViewModel
            if (values.Length < 2) return NormalStyle ?? DefaultStyle;
            return ReferenceEquals(values[0], values[1]) 
                ? SelectedStyle ?? DefaultStyle 
                : NormalStyle ?? DefaultStyle;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

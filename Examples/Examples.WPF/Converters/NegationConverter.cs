using System;
using System.Globalization;
using System.Windows.Data;

namespace Examples.WPF.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public class NegationConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null ? !(bool)value : false;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value != null ? !(bool)value : false;

}

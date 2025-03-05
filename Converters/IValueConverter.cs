using System;
using System.Globalization;
using Microsoft.Maui.Controls;
namespace OlymPOS.Converters;

public class ReceiptStatusToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ReceiptStatus status)
        {
            return status switch
            {
                ReceiptStatus.AllTrue => "green.png",
                ReceiptStatus.AllFalse => "red.png",
                ReceiptStatus.Partial => "yellow.png",
                _ => "transparent.png" // Default or error case
            };
        }
        return "transparent.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

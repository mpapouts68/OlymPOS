using System;
using System.Globalization;
using Microsoft.Maui.Controls;


namespace OlymPOS.Converters;


public class QuantityToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int quantity)
        {
            return quantity switch
            {
                1 => "o1.png",
                2 => "o2.png",
                3 => "o3.png",
                4 => "o4.png",
                5 => "o5.png", 
                6 => "o6.png",
                7 => "o7.png",
                8 => "o8.png",
                9 => "o9.png",
                0 => "o0.png", 
                _ => "o9p.png" // Default case for quantities outside the 1-10 range or negative
            };
        }
        return "o9p.png"; // Return a default image if the value is not an int or an unexpected value
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // ConvertBack is not implemented because we don't need to convert images back to quantities
        throw new NotImplementedException();
    }
}

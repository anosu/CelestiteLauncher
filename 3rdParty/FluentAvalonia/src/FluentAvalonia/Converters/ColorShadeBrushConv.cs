﻿using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FluentAvalonia.UI.Media;

namespace FluentAvalonia.Converters;

/// <summary>
/// A converter that allows lightening or darkening a color by an amount 
/// specified in the parameter argument
/// </summary>
public class ColorShadeBrushConv : IValueConverter
{

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = (Color2)value;

        if (!float.TryParse(parameter.ToString(), out float amount))
        {
            amount = 0;
        }

        return new SolidColorBrush(color.LightenPercent(amount));
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

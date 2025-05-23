﻿using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace FluentAvalonia.UI.Controls.IconElement;

[TypeConverter(typeof(SymbolIconConverter))]
public partial class SymbolIcon : FAIconElement
{
    private static readonly Typeface _font = new(new FontFamily("avares://FluentAvalonia/Fonts#FluentSystemIcons-Resizable"));

    private bool _suspendCreate = true;
    private TextLayout _textLayout;

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<FontIcon>();

    public static readonly StyledProperty<Symbol> SymbolProperty =
        AvaloniaProperty.Register<SymbolIcon, Symbol>(nameof(Symbol), Symbol.Home);
    public static readonly StyledProperty<bool> IsFilledProperty =
        AvaloniaProperty.Register<SymbolIcon, bool>(nameof(IsFilled));

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public Symbol Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public bool IsFilled
    {
        get => GetValue(IsFilledProperty);
        set => SetValue(IsFilledProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == TextElement.FontSizeProperty)
        {
            InvalidateMeasure();
            InvalidateText();
        }
        else if (change.Property == TextElement.ForegroundProperty ||
                 change.Property == SymbolProperty ||
                 change.Property == IsFilledProperty)
        {
            InvalidateText();
        }

        base.OnPropertyChanged(change);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_suspendCreate || _textLayout is null)
        {
            _suspendCreate = false;
            InvalidateText();
        }

        return new Size(
            Math.Min(availableSize.Width, FontSize),
            Math.Min(availableSize.Height, FontSize));
    }

    public override void Render(DrawingContext context)
    {
        if (_textLayout is null)
            return;

        var canvas = new Rect(Bounds.Size);
        using (context.PushClip(canvas))
        {
            var origin = new Point(canvas.Center.X - _textLayout.Width / 2, 0);
            _textLayout.Draw(context, origin);
        }
    }

    private void InvalidateText()
    {
        if (_suspendCreate)
            return;

        _textLayout = new TextLayout(
            Symbol.ToString(IsFilled),
            _font,
            FontSize,
            Foreground,
            TextAlignment.Center);

        InvalidateVisual();
    }
}

public class SymbolIconConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string) || sourceType == typeof(Symbol))
        {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string val && SymbolExtensions.TryParse(val, out var pSymbol))
        {
            return new SymbolIcon { Symbol = pSymbol };
        }
        else if (value is Symbol symbol)
        {
            return new SymbolIcon { Symbol = symbol };
        }
        return base.ConvertFrom(context, culture, value);
    }
}

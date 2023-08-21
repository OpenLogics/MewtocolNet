using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;


namespace Examples.WPF.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public class ColorHashConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

        var hashCode = value.GetHashCode();
        var randColor = GenerateRandomVibrantColor(new Random(hashCode));

        System.Windows.Media.Brush outBrush = new System.Windows.Media.SolidColorBrush(new System.Windows.Media.Color {
            R = randColor.R,
            G = randColor.G,
            B = randColor.B,
            A = 255,
        });

        return outBrush;

    }

    private Color GenerateRandomVibrantColor(Random random) {

        byte red = (byte)random.Next(256);
        byte green = (byte)random.Next(256);
        byte blue = (byte)random.Next(256);

        Color color = Color.FromArgb(255, red, green, blue);

        // Ensure the color is vibrant and colorful
        while (!IsVibrantColor(color)) {
            red = (byte)random.Next(256);
            green = (byte)random.Next(256);
            blue = (byte)random.Next(256);
            color = Color.FromArgb(255,red, green, blue);
        }

        return color;
    }

    private bool IsVibrantColor(Color color) {

        int minBrightness = 100;
        int maxBrightness = 200;
        int minSaturation = 150;

        int brightness = (int)(color.GetBrightness() * 255);
        int saturation = (int)(color.GetSaturation() * 255);

        return brightness >= minBrightness && brightness <= maxBrightness && saturation >= minSaturation;

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

}

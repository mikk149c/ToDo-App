
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ToDo_App.UI.Appearance
{
    public sealed class ThemedContentDialog : ContentDialog
    {
        public ThemedContentDialog()
        {
            Color accent =
                ((SolidColorBrush)Application.Current.Resources["PrimaryBrush"]).Color;

            Resources = new ResourceDictionary
            {
                ["AccentButtonBackground"] = new SolidColorBrush(accent),
                ["AccentButtonBackgroundPointerOver"] = new SolidColorBrush(lighten(accent, 0.12)),
                ["AccentButtonBackgroundPressed"] = new SolidColorBrush(darken(accent, 0.12)),
                ["AccentButtonForeground"] = new SolidColorBrush(Colors.White),
                ["AccentButtonForegroundPointerOver"] = new SolidColorBrush(Colors.White),
                ["AccentButtonForegroundPressed"] = new SolidColorBrush(Colors.White)
            };
        }

        private static Color lighten(Color color, double amount)
        {
            byte blend(byte channel) =>
                (byte)Math.Round(channel + (255 - channel) * amount);

            return Color.FromArgb(
                color.A,
                blend(color.R),
                blend(color.G),
                blend(color.B));
        }

        private static Color darken(Color color, double amount)
        {
            byte blend(byte channel) =>
                (byte)Math.Round(channel * (1 - amount));

            return Color.FromArgb(
                color.A,
                blend(color.R),
                blend(color.G),
                blend(color.B));
        }
    }
}

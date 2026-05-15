using System.Globalization;
using System.Windows.Data;

namespace Desktop.Infrastructure.Converters;

public class BoolToFactoryPaidConverter : IValueConverter
{
    public static readonly BoolToFactoryPaidConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Desfazer Pgto Fábrica" : "Pagar Fábrica";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Desktop.Features.Orders;

public class StepToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string targetStepStr && int.TryParse(targetStepStr, out int targetStep))
        {
            if (currentStep >= targetStep)
                return Application.Current.FindResource("PrimaryBrush");
        }
        return new SolidColorBrush(Colors.LightGray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

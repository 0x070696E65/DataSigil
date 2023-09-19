using System.Globalization;

namespace DataSigil.Utils;

public class DivideByMillionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ulong xymAmount)
        {
            // 1000000で割る
            double result = xymAmount / 1000000.0;
            return result.ToString();
        }

        return value; // 値が変換できない場合はそのまま返す
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new Exception("Not implemented");
    }
}
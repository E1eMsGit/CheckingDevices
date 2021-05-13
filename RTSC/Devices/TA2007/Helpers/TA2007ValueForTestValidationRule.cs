using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace RTSC.Devices.TA2007.Helpers
{
    class TA2007ValueForTestValidationRule: ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            var decimalSeparator = ci.NumberFormat.NumberDecimalSeparator;
            var floatRegex = string.Format("^[-]?[0-9]+([.]?[0-9]+)?$", decimalSeparator);

            Regex regex = new Regex(floatRegex);
            if (!regex.IsMatch(value.ToString()))
            {
                return new ValidationResult(false, "Неправильное значение.");
            }
            else if (value.ToString().Length == 0 || value.ToString() == null)
            {
                return new ValidationResult(false, "Введите значение.");
            }
            else
            {
                return ValidationResult.ValidResult;
            }
        }
    }
}

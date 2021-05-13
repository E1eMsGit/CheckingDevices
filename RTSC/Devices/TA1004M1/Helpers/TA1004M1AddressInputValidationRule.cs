using System.Globalization;
using System.Windows.Controls;

namespace RTSC.Devices.TA1004M1.Helpers
{
    class TA1004M1AddressInputValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return value.ToString().Length < 4
                ? new ValidationResult(false, "Введите 4 разряда.")
                : ValidationResult.ValidResult;
        }
    }
}

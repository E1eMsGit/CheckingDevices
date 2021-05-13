using System;
using System.Globalization;
using System.Windows.Controls;

namespace RTSC.Devices.TA2006.Helpers
{
    class TA2006DataGridInputByteValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int intVal;           
            
            if (int.TryParse(Convert.ToString(value), out intVal))
            {
                return intVal > byte.MaxValue || intVal < byte.MinValue
                    ? new ValidationResult(false, $"Диапазон значений от {byte.MinValue} до {byte.MaxValue}")
                    : ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "Диапазон значений от 0 до 255");
            }          
        }
    }
}

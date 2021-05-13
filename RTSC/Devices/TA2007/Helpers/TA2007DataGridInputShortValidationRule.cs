using System;
using System.Globalization;
using System.Windows.Controls;

namespace RTSC.Devices.TA2007.Helpers
{
    class TA2007DataGridInputShortValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int intVal;           
            
            if (int.TryParse(Convert.ToString(value), out intVal))
            {
                return intVal > short.MaxValue || intVal < short.MinValue
                    ? new ValidationResult(false, $"Диапазон значений от {short.MinValue} до {short.MaxValue}")
                    : ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "Диапазон значений от 0 до 255");
            }          
        }
    }
}

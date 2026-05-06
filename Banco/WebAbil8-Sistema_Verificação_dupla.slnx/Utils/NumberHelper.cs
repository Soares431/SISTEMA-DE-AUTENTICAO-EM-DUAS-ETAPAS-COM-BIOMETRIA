namespace WebAbil8_Sistema_Verificação_dupla.slnx.Utils
{
    public class NumberHelper
    {

        public static decimal ConvertToDecimal(string strNumber)
        {
            decimal decimalValue;
            if (decimal.TryParse(
                strNumber,
                System.Globalization.NumberStyles.Any, //Essas duas linhas server pra caso o usuario digita virgula ou ponto em numero decimais
                System.Globalization.NumberFormatInfo.InvariantInfo,
                out decimalValue
                ))
                return decimalValue;
            return 0;
        }

        public static bool IsNumeric(string strNumber)
        {
            decimal decimalValue;
            bool IsNumber = decimal.TryParse(
                strNumber,
                System.Globalization.NumberStyles.Any, // Essas duas linhas server pra caso o usuario digita virgula ou ponto em numero decimais
                System.Globalization.NumberFormatInfo.InvariantInfo,
                out decimalValue
                );
            return IsNumber;
        }

    }
}

using System.Globalization;
using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services.Values
{
	public static class NumberFormatHelper
	{
		public static NumberFormatInfo DefaultNumberFormat { get; } = CreateNumberFormatProvider();

		public static NumberFormatInfo CreateNumberFormatProvider(string decimalSeparator)
		{
			var format = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
			format.NumberDecimalSeparator = decimalSeparator;
			return format;
		}

		public static NumberFormatInfo CreateNumberFormatProvider()
		{
			return CreateNumberFormatProvider(DefaultSettings.DecimalSeparator);
		}
	}
}

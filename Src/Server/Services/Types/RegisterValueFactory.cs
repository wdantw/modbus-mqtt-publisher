using ModbusMqttPublisher.Server.Contracts;
using ModbusMqttPublisher.Server.Contracts.Settings;

namespace ModbusMqttPublisher.Server.Services.Types
{
	public class RegisterValueFactory : IRegisterValueFactory
	{
		private IRegisterValue WrapDoubleRegister(IRegisterValue sourceValue, RegisterSettings register)
		{
			if (register.Scale.HasValue)
			{
				return new DoubleRegisterValue(
					register.Scale.Value,
					register.Precision,
					register.DecimalSeparator ?? DefaultSettings.DecimalSeparator,
					sourceValue,
					register.CompareDiff
					);
			}
			else
			{
				return sourceValue;
			}
		}

		private IRegisterValue CreateSource(RegisterSettings registerSettings)
		{
			switch (registerSettings.RegFormat)
			{
				case Contracts.RegisterFormat.Default:

					if (registerSettings.RegType.IsBitReg())
						return new BitRegisterValue();
					else
						return new UshortRegisterValue();

				case Contracts.RegisterFormat.Uint32:
					return new UintRegisterValue();

				case Contracts.RegisterFormat.Uint64:
					return new UlongRegisterValue();

				case Contracts.RegisterFormat.Int16:
					return new ShortRegisterValue();

				case Contracts.RegisterFormat.Int32:
					return new IntRegisterValue();

				case Contracts.RegisterFormat.Int64:
					return new LongRegisterValue();

				case Contracts.RegisterFormat.String:
					return new StringRegisterValue();

				default:
					throw new ArgumentException("Неизвестный формат регистра");
			}
		}

		public IRegisterValue Create(RegisterSettings registerSettings)
		{
			return WrapDoubleRegister(CreateSource(registerSettings), registerSettings);
		}
	}
}

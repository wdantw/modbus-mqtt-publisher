using ModbusMqttPublisher.Server.Contracts.Configs;
using System.Text.RegularExpressions;

namespace ModbusMqttPublisher.Server.Services.Configuration
{
	public static class RegisterMerger
    {

        public static void MergeRegisterCommonParams(this ModbusRegisterCommonParams dest, ModbusRegisterCommonParams overrides)
        {
			dest.Scale = overrides.Scale ?? dest.Scale;
            dest.Precision = overrides.Precision ?? dest.Precision;
            dest.WbEvents = overrides.WbEvents ?? dest.WbEvents;
            dest.ReadPeriod = overrides.ReadPeriod ?? dest.ReadPeriod;
            dest.Tags = overrides.Tags ?? dest.Tags;
            dest.Name = overrides.Name ?? dest.Name;
			dest.DecimalSeparator = overrides.DecimalSeparator ?? dest.DecimalSeparator;
			dest.CompareDiff = overrides.CompareDiff ?? dest.CompareDiff;
			dest.ForcePublish = overrides.ForcePublish ?? dest.ForcePublish;
		}

		private static string[] TagList(this string? tags)
        {
            if (tags == null)
                return Array.Empty<string>();

            return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public static ModbusRegisterCompleted ApplyRegisterModifier(this ModbusRegisterCompleted dest, ModbusRegisterModifier modifier)
        {
            var destTags = dest.Tags.TagList();

            bool needApply =
                modifier.Tags.TagList().Any(t => destTags.Contains(t, StringComparer.CurrentCultureIgnoreCase))
                || (dest.Name != null && modifier.Names != null && Regex.IsMatch(dest.Name, "^" + modifier.Names + "$"));

            if (needApply)
                dest.MergeRegisterCommonParams(modifier);

            return dest!;
        }

        public static ModbusRegisterCompleted ApplyRegisterModifiers(this ModbusRegisterCompleted dest, IEnumerable<ModbusRegisterModifier> modifiers)
        {
            foreach (var modifier in modifiers)
                dest.ApplyRegisterModifier(modifier);

            return dest;
        }

		public static List<ModbusRegisterCompleted> ApplyRegistersModifiers(
			this List<ModbusRegisterCompleted> dest,
			IEnumerable<ModbusRegisterModifier> modifiers
			)
		{
			foreach (var reg in dest)
				reg.ApplyRegisterModifiers(modifiers);

			return dest;
		}

		public static List<ModbusRegisterCompleted> MergeRegisters(
            this List<ModbusRegisterCompleted> dest,
            ModbusRegisterTemplate[] templates,
			IEnumerable<ModbusRegisterModifier> modifiers,
            IEnumerable<ModbusRegisterModifier> globalModifiers
			)
        {
            foreach (var newReg in templates.SelectMany(t => t.ResolveRegisters()))
                dest.Add(newReg.ApplyRegisterModifiers(globalModifiers));

            dest.ApplyRegistersModifiers(modifiers);

            return dest;
        }
    }
}

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace BuildingBlocks.Application;

/// <summary>
/// Resolves the display text of an enum value once per value and reuses it afterwards, so callers on
/// hot paths (consume logs, Hangfire jobs, Kafka handlers) never pay for reflection per call.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the display text for an enum value: the value's <see cref="DescriptionAttribute"/> when it
    /// declares one, otherwise the member name. Declaring a description is therefore optional - an
    /// enum whose member names already read correctly needs no attributes.
    /// </summary>
    /// <param name="value">Enum value to describe.</param>
    /// <returns>
    /// The declared description, or the result of <see cref="object.ToString"/> when the value
    /// declares none or is not a defined member of its enum.
    /// </returns>
    public static string GetDescription<TEnum>(this TEnum value)
        where TEnum : struct, Enum
    {
        return EnumDescriptionCache<TEnum>.GetDescription(value);
    }

    /// <summary>
    /// One cache per closed enum type. The generic type argument keys the cache, so reflection runs
    /// exactly once per enum type and lookups stay strongly typed - no boxing of the enum value, and
    /// no dictionary keyed on <see cref="Type"/>.
    /// </summary>
    private static class EnumDescriptionCache<TEnum>
        where TEnum : struct, Enum
    {
        /// <summary>
        /// Populated by the static initializer, which the runtime guarantees to run once per closed
        /// generic type and to be thread-safe. <see cref="ConcurrentDictionary{TKey, TValue}"/> then
        /// keeps later writes (undefined values) safe across threads.
        /// </summary>
        private static readonly ConcurrentDictionary<TEnum, string> DescriptionsByValue = ReadDeclaredDescriptions();

        /// <summary>
        /// Returns the cached description, computing and caching one for values that were not declared
        /// on the enum.
        /// </summary>
        public static string GetDescription(TEnum value)
        {
            // Declared members are already present. An undefined value - a cast out-of-range integer,
            // or a combination of flags - falls back to ToString() and is cached on first use so the
            // fallback is paid at most once per distinct value.
            return DescriptionsByValue.GetOrAdd(value, static undefinedValue => undefinedValue.ToString());
        }

        /// <summary>
        /// Reads every declared member of the enum once and records its description.
        /// </summary>
        private static ConcurrentDictionary<TEnum, string> ReadDeclaredDescriptions()
        {
            var descriptionsByValue = new ConcurrentDictionary<TEnum, string>();
            var declaredMembers = typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var declaredMember in declaredMembers)
            {
                var declaredValue = (TEnum)declaredMember.GetValue(null)!;
                var descriptionAttribute = declaredMember.GetCustomAttribute<DescriptionAttribute>();
                var description = descriptionAttribute is not null
                    && !string.IsNullOrWhiteSpace(descriptionAttribute.Description)
                        ? descriptionAttribute.Description
                        : declaredValue.ToString();

                // TryAdd, not the indexer: when several members share one numeric value, the first
                // declared member wins, which is the name ToString() would have returned anyway.
                descriptionsByValue.TryAdd(declaredValue, description);
            }

            return descriptionsByValue;
        }
    }
}

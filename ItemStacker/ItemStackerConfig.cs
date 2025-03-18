using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ItemStacker;

[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "The class is used during json deserialization")]
internal sealed class ItemStackerConfig {
	internal const byte DefaultStackLimiterDelay = 1;

	[JsonInclude]
	internal byte StackLimiterDelay { get; private init; } = DefaultStackLimiterDelay;

	[JsonConstructor]
	private ItemStackerConfig() { }
}

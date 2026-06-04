using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam;
using ConsoleTables;
using PluginLocale = ItemStacker.Localization;
using SteamKit2.Internal;
using SteamKit2;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace ItemStacker.Handlers;

internal static class StackHandler {
	private static byte StackLimiterDelay => ItemStackerPlugin.Config?.StackLimiterDelay ?? ItemStackerConfig.DefaultStackLimiterDelay;
	private static readonly SemaphoreSlim StackSemaphore = new(1, 1);
	internal static ConcurrentDictionary<string, StackStatus> BotStatuses { get; } = new();

	internal sealed record StackStatus(string BotName, uint AppID, uint Progress, uint Total, bool IsUnstack) {
		internal string ToTable() {
			ConsoleTable statusTable = new ConsoleTable("Bot", "Type", "AppID", "Progress")
				.Configure(o => o.EnableCount = false);

			_ = statusTable.AddRow(
					BotName,
					IsUnstack ? "Unstack" : "Stack",
					AppID,
					$"{Progress}/{Total}"
					);

			return statusTable.ToString();
		}
	}

	internal static string GetStatusTable() {
		if (BotStatuses.IsEmpty) {
			return PluginLocale.Strings.BotNoStackRun;
		}

		ConsoleTable statusTable = new ConsoleTable("Bot", "Type", "AppID", "Progress")
			.Configure(o => o.EnableCount = false);

		foreach (StackStatus status in BotStatuses.Values) {
			_ = statusTable.AddRow(
				status.BotName,
				status.IsUnstack ? "Unstack" : "Stack",
				status.AppID,
				$"{status.Progress}/{status.Total}"
			);
		}

		return string.Join(Environment.NewLine, Strings.Success, statusTable.ToString());
	}

	internal static async Task<string> StackInventory(Bot bot, uint appID, ulong contextID, Func<Asset, bool>? filterFunction = null) {
		ArgumentNullException.ThrowIfNull(bot);

		InventoryHandler? inventoryHandler = bot.GetHandler<InventoryHandler>();

		if (inventoryHandler == null) {
			throw new InvalidOperationException(nameof(inventoryHandler));
		}

		BotStatuses[bot.BotName] = new StackStatus(bot.BotName, appID, 0, 0, false);

		await StackSemaphore.WaitAsync().ConfigureAwait(false);

		try {

			filterFunction ??= static _ => true;

			HashSet<Asset> inventory = [];

			try {
				inventory = await bot.ArchiHandler.GetMyInventoryAsync(appID, contextID).Where(item => filterFunction(item)).ToHashSetAsync().ConfigureAwait(false);
			} catch (TimeoutException e) {
				bot.ArchiLogger.LogGenericWarningException(e);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
			}

			if (inventory.Count == 0) {
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(inventory));
			}

			HashSet<IGrouping<ulong, Asset>> assetGroups = [.. inventory.GroupBy(asset => asset.ClassID).Where(assetGroup => assetGroup.Count() > 1)];

			if (assetGroups == null) {
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(assetGroups));
			}

			uint total = (uint) assetGroups.Sum(g => g.Count() - 1);
			uint stackCount = 0;
			uint progress = 0;

			foreach (IGrouping<ulong, Asset> assetGroup in assetGroups) {
				ulong mainAssetID = assetGroup.First().AssetID;

				foreach (Asset asset in assetGroup.Skip(1)) {
					SteamUnifiedMessages.ServiceMethodResponse<CInventory_Response>? response = await inventoryHandler.CombineItemStacks(appID, asset, mainAssetID, bot.SteamID).ConfigureAwait(false);

					if (response == null) {
						return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(inventory));
					}

					if (response.Result != EResult.OK) {
						return string.Format(CultureInfo.CurrentCulture, Strings.WarningFailedWithError, response.Result);
					}

					stackCount++;
					progress++;

					BotStatuses[bot.BotName] = BotStatuses[bot.BotName] with {
						Progress = progress,
						Total = total
					};

					await Task.Delay(StackLimiterDelay * 1000).ConfigureAwait(false);
				}
			}

			return PluginLocale.Strings.FormatBotDoneStacking(stackCount);
		} finally {
			_ = StackSemaphore.Release();
			BotStatuses.TryRemove(bot.BotName, out _);
		}
	}

	internal static async Task<string> UnstackInventory(Bot bot, uint appID, ulong contextID, Func<Asset, bool>? filterFunction = null) {
		ArgumentNullException.ThrowIfNull(bot);

		InventoryHandler? inventoryHandler = bot.GetHandler<InventoryHandler>();

		if (inventoryHandler == null) {
			throw new InvalidOperationException(nameof(inventoryHandler));
		}

		BotStatuses[bot.BotName] = new StackStatus(bot.BotName, appID, 0, 0, true);

		await StackSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			HashSet<Asset> inventory = [];

			try {
				inventory = await bot.ArchiHandler.GetMyInventoryAsync(appID, contextID).Where(item => (filterFunction == null || filterFunction(item)) && item.Amount > 1).ToHashSetAsync().ConfigureAwait(false);
			} catch (TimeoutException e) {
				bot.ArchiLogger.LogGenericWarningException(e);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
			}

			if (inventory.Count == 0) {
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(inventory));
			}

			uint total = (uint) inventory.Count;
			uint unstackCount = 0;
			uint progress = 0;

			foreach (Asset asset in inventory) {
				SteamUnifiedMessages.ServiceMethodResponse<CInventory_Response>? response = await inventoryHandler.SplitItemStack(appID, asset.AssetID, 1, bot.SteamID).ConfigureAwait(false);

				if (response == null) {
					return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(inventory));
				}

				if (response.Result != EResult.OK) {
					return string.Format(CultureInfo.CurrentCulture, Strings.WarningFailedWithError, response.Result);
				}

				unstackCount++;
				progress++;

				BotStatuses[bot.BotName] = BotStatuses[bot.BotName] with {
					Progress = progress,
					Total = total
				};

				await Task.Delay(StackLimiterDelay * 1000).ConfigureAwait(false);
			}

			return PluginLocale.Strings.FormatBotDoneUnstacking(unstackCount);
		} finally {
			_ = StackSemaphore.Release();
			BotStatuses.TryRemove(bot.BotName, out _);
		}
	}

	internal static async Task<string> SplitItems(Bot bot, HashSet<ulong> itemIDs, uint quantity, uint appID, ulong contextID) {
		ArgumentNullException.ThrowIfNull(bot);

		InventoryHandler? inventoryHandler = bot.GetHandler<InventoryHandler>();

		if (inventoryHandler == null) {
			throw new InvalidOperationException(nameof(inventoryHandler));
		}

		await StackSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			HashSet<Asset> inventory = [];

			try {
				inventory = await bot.ArchiHandler.GetMyInventoryAsync(appID, contextID).Where(asset => itemIDs.Contains(asset.AssetID) && asset.Amount > 1).ToHashSetAsync().ConfigureAwait(false);
			} catch (TimeoutException e) {
				bot.ArchiLogger.LogGenericWarningException(e);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
			}

			if (inventory.Count == 0) {
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(inventory));
			}

			uint unstackCount = 0;

			foreach (Asset asset in inventory) {
				if (quantity > asset.Amount) {
					return string.Format(CultureInfo.CurrentCulture, Strings.WarningFailedWithError, $"{nameof(quantity)} > {nameof(asset.Amount)}");
				}

				SteamUnifiedMessages.ServiceMethodResponse<CInventory_Response>? response = await inventoryHandler.SplitItemStack(appID, asset.AssetID, quantity, bot.SteamID).ConfigureAwait(false);

				if (response == null) {
					return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(inventory));
				}

				if (response.Result != EResult.OK) {
					return string.Format(CultureInfo.CurrentCulture, Strings.WarningFailedWithError, response.Result);
				}

				unstackCount++;

				await Task.Delay(StackLimiterDelay * 1000).ConfigureAwait(false);
			}

			return PluginLocale.Strings.FormatBotDoneUnstacking(unstackCount);
		} finally {
			_ = StackSemaphore.Release();
		}
	}
}

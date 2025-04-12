using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam;
using PluginLocale = ItemStacker.Localization;
using SteamKit2;
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

	internal static async Task<string> StackInventory(Bot bot, uint appID, ulong contextID, Func<Asset, bool>? filterFunction = null) {
		ArgumentNullException.ThrowIfNull(bot);

		InventoryHandler? inventoryHandler = bot.GetHandler<InventoryHandler>();

		if (inventoryHandler == null) {
			throw new InvalidOperationException(nameof(inventoryHandler));
		}

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

			var assetGroups = inventory.GroupBy(asset => asset.ClassID).Where(assetGroup => assetGroup.Count() > 1).ToHashSet();

			if (assetGroups == null) {
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(assetGroups));
			}

			uint itemsCount = 0;

			foreach (var assetGroup in assetGroups) {
				ulong mainAssetID = assetGroup.First().AssetID;

				foreach (var asset in assetGroup.Skip(1)) {
					var response = await inventoryHandler.CombineItemStacks(appID, asset, mainAssetID, bot.SteamID).ConfigureAwait(false);

					if (response == null) {
						return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(inventory));
					}

					if (response.Result != EResult.OK) {
						return string.Format(CultureInfo.CurrentCulture, Strings.WarningFailedWithError, response.Result);
					}

					itemsCount++;

					await Task.Delay(StackLimiterDelay * 1000).ConfigureAwait(false);
				}
			}

			return PluginLocale.Strings.FormatBotDoneStacking(itemsCount);
		} finally {
			StackSemaphore.Release();
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

			foreach (var asset in inventory) {
				if (quantity > asset.Amount) {
					return string.Format(CultureInfo.CurrentCulture, Strings.WarningFailedWithError, $"{nameof(quantity)} > {nameof(asset.Amount)}");
				}

				var response = await inventoryHandler.SplitItemStack(appID, asset.AssetID, quantity, bot.SteamID).ConfigureAwait(false);

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
		StackSemaphore.Release();
	}
	}
}

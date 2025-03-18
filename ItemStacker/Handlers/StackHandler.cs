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

	internal static async Task<string> StackInventoryItems(Bot bot, uint appID, ulong contextID) {
		ArgumentNullException.ThrowIfNull(bot);

		InventoryHandler? inventoryHandler = bot.GetHandler<InventoryHandler>();

		if (inventoryHandler == null) {
			throw new InvalidOperationException(nameof(inventoryHandler));
		}

		await StackSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			HashSet<Asset> inventory = [];

			try {
				inventory = await bot.ArchiHandler.GetMyInventoryAsync(appID, contextID).ToHashSetAsync().ConfigureAwait(false);
			} catch (TimeoutException e) {
				bot.ArchiLogger.LogGenericWarningException(e);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
			}

			if (inventory.Count == 0) {
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(inventory));
			}

			var filteredInventory = inventory.GroupBy(asset => asset.ClassID).Where(assetGroup => assetGroup.Count() > 1).ToHashSet();

			if (filteredInventory == null) {
				return string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(filteredInventory));
			}

			uint itemsCount = 0;

			foreach (var assetGroup in filteredInventory) {
				ulong destItemID = assetGroup.First().AssetID;

				foreach (var asset in assetGroup.Skip(1)) {
					var response = await inventoryHandler.CombineItemStacks(appID, asset, destItemID, bot.SteamID).ConfigureAwait(false);

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
}

using ArchiSteamFarm.Core;
using ArchiSteamFarm.NLog;
using ArchiSteamFarm.Steam.Data;
using SteamKit2.Internal;
using SteamKit2;
using System.Threading.Tasks;
using System;

namespace ItemStacker.Handlers;

internal sealed class InventoryHandler : ClientMsgHandler {
	private readonly ArchiLogger ArchiLogger;
	private readonly Inventory UnifiedInventoryService;

	internal InventoryHandler(ArchiLogger archiLogger, SteamUnifiedMessages steamUnifiedMessages) {
		ArgumentNullException.ThrowIfNull(archiLogger);
		ArgumentNullException.ThrowIfNull(steamUnifiedMessages);

		ArchiLogger = archiLogger;
		UnifiedInventoryService = steamUnifiedMessages.CreateService<Inventory>();
	}

	public override void HandleMsg(IPacketMsg packetMsg) => ArgumentNullException.ThrowIfNull(packetMsg);

	internal async Task<SteamUnifiedMessages.ServiceMethodResponse<CInventory_Response>?> CombineItemStacks(uint appID, Asset asset, ulong destItemID, ulong steamID) {
		if (Client == null) {
			throw new InvalidOperationException(nameof(Client));
		}

		if (!Client.IsConnected) {
			return null;
		}

		CInventory_CombineItemStacks_Request request = new() {
			appid = appID,
			fromitemid = asset.AssetID,
			destitemid = destItemID,
			quantity = asset.Amount,
			steamid = steamID
		};

		SteamUnifiedMessages.ServiceMethodResponse<CInventory_Response> response;

		try {
			response = await UnifiedInventoryService.CombineItemStacks(request).ToLongRunningTask().ConfigureAwait(false);
		} catch (Exception e) {
			ArchiLogger.LogGenericWarningException(e);

			return null;
		}

		return response;
	}

	internal async Task<SteamUnifiedMessages.ServiceMethodResponse<CInventory_Response>?> SplitItemStack(uint appID, ulong itemID, uint quantity, ulong steamID) {
		if (Client == null) {
			throw new InvalidOperationException(nameof(Client));
		}

		if (!Client.IsConnected) {
			return null;
		}

		CInventory_SplitItemStack_Request request = new() {
			appid = appID,
			itemid = itemID,
			quantity = quantity,
			steamid = steamID
		};

		SteamUnifiedMessages.ServiceMethodResponse<CInventory_Response> response;

		try {
			response = await UnifiedInventoryService.SplitItemStack(request).ToLongRunningTask().ConfigureAwait(false);
		} catch (Exception e) {
			ArchiLogger.LogGenericWarningException(e);

			return null;
		}

		return response;
	}
}

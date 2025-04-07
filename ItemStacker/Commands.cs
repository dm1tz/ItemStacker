using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm;
using Interaction = ArchiSteamFarm.Steam.Interaction;
using ItemStacker.Handlers;
using SteamKit2;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System;
using ArchiSteamFarm.Steam.Data;

namespace ItemStacker;

internal static class Commands {
	internal static async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		switch (args.Length) {
			case 1:
				switch (args[0].ToUpperInvariant()) {
					case "ITEMSTACKERVERSION" or "ISVERSION" or "ISV":
						return ResponseVersion(access);
					default:
						return null;
				}
			default:
				switch (args[0].ToUpperInvariant()) {
					case "STACKITEMS" or "STI" when args.Length > 3:
						return await ResponseStackItems(access, args[1], args[2], Utilities.GetArgsAsText(message, 3), steamID).ConfigureAwait(false);
					case "STACKITEMS" or "STI" when args.Length > 2:
						return await ResponseStackItems(access, bot, args[1], args[2]).ConfigureAwait(false);
					case "STACKITEMS&" or "STI&" when args.Length > 3:
						return await ResponseStackItemsByAssetRarity(access, bot, args[1], args[2], Utilities.GetArgsAsText(args, 3, ",")).ConfigureAwait(false);
					case "STACKITEMS&" or "STI&" when args.Length > 3:
						return await ResponseStackItemsByAssetRarity(access, args[1], args[2], args[3], Utilities.GetArgsAsText(args, 4, ",")).ConfigureAwait(false);
					case "SPLITITEMS" or "SPI" when args.Length > 5:
							return await ResponseSplitItems(access, args[1], args[2], args[3], args[4], args[5]).ConfigureAwait(false);
					case "SPLITITEMS" or "SPI" when args.Length > 4:
								return await ResponseSplitItems(access, bot, args[1], args[2], args[3], args[4]).ConfigureAwait(false);
					default:
						return null;
				}
		}
	}

	private static HashSet<EAssetRarity>? ParseAssetRarities(string assetRaritiesText) {
		ArgumentException.ThrowIfNullOrEmpty(assetRaritiesText);

		string[] assetRaritiesArgs = assetRaritiesText.Split(SharedInfo.ListElementSeparators, StringSplitOptions.RemoveEmptyEntries);

		HashSet<EAssetRarity> assetRarities = [];

		foreach (string assetRarityArg in assetRaritiesArgs) {
			if (!Enum.TryParse(assetRarityArg, true, out EAssetRarity assetRarity) || !Enum.IsDefined(assetRarity)) {
				return null;
			}

			assetRarities.Add(assetRarity);
		}

		return assetRarities;
	}

	private async static Task<string?> ResponseStackItems(EAccess access, Bot bot, string targetAppID, string targetContextID) {
		ArgumentException.ThrowIfNullOrEmpty(targetAppID);
		ArgumentException.ThrowIfNullOrEmpty(targetContextID);

		if (access < EAccess.Master) {
			return null;
		}

		if (!uint.TryParse(targetAppID, out uint appID) || (appID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, (nameof(appID))));
		}

		if (!ulong.TryParse(targetContextID, out ulong contextID) || (contextID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, nameof(contextID)));
		}

		string result = await StackHandler.StackItems(bot, appID, contextID).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseStackItems(EAccess access, string botNames, string appID, string contextID, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);
		ArgumentException.ThrowIfNullOrEmpty(appID);
		ArgumentException.ThrowIfNullOrEmpty(contextID);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseStackItems(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, appID, contextID)))).ConfigureAwait(false);

		List<string> responses = [..results.Where(static result => !string.IsNullOrEmpty(result))!];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private async static Task<string?> ResponseStackItemsByAssetRarity(EAccess access, Bot bot, string targetAppID, string targetContextID, string assetRaritiesText) {
		ArgumentException.ThrowIfNullOrEmpty(targetAppID);
		ArgumentException.ThrowIfNullOrEmpty(targetContextID);

		if (access < EAccess.Master) {
			return null;
		}

		if (!uint.TryParse(targetAppID, out uint appID) || (appID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, (nameof(appID))));
		}

		if (!ulong.TryParse(targetContextID, out ulong contextID) || (contextID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, nameof(contextID)));
		}

		HashSet<EAssetRarity>? assetRarities = ParseAssetRarities(assetRaritiesText);

		if ((assetRarities == null) || (assetRarities.Count == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(assetRarities)));
		}

		string result = await StackHandler.StackItems(bot, appID, contextID, item => assetRarities.Contains(item.Rarity)).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseStackItemsByAssetRarity(EAccess access, string botNames, string appID, string contextID, string assetRaritiesText, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);
		ArgumentException.ThrowIfNullOrEmpty(appID);
		ArgumentException.ThrowIfNullOrEmpty(contextID);
		ArgumentException.ThrowIfNullOrEmpty(assetRaritiesText);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseStackItemsByAssetRarity(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, appID, contextID, assetRaritiesText)))).ConfigureAwait(false);

		List<string> responses = [..results.Where(static result => !string.IsNullOrEmpty(result))!];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private async static Task<string?> ResponseSplitItems(EAccess access, Bot bot, string targetItemIDs, string targetQuantity, string targetAppID, string targetContextID) {
		ArgumentException.ThrowIfNullOrEmpty(targetAppID);
		ArgumentException.ThrowIfNullOrEmpty(targetContextID);

		if (access < EAccess.Master) {
			return null;
		}

		string[] targets = targetItemIDs.Split(SharedInfo.ListElementSeparators, StringSplitOptions.RemoveEmptyEntries);

		if (targets.Length == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, (nameof(targets))));
		}

		HashSet<ulong> itemIDs = [];

		foreach (string target in targets) {
			if (!ulong.TryParse(target, out ulong itemID) || (itemID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, (nameof(targets))));
			}

			itemIDs.Add(itemID);
		}

		if (!uint.TryParse(targetQuantity, out uint quantity) || (quantity == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, (nameof(quantity))));
		}

		if (!uint.TryParse(targetAppID, out uint appID) || (appID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, (nameof(appID))));
		}

		if (!ulong.TryParse(targetContextID, out ulong contextID) || (contextID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, nameof(contextID)));
		}

		string result = await StackHandler.SplitItems(bot, itemIDs, quantity, appID, contextID).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseSplitItems(EAccess access, string botNames, string itemIDs, string quantity, string appID, string contextID, ulong steamID = 0) {
		ArgumentException.ThrowIfNullOrEmpty(botNames);
		ArgumentException.ThrowIfNullOrEmpty(appID);
		ArgumentException.ThrowIfNullOrEmpty(contextID);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseSplitItems(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, itemIDs, quantity, appID, contextID)))).ConfigureAwait(false);

		List<string> responses = [..results.Where(static result => !string.IsNullOrEmpty(result))!];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}


	private static string? ResponseVersion(EAccess access) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		return Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotVersion, nameof(ItemStackerPlugin), typeof(ItemStackerPlugin).Assembly.GetName().Version));
	}
}

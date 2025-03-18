using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using Interaction = ArchiSteamFarm.Steam.Interaction;
using ItemStacker.Handlers;
using SteamKit2;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace ItemStacker;

[Export(typeof(IPlugin))]
internal sealed class ItemStackerPlugin : IBotCommand2, IBotSteamClient {
	[JsonInclude]
	[Required]
	public string Name => nameof(ItemStackerPlugin);

	[JsonInclude]
	[Required]
	public Version Version => typeof(ItemStackerPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	internal static ItemStackerConfig? Config { get; private set; }

	public Task OnLoaded() => Task.CompletedTask;

	public Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
		if (additionalConfigProperties == null) {
			return Task.CompletedTask;
		}

		ItemStackerConfig? config = null;

		foreach ((string configProperty, JsonElement configValue) in additionalConfigProperties) {
			try {
				if (configProperty == nameof(ItemStackerPlugin)) {
					config = configValue.ToJsonObject<ItemStackerConfig>();
				}
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);

				return Task.CompletedTask;
			}
		}

		Config = config;

		return Task.CompletedTask;
	}

	public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) {
		ArgumentNullException.ThrowIfNull(bot);
		ArgumentNullException.ThrowIfNull(callbackManager);

		return Task.CompletedTask;
	}

	public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot) {
		ArgumentNullException.ThrowIfNull(bot);

		SteamUnifiedMessages? steamUnifiedMessages = bot.GetHandler<SteamUnifiedMessages>();

		if (steamUnifiedMessages == null) {
			throw new InvalidOperationException(nameof(steamUnifiedMessages));
		}

		return Task.FromResult<IReadOnlyCollection<ClientMsgHandler>?>(new HashSet<ClientMsgHandler>(1) {
				new InventoryHandler(bot.ArchiLogger, steamUnifiedMessages) });
	}

	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		ArgumentNullException.ThrowIfNull(bot);

		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(message);

		if ((args == null) || (args.Length == 0)) {
			throw new ArgumentNullException(nameof(args));
		}

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		return args[0].ToUpperInvariant() switch {
			"STACKINVENTORYITEMS" or "SII" when args.Length > 3 => await ResponseStackInventoryItems(access, args[1], args[2], Utilities.GetArgsAsText(message, 3), steamID).ConfigureAwait(false),
			"STACKINVENTORYITEMS" or "SII" when args.Length > 2 => await ResponseStackInventoryItems(access, bot, args[1], args[2]).ConfigureAwait(false),
			_ => null
		};
	}

	private async static Task<string?> ResponseStackInventoryItems(EAccess access, Bot bot, string targetAppID, string targetContextID) {
		ArgumentException.ThrowIfNullOrEmpty(targetAppID);
		ArgumentException.ThrowIfNullOrEmpty(targetContextID);

		if (access < EAccess.Master) {
			return null;
		}

		if (!uint.TryParse(targetAppID, out uint appID) || (appID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, (nameof(appID))));
		}

		if (!ulong.TryParse(targetContextID, out ulong contextID) || (contextID == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(contextID)));
		}

		string result = await StackHandler.StackInventoryItems(bot, appID, contextID).ConfigureAwait(false);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseStackInventoryItems(EAccess access, string botNames, string appID, string contextID, ulong steamID = 0) {
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

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseStackInventoryItems(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, appID, contextID)))).ConfigureAwait(false);

		List<string> responses = [..results.Where(static result => !string.IsNullOrEmpty(result))!];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
}

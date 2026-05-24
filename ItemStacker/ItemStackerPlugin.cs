using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ItemStacker.Handlers;
using SteamKit2;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Composition;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace ItemStacker;

[Export(typeof(IPlugin))]
internal sealed class ItemStackerPlugin : IBotCommand2, IBotSteamClient, IGitHubPluginUpdates {
	public string Name => nameof(ItemStackerPlugin);
	public string RepositoryName => "dm1tz/ItemStacker";
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

		return await Commands.OnBotCommand(bot, access, message, args, steamID).ConfigureAwait(false);
	}
}

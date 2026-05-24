## Installation
- Download the latest `ItemStacker.zip` archive from the [release page](https://github.com/dm1tz/ItemStacker/releases/latest).
- Extract archive contents into the ASF `plugins` directory.

## Configuration

ItemStacker configuration is appended to `ASF.json` and has the following structure:
```json
{
	...
	"ItemStackerPlugin": {
		"StackLimiterDelay": 1,
	}
}
```

All options are explained below:

### `StackLimiterDelay`
`byte` type with the default value of `1`. This property defines, in seconds, the minimum delay between each stack/unstack request.

## Commands

Command | Alias | Access | Description
--- | --- | --- | ---
`stackinventory [Bots] <AppID> <ContextID>` | `sti` | `Master` | Stacks all stackable items in the inventory of the given bot instances for the specified game.
`stackinventory& [Bots] <AppID> <ContextID> <Rarities>` | `sti&` | `Master` | Stacks items of the specified rarities in the inventory of the given bot instances.
`unstackinventory [Bots] <AppID> <ContextID>` | `usti` | `Master` | Unstacks all stacked items in the inventory of the given bot instances.
`unstackinventory& [Bots] <AppID> <ContextID> <Rarities>` | `usti&` | `Master` | Unstacks stacked items of the specified rarities in the inventory of the given bot instances.
`splititems [Bots] <ItemIDs> <Quantity> <AppID> <ContextID>` | `spi` | `Master` | Splits the given item stacks by the specified quantity on the given bot instances.
`stackstatus` | `stst` | `FamilySharing` | Prints the current stack or unstack operation status, including type, progress, and items processed.
`isversion` | `isv` | `FamilySharing` | Prints the actual version of plugin.

## Installation
- Download latest `ItemStacker.zip` archive from [release page](https://github.com/dm1tz/ItemStacker/releases/latest).
- Extract archive contents into ASF `plugins` directory.

## Configuration

ItemStacker configuration is appended to `ASF.json` and has following structure:
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
`byte` type with default value of `1`. This property defines, in seconds, the minimum delay between each stack/unstack request.

## Commands

Command | Alias | Access | Description
--- | --- | --- | ---
`stackinventory [Bots] <AppID> <ContextID>` | `sti` | `Master` | Stacks all stackable items in inventory of given bot instances for specified `AppID`.
`stackinventory& [Bots] <AppID> <ContextID> <Rarities>` | `sti&` | `Master` | Stacks items of specified rarities in inventory of given bot instances for specified `AppID`.
`stackitem [Bots] <AppID> <ContextID> <ClassIDs>` | `stit` | `Master` | Stacks all stackable items of specified `ClassIDs` in inventory of given bot instances for specified `AppID`.
`stackitem* [Bots] <AppID> <ContextID> <AssetNames>` | `stit` | `Master` | Stacks all stackable items of specified names in inventory of given bot instances for specified `AppID`. **Note**: item's name may be localized.
`unstackinventory [Bots] <AppID> <ContextID>` | `usti` | `Master` | Unstacks all stacked items in inventory of given bot instances for specified `AppID`.
`unstackinventory& [Bots] <AppID> <ContextID> <Rarities>` | `usti&` | `Master` | Unstacks all stacked items of specified rarities in inventory of given bot instances.
`unstackitem [Bots] <AppID> <ContextID> <ClassIDs>` | `ustit` | `Master` | Unstacks all stacked items of specified `ClassIDs` in inventory of given bot instances for specified `AppID`.
`unstackitem* [Bots] <AppID> <ContextID> <AssetNames>` | `ustit` | `Master` | Unstacks all stacked items of specified names in inventory of given bot instances for specified `AppID`. **Note**: item's name may be localized.
`stackstatus` | `stst` | `FamilySharing` | Prints current stack/unstack operation status table.
`isversion` | `isv` | `FamilySharing` | Prints plugin version.

## Features

**Vehicle Decay Protection** allows you to scale or nullify vehicle decay damage multiple ways.

- Scale decay damage when a vehicle is under a roof (does not use permission)
- Scale decay damage when a vehicle is near a tool cupboard (does not use permission)
- Nullify decay damage for a configurable amount of time after a vehicle has been used (does not use permission)
- Nullify decay damage if a vehicle is owned by (or had a lock deployed onto it by) a player with permission

## No-Plugin Alternative

Vehicle decay is already somewhat configurable in the vanilla game using the following convars.

Minicopters and Scrap Transport Helicopters:
- `minicopter.insidedecayminutes`
- `minicopter.outsidedecayminutes`

Row Boats, RHIBs, and Kayaks:
- `motorrowboat.outsidedecayminutes`
- `motorrowboat.deepwaterdecayminutes`

Duo and Solo Submarines:
- `basesubmarine.outsidedecayminutes`
- `basesubmarine.deepwaterdecayminutes`

Snowmobile and Tomaha:
- `snowmobile.outsidedecayminutes`

Other:
- `baseridableanimal.decayminutes`
- `hotairballoon.outsidedecayminutes`
- `modularcar.outsidedecayminutes`

If you want to simply disable decay for vehicles, then you can just set these to really high (i.e., `1000000`) and you don't need a plugin.

Some vehicles also have an internal multiplier that affects decay damage while under a roof.
- Horses: `2.0x` decay damage while under a roof
- Modular Cars: `0.1x` decay damage while under a roof
- All Boats and Submarines: no decay damage while under a roof
  - In vanilla, they also have to be in shallow water, but this plugin removes that restriction
- Hot Air Balloons: no decay damage while under a roof
- Snowmobiles and Tomahas: no decay damage while under a roof

## Permissions

*Note: This plugin has four different features for protecting vehicles against decay. Only one of those features uses permissions: "no decay based on vehicle ownership". If you simply want to make vehicles not decay if they have been recently used, or if you want to scale decay for vehicles under a roof or near a tool cupboard, those features DO NOT use permissions, so skip ahead to the configuration section of the plugin.*

Granting the following permissions to a player will cause their **owned** vehicles to not decay under any circumstances. You can grant permissions by vehicle type, or for all vehicles with a single permission.

- `vehicledecayprotection.nodecay.allvehicles` (all in one)
- `vehicledecayprotection.nodecay.duosubmarine`
- `vehicledecayprotection.nodecay.hotairballoon`
- `vehicledecayprotection.nodecay.kayak`
- `vehicledecayprotection.nodecay.minicopter`
- `vehicledecayprotection.nodecay.modularcar`
- `vehicledecayprotection.nodecay.rhib`
- `vehicledecayprotection.nodecay.ridablehorse`
- `vehicledecayprotection.nodecay.rowboat`
- `vehicledecayprotection.nodecay.scraptransporthelicopter`
- `vehicledecayprotection.nodecay.sled.xmas`
- `vehicledecayprotection.nodecay.sled`
- `vehicledecayprotection.nodecay.snowmobile`
- `vehicledecayprotection.nodecay.solosubmarine`
- `vehicledecayprotection.nodecay.tomaha`

**Note: Vehicles are never assigned an owner by the vanilla game, with the exception of deployable vehicles like Kayaks, so you will need another plugin to assign ownership to most vehicles in order to benefit from the `nodecay` permissions.**

### How vehicle ownership is determined

Vehicle ownership is determined by the `OwnerID` property of the vehicle, which is usually a player's Steam ID, or `0` for no owner. Most plugins that spawn vehicles for a player will assign that player as the owner. For vehicles spawned by the vanilla game, it's recommended to use one of the following plugins to grant vehicle ownership.

- [Vehicle Vendor Options](https://umod.org/plugins/vehicle-vendor-options) - Automatically assigns ownership of vehicles purchased at vanilla NPC vendors if the player has permission
- [Claim Vehicle](https://umod.org/plugins/claim-vehicle) - Allows players with permission to claim ownership of unowned vehicles using a command on cooldown
- [Vehicle Deployed Locks](https://umod.org/plugins/vehicle-deployed-locks) - Optionally assigns ownership to vehicles when a lock is deployed onto them

Alternatively, if the vehicle has a **code lock** or **key lock** attached to it via [Vehicle Deployed Locks](https://umod.org/plugins/vehicle-deployed-locks), this plugin will also check the `nodecay` permissions of the lock owner, if the lock is locked.

## Configuration

Default configuration (equivalent to vanilla):

```json
{
  "Enable permission": true,
  "Vehicles": {
    "Duo Submarine": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 45.0,
      "Decay interval (seconds)": 60.0
    },
    "Hot Air Balloon": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 10.0,
      "Decay interval (seconds)": 60.0
    },
    "Kayak": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 45.0,
      "Decay interval (seconds)": 60.0
    },
    "Minicopter": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 1.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 10.0,
      "Decay interval (seconds)": 60.0
    },
    "Modular Car": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.1,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 10.0,
      "Decay interval (seconds)": 60.0
    },
    "RHIB": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 45.0,
      "Decay interval (seconds)": 60.0
    },
    "Ridable Horse": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 2.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 10.0,
      "Decay interval (seconds)": 60.0
    },
    "Rowboat": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 45.0,
      "Decay interval (seconds)": 60.0
    },
    "Scrap Transport Helicopter": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 1.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 10.0,
      "Decay interval (seconds)": 60.0
    },
    "Sled": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 1.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Decay interval (seconds)": 60.0
    },
    "Sled Xmas": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 1.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Decay interval (seconds)": 60.0
    },
    "Snowmobile": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 45.0,
      "Decay interval (seconds)": 60.0
    },
    "Solo Submarine": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 45.0,
      "Decay interval (seconds)": 60.0
    },
    "Tomaha": {
      "Allow the plugin to influence decay": true,
      "Decay multiplier while inside": 0.0,
      "Decay multiplier near tool cupboard": 1.0,
      "Protect from decay after recent use (minutes)": 45.0,
      "Decay interval (seconds)": 60.0
    }
  }
}
```

- `Enable permission` (`true` or `false`) -- Determines whether the permission feature is enabled. If you are not using permissions to prevent vehicle decay, set this to `false` to improve performance.

Each vehicle type has the following options:

- `Allow the plugin to influence decay` (`true` or `false`) -- Determines whether the plugin will influence decay for this vehicle type. If you don't want to alter decay for a given vehicle type, you may set this to `false` to ensure that vanilla decay is used.
- `Decay multiplier while inside` -- Determines how much to scale decay damage for vehicles that are inside (under a roof). Set to `0.0` to completely nullify decay damage to vehicles while they are inside. Setting to `1.0` will improve performance by avoiding even checking if the vehicle is inside.
- `Decay multiplier near tool cupboard` -- Determines how much to scale decay damage for vehicles that are near **any** tool cupboard (regardless of whether the vehicle owner is authorized). Defaults to `1.0` which has no effect. Set to `0.0` to completely nullify decay damage near tool cupboards.
- `Protect from decay after recent use (minutes)` -- Determines how many minutes to protect vehicles from decay after they have been used.
- `Decay interval (seconds)` -- Determines how often each vehicle can take decay damage. Raise this value to deal decay damage less frequently and to improve performance. The plugin will automatically compensate for slower schedules by dealing higher amounts of decay damage, so you don't have to worry about this affecting damage over time.

## Performance tips

When carefully tuned, this plugin can actually improve the performance of decay calculation compared to vanilla. Here are some tips that can help you achieve that.

- Set `Enable permission` to `false` when not using the permissions feature, to avoid checking vehicle owner permissions.
- Raise `Protect from decay after recent use (minutes)` to increase the likelihood that other expensive checks are skipped.
- Set `Decay multiplier while inside` to `1.0` to skip checking whether the vehicle is inside.
- Set `Decay multiplier near tool cupboard` to `1.0` to skip building privilege checks, which are the most expensive type of check.
- Raise `Decay interval (seconds)` to reduce the frequency at which the various checks are performed. The default configuration of the plugin is `60.0` to match vanilla Rust, but `600.0` is recommended as a starting point.

## Developer Hooks

```cs
object OnVehicleDecayReplace(BaseEntity entity)
```

- Called when this plugin is about to replace decay logic for a vehicle.
- Returning `false` will prevent this plugin from replacing that vehicle's decay logic.
- Returning `null` will result in the default behavior.

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

Some vehicles also have an internal multiplier that reduces decay damage while under a roof.
- Horses: `0.5x` decay damage while under a roof
- Modular Cars: `0.1x` decay damage while under a roof
- All Boats and Submarines: no decay damage while under a roof
  - In vanilla, they also have to be in shallow water, but this plugin removes that restriction
- Hot Air Balloons: no decay damage while under a roof
- Snowmobiles and Tomahas: no decay damage while under a roof

## Permissions

*Note: This plugin has four different features for protecting vehicles against decay. Only one of those features uses permissions: no decay based on vehicle ownership. If you simply want to make vehicles not decay if they have been recently used, or if you want to scale decay for vehicles under a roof or near a tool cupboard, those features DO NOT use permissions, so skip ahead to the configuration section of the plugin.*

Granting the following permissions to a player will cause their **owned** vehicles to not decay under any circumstances. You can grant permissions by vehicle type, or for all vehicles with a single permission.

- `vehicledecayprotection.nodecay.allvehicles` (all in one)
- `vehicledecayprotection.nodecay.hotairballoon`
- `vehicledecayprotection.nodecay.duosubmarine`
- `vehicledecayprotection.nodecay.kayak`
- `vehicledecayprotection.nodecay.minicopter`
- `vehicledecayprotection.nodecay.modularcar`
- `vehicledecayprotection.nodecay.rhib`
- `vehicledecayprotection.nodecay.ridablehorse`
- `vehicledecayprotection.nodecay.rowboat`
- `vehicledecayprotection.nodecay.scraptransporthelicopter`
- `vehicledecayprotection.nodecay.snowmobile`
- `vehicledecayprotection.nodecay.solosubmarine`
- `vehicledecayprotection.nodecay.tomaha`

**Note: Vehicles are never assigned an owner by the vanilla game, with the exception of deployable vehicles like Kayaks, so you will need another plugin to assign ownership to most vehicles in order to benefit from the `nodecay` permissions.**

### How vehicle ownership is determined

Vehicle ownership is determined by the `OwnerID` property of the vehicle, which is usually a player's Steam ID, or `0` for no owner. Most plugins that spawn vehicles for a player will assign that player as the owner. For vehicles spawned by the vanilla game, it's recommended to use one of the following plugins to grant vehicle ownership.

- [Vehicle Vendor Options](https://umod.org/plugins/vehicle-vendor-options) - Automatically assigns ownership of vehicles purchased at vanilla NPC vendors if the player has permission
- [Claim Vehicle Ownership](https://umod.org/plugins/claim-vehicle-ownership) - Allows players with permission to claim ownership of unowned vehicles using a command on cooldown

Alternatively, if the vehicle has a **code lock** or **key lock** attached to it via [Vehicle Deployed Locks](https://umod.org/plugins/vehicle-deployed-locks), this plugin will also check the `nodecay` permissions of the lock owner, if the lock is locked.

## Configuration

Default configuration (equivalent to vanilla):

```json
{
  "EnablePermission": true,
  "Vehicles": {
    "DuoSubmarine": {
      "DecayMultiplierInside": 0.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0,
      "DecayIntervalSeconds": 60.0
    },
    "HotAirBalloon": {
      "DecayMultiplierInside": 0.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0,
      "DecayIntervalSeconds": 60.0
    },
    "Kayak": {
      "DecayMultiplierInside": 0.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0,
      "DecayIntervalSeconds": 60.0
    },
    "Minicopter": {
      "DecayMultiplierInside": 1.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0,
      "DecayIntervalSeconds": 60.0
    },
    "ModularCar": {
      "DecayMultiplierInside": 0.1,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0,
      "DecayIntervalSeconds": 60.0
    },
    "RHIB": {
      "DecayMultiplierInside": 0.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0,
      "DecayIntervalSeconds": 60.0
    },
    "RidableHorse": {
      "DecayMultiplierInside": 0.5,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0,
      "DecayIntervalSeconds": 60.0
    },
    "Rowboat": {
      "DecayMultiplierInside": 0.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0,
      "DecayIntervalSeconds": 60.0
    },
    "ScrapTransportHelicopter": {
      "DecayMultiplierInside": 1.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0,
      "DecayIntervalSeconds": 60.0
    },
    "Snowmobile": {
      "DecayMultiplierInside": 0.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0,
      "DecayIntervalSeconds": 60.0
    },
    "SoloSubmarine": {
      "DecayMultiplierInside": 0.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0,
      "DecayIntervalSeconds": 60.0
    },
    "Tomaha": {
      "DecayMultiplierInside": 0.0,
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0,
      "DecayIntervalSeconds": 60.0
    }
  }
}
```

- `EnablePermission` (`true` or `false`) -- Determines whether the permission feature is enabled. If you are not using permissions to prevent vehicle decay, set this to `false` to improve performance.

Each vehicle type has the following options:
- `DecayMultiplierInside` -- Determines how much to scale decay damage for vehicles that are inside (under a roof). Set to `0.0` to completely nullify decay damage to vehicles while they are inside. Setting to `1.0` will improve performance by avoiding even checking if the vehicle is inside.
- `DecayMultiplierNearTC` -- Determines how much to scale decay damage for vehicles that are near any tool cupboard. Defaults to `1.0` which has no effect. Set to `0.0` to completely nullify decay damage.
- `ProtectionMinutesAfterUse` -- Determines how many minutes to protect vehilces from decay after they have been used.
- `DecayIntervalSeconds` -- Determines how often each vehicle can take decay damage. Raise this value to deal decay damage less frequently and to improve performance.

## Performance tips

When carefully tuned, this plugin can actually improve the performance of decay calculation compared to vanilla. Here are some tips that can help you achieve that.

- Set `EnablePermission` to `false` when not using the permissions feature, to avoid checking vehicle owner permissions.
- Raise `ProtectionMinutesAfterUse` to increase the likelihood that other expensive checks are skipped.
- Set `DecayMultiplierInside` to `1.0` to skip checking whether the vehicle is inside.
- Set `DecayMultiplierNearTC` to `1.0` to skip building privilege checks, which are the most expensive type of check.
- Raising `DecayIntervalSeconds` to reduce the frequency at which the various checks are performed. The default configuration of the plugin is `60.0` to match vanilla Rust, but `300.0` is recommended as a starting point.

## Features

**Vehicle Decay Protection** allows you to scale or nullify vehicle decay damage multiple ways.

- Scale decay damage when a vehicle is near a tool cupboard
- Nullify decay damage for a configurable amount of time after a vehicle has been used
- Nullify decay damage if a vehicle is owned by a player with permission

## No-Plugin Alternative

Vehicle decay is already somewhat configurable in the vanilla game using the following server variables.
- `minicopter.outsidedecayminutes` -- Affects Minicopters and Scrap Transport Helicopters
- `modularcar.outsidedecayminutes` -- Affects Modular Cars
- `motorrowboat.outsidedecayminutes` -- Affects Row Boats, RHIBs, and Kayaks
- `hotairballoon.outsidedecayminutes` -- Affects Hot Air Balloons
- `baseridableanimal.decayminutes` -- Affects Ridable Horses

If you want to simply disable decay for vehicles, then you can just set these to really high (i.e., `1000000`) and you don't need a plugin. Most vehicles also have an internal multiplier (usually `0.1`) that reduces decay damage while inside a building (i.e., under a roof).

If you want to selectively reduce or nullify decay damage only for vehicles that are near a tool cupboard, for vehicles that have recently been used, or for vehicles owned by privileged players, then this plugin is for you.

## Permissions

Granting the following permissions to a player will cause their **owned** vehicles to not decay under any circumstances. You can grant permissions by vehicle type, or for all vehicles with a single permission.

- `vehicledecayprotection.nodecay.allvehicles` (all in one)
- `vehicledecayprotection.nodecay.hotairballoon`
- `vehicledecayprotection.nodecay.kayak`
- `vehicledecayprotection.nodecay.minicopter`
- `vehicledecayprotection.nodecay.modularcar`
- `vehicledecayprotection.nodecay.rhib`
- `vehicledecayprotection.nodecay.ridablehorse`
- `vehicledecayprotection.nodecay.rowboat`
- `vehicledecayprotection.nodecay.scraptransporthelicopter`

**Note: Except for Kayaks, vehicles are never assigned an owner by the vanilla game, so you will need another plugin to assign ownership to vehicles in order to benefit from the `nodecay` permissions.** Getting in a vehicle, driving it to your base, or locking it **do not** make you the owner.

Vehicle ownership is determined by the `OwnerID` property of the vehicle, which is usually a player's Steam ID, or `0` for no owner. Most plugins that spawn vehicles for a player will assign that player as the owner. For vehicles spawned by the vanilla game, it's recommended to use one of the following plugins to grant vehicle ownership.

- [Vehicle Vendor Options](https://umod.org/plugins/vehicle-vendor-options) - Automatically assigns ownership of vehicles purchased at vanilla NPC vendors if the player has permission
- [Claim Vehicle Ownership](https://umod.org/plugins/claim-vehicle-ownership) - Allows players with permission to claim ownership of unowned vehicles using a command on cooldown

## Configuration

Default configuration:
```json
{
  "Vehicles": {
    "HotAirBalloon": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0
    },
    "Kayak": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0
    },
    "Minicopter": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0
    },
    "ModularCar": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0
    },
    "RHIB": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0
    },
    "RidableHorse": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0
    },
    "Rowboat": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 45.0
    },
    "ScrapTransportHelicopter": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0
    }
  }
}
```

Each vehicle type has the following options:
- `DecayMultiplierNearTC` -- Used to scale decay damage taken by vehicles near a tool cupboard, regardless of the vehicle owner or upkeep. Defaults to `1` which has no effect. Set to `0` to completely nullify decay damage near a tool cupboard.
  - Note: This is a global setting per vehicle type that does not use any permissions.
- `ProtectionMinutesAfterUse` -- Used to prevent decay damage to vehicles this many minutes after they have been used (e.g., when their engine was last on). This mechanic already exists in vanilla Rust for most vehicles, but the protection window is usually just 10 minutes (45 for boats). You can change this option to protect vehicles for longer, but setting it to a lower value than default will have no effect for most vehicles.
  - Note: This is a global setting per vehicle type that does not use any permissions.

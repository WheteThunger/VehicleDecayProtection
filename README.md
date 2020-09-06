**Vehicle Decay Protection** protects vehicles from decay around tool cupboards and when recently used.

Vehicle decay is already somewhat configurable in the vanilla game using the following server variables.
- `minicopter.outsidedecayminutes` -- Affects Minicopters and Scrap Transport Helicopters
- `modularcar.outsidedecayminutes` -- Affects Modular Cars
- `motorrowboat.outsidedecayminutes` -- Affects Row Boats, RHIBs, and Kayaks
- `hotairballoon.outsidedecayminutes` -- Affects Hot Air Balloons

If you want to simply disable decay for vehicles, then you can just set these to really high (i.e., `1000000`) and you don't need a plugin. Most vehicles also have an internal multiplier (usually `0.1`) that reduces decay damage while inside a building (i.e., under a roof).

If you want to selectively reduce or nullify decay damage only for vehicles that are near a tool cupboard, or for vehicles that have recently been used, then this plugin is for you.

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
      "ProtectionMinutesAfterUse": 10.0
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
      "ProtectionMinutesAfterUse": 10.0
    },
    "Rowboat": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0
    },
    "ScrapTransportHelicopter": {
      "DecayMultiplierNearTC": 1.0,
      "ProtectionMinutesAfterUse": 10.0
    }
  }
}
```

- `DecayMultiplierNearTC` -- Used to scale decay damage taken by vehicles near a tool cupboard (regardless of the owner). Defaults to `1` which has no effect. Set to `0` to completely nullify decay damage near a tool cupboard.
- `ProtectionMinutesAfterUse` -- Used to prevent decay damage to vehicles this many minutes after they have been used (e.g., when their engine was last on). This mechanic already exists in vanilla Rust for most vehicles, but the protection window is usually just 10 minutes, so you can change this option to protect vehicles for longer. Note: Reducing this option below 10 minutes will have no effect for most vehicles.

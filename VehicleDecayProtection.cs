using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Decay Protection", "WhiteThunder", "1.6.0")]
    [Description("Protects vehicles from decay based on ownership and other factors.")]
    internal class VehicleDecayProtection : CovalencePlugin
    {
        #region Fields

        private Configuration _pluginConfig;

        private const string Permission_NoDecay_AllVehicles = "vehicledecayprotection.nodecay.allvehicles";
        private const string Permission_NoDecay_DuoSub = "vehicledecayprotection.nodecay.duosubmarine";
        private const string Permission_NoDecay_HotAirBalloon = "vehicledecayprotection.nodecay.hotairballoon";
        private const string Permission_NoDecay_Kayak = "vehicledecayprotection.nodecay.kayak";
        private const string Permission_NoDecay_MiniCopter = "vehicledecayprotection.nodecay.minicopter";
        private const string Permission_NoDecay_ModularCar = "vehicledecayprotection.nodecay.modularcar";
        private const string Permission_NoDecay_RHIB = "vehicledecayprotection.nodecay.rhib";
        private const string Permission_NoDecay_RidableHorse = "vehicledecayprotection.nodecay.ridablehorse";
        private const string Permission_NoDecay_Rowboat = "vehicledecayprotection.nodecay.rowboat";
        private const string Permission_NoDecay_ScrapHeli = "vehicledecayprotection.nodecay.scraptransporthelicopter";
        private const string Permission_NoDecay_Snowmobile = "vehicledecayprotection.nodecay.snowmobile";
        private const string Permission_NoDecay_SoloSub = "vehicledecayprotection.nodecay.solosubmarine";
        private const string Permission_NoDecay_Tomaha = "vehicledecayprotection.nodecay.tomaha";

        private const string SnowmobileShortPrefabName = "snowmobile";
        private const string TomahaShortPrefabName = "tomahasnowmobile";

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(Permission_NoDecay_AllVehicles, this);
            permission.RegisterPermission(Permission_NoDecay_DuoSub, this);
            permission.RegisterPermission(Permission_NoDecay_HotAirBalloon, this);
            permission.RegisterPermission(Permission_NoDecay_Kayak, this);
            permission.RegisterPermission(Permission_NoDecay_MiniCopter, this);
            permission.RegisterPermission(Permission_NoDecay_ModularCar, this);
            permission.RegisterPermission(Permission_NoDecay_RHIB, this);
            permission.RegisterPermission(Permission_NoDecay_RidableHorse, this);
            permission.RegisterPermission(Permission_NoDecay_Rowboat, this);
            permission.RegisterPermission(Permission_NoDecay_ScrapHeli, this);
            permission.RegisterPermission(Permission_NoDecay_Snowmobile, this);
            permission.RegisterPermission(Permission_NoDecay_SoloSub, this);
            permission.RegisterPermission(Permission_NoDecay_Tomaha, this);
        }

        // Using separate hooks to theoretically improve performance by reducing hook calls
        private object OnEntityTakeDamage(BaseVehicle entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        private object OnEntityTakeDamage(HotAirBalloon entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        private object OnEntityTakeDamage(BaseVehicleModule entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        #endregion

        #region Helper Methods

        private bool? ProcessDecayDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || !hitInfo.damageTypes.Has(Rust.DamageType.Decay))
                return null;

            float damageMultiplier = 1;

            VehicleConfig vehicleConfig;
            string vehicleSpecificNoDecayPerm;
            float timeSinceLastUsed;
            BaseCombatEntity vehicle;
            ulong lockOwnerId;

            if (!GetSupportedVehicleInformation(entity, out vehicleConfig, out vehicleSpecificNoDecayPerm, out timeSinceLastUsed, out vehicle))
                return null;

            if (timeSinceLastUsed != 0 && timeSinceLastUsed < 60 * vehicleConfig.ProtectionMinutesAfterUse)
            {
                if (_pluginConfig.Debug)
                    LogWarning($"{entity.ShortPrefabName}: Nullifying decay damage due to being recently used: {(int)timeSinceLastUsed}s < {60 * vehicleConfig.ProtectionMinutesAfterUse}s.");

                damageMultiplier = 0;
            }
            else if (UserHasPermission(vehicle.OwnerID, vehicleSpecificNoDecayPerm))
            {
                if (_pluginConfig.Debug)
                    LogWarning($"{entity.ShortPrefabName}: Nullifying decay damage due to owner permission. OwnerId: {vehicle.OwnerID}.");

                damageMultiplier = 0;
            }
            else if (LockOwnerHasPermission(vehicle, vehicleSpecificNoDecayPerm, out lockOwnerId))
            {
                if (_pluginConfig.Debug)
                    LogWarning($"{entity.ShortPrefabName}: Nullifying decay damage due to lock owner permission. OwnerId: {lockOwnerId}.");

                damageMultiplier = 0;
            }
            else
            {
                if (vehicleConfig.DecayMultiplierInside != 1.0 && !entity.IsOutside())
                {
                    if (_pluginConfig.Debug)
                        LogWarning($"{entity.ShortPrefabName}: Multiplying decay damage due to being inside: x{vehicleConfig.DecayMultiplierInside}.");

                    damageMultiplier = vehicleConfig.DecayMultiplierInside;
                }

                // Skip building privilege check if damage multiplier is already 0.
                if (damageMultiplier != 0 && vehicleConfig.DecayMultiplierNearTC != 1.0 && entity.GetBuildingPrivilege() != null)
                {
                    if (_pluginConfig.Debug)
                        LogWarning($"{entity.ShortPrefabName}: Multiplying decay damage due to being near TC: x{vehicleConfig.DecayMultiplierNearTC}.");

                    damageMultiplier *= vehicleConfig.DecayMultiplierNearTC;
                }
            }

            if (damageMultiplier != 1)
            {
                hitInfo.damageTypes.Scale(Rust.DamageType.Decay, damageMultiplier);

                // If no damage, return true to prevent the vehicle being considered attacked (which would have prevented repair).
                if (!hitInfo.hasDamage)
                    return true;
            }

            return null;
        }

        // Returns false if vehicle is not supported.
        private bool GetSupportedVehicleInformation(BaseCombatEntity entity, out VehicleConfig config, out string noDecayPerm, out float timeSinceLastUsed, out BaseCombatEntity vehicle)
        {
            config = null;
            noDecayPerm = string.Empty;
            timeSinceLastUsed = 0;
            vehicle = entity;

            var hab = entity as HotAirBalloon;
            if (!ReferenceEquals(hab, null))
            {
                config = _pluginConfig.Vehicles.HotAirBalloon;
                noDecayPerm = Permission_NoDecay_HotAirBalloon;
                timeSinceLastUsed = Time.time - hab.lastBlastTime;
                return true;
            }

            var kayak = entity as Kayak;
            if (!ReferenceEquals(kayak, null))
            {
                config = _pluginConfig.Vehicles.Kayak;
                noDecayPerm = Permission_NoDecay_Kayak;
                timeSinceLastUsed = kayak.timeSinceLastUsed;
                return true;
            }

            // Must go before MiniCopter.
            var scrapHeli = entity as ScrapTransportHelicopter;
            if (!ReferenceEquals(scrapHeli, null))
            {
                config = _pluginConfig.Vehicles.ScrapTransportHelicopter;
                noDecayPerm = Permission_NoDecay_ScrapHeli;
                timeSinceLastUsed = Time.time - scrapHeli.lastEngineOnTime;
                return true;
            }

            var minicopter = entity as MiniCopter;
            if (!ReferenceEquals(minicopter, null))
            {
                config = _pluginConfig.Vehicles.Minicopter;
                noDecayPerm = Permission_NoDecay_MiniCopter;
                timeSinceLastUsed = Time.time - minicopter.lastEngineOnTime;
                return true;
            }

            // Must go before MotorRowboat.
            var rhib = entity as RHIB;
            if (!ReferenceEquals(rhib, null))
            {
                config = _pluginConfig.Vehicles.RHIB;
                noDecayPerm = Permission_NoDecay_RHIB;
                timeSinceLastUsed = rhib.timeSinceLastUsedFuel;
                return true;
            }

            var horse = entity as RidableHorse;
            if (!ReferenceEquals(horse, null))
            {
                config = _pluginConfig.Vehicles.RidableHorse;
                noDecayPerm = Permission_NoDecay_RidableHorse;
                timeSinceLastUsed = Time.time - horse.lastInputTime;
                return true;
            }

            var rowboat = entity as MotorRowboat;
            if (!ReferenceEquals(rowboat, null))
            {
                config = _pluginConfig.Vehicles.Rowboat;
                noDecayPerm = Permission_NoDecay_Rowboat;
                timeSinceLastUsed = rowboat.timeSinceLastUsedFuel;
                return true;
            }

            var vehicleModule = entity as BaseVehicleModule;
            if (!ReferenceEquals(vehicleModule, null))
            {
                config = _pluginConfig.Vehicles.ModularCar;
                noDecayPerm = Permission_NoDecay_ModularCar;
                var car = vehicleModule.Vehicle as ModularCar;
                if (car == null)
                    return false;

                timeSinceLastUsed = Time.time - car.lastEngineOnTime;
                vehicle = car;
                return true;
            }

            // Must go before BaseSubmarine.
            var duoSub = entity as SubmarineDuo;
            if (!ReferenceEquals(duoSub, null))
            {
                config = _pluginConfig.Vehicles.DuoSubmarine;
                noDecayPerm = Permission_NoDecay_DuoSub;
                timeSinceLastUsed = duoSub.timeSinceLastUsed;
                return true;
            }

            var soloSub = entity as BaseSubmarine;
            if (!ReferenceEquals(soloSub, null))
            {
                config = _pluginConfig.Vehicles.SoloSubmarine;
                noDecayPerm = Permission_NoDecay_SoloSub;
                timeSinceLastUsed = soloSub.timeSinceLastUsed;
                return true;
            }

            var snowmobile = entity as Snowmobile;
            if (!ReferenceEquals(snowmobile, null))
            {
                if (snowmobile.ShortPrefabName == SnowmobileShortPrefabName)
                {
                    config = _pluginConfig.Vehicles.Snowmobile;
                    noDecayPerm = Permission_NoDecay_Snowmobile;
                    timeSinceLastUsed = snowmobile.timeSinceLastUsed;
                    return snowmobile;
                }
                if (snowmobile.ShortPrefabName == TomahaShortPrefabName)
                {
                    config = _pluginConfig.Vehicles.Tomaha;
                    noDecayPerm = Permission_NoDecay_Tomaha;
                    timeSinceLastUsed = snowmobile.timeSinceLastUsed;
                    return snowmobile;
                }
            }

            return false;
        }

        private bool UserHasPermission(ulong userId, string vehicleSpecificNoDecayPerm)
        {
            if (userId == 0)
                return false;

            var userIdString = userId.ToString();

            return permission.UserHasPermission(userIdString, Permission_NoDecay_AllVehicles)
                || permission.UserHasPermission(userIdString, vehicleSpecificNoDecayPerm);
        }

        private bool LockOwnerHasPermission(BaseEntity vehicle, string vehicleSpecificNoDecayPerm, out ulong lockOwnerId)
        {
            lockOwnerId = 0;

            var baseLock = vehicle.GetSlot(BaseEntity.Slot.Lock) as BaseLock;
            if (baseLock == null || !baseLock.IsLocked() || baseLock.OwnerID == vehicle.OwnerID)
                return false;

            lockOwnerId = baseLock.OwnerID;
            return UserHasPermission(baseLock.OwnerID, vehicleSpecificNoDecayPerm);
        }

        #endregion

        #region Configuration

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("Vehicles")]
            public VehicleConfigMap Vehicles = new VehicleConfigMap();

            [JsonProperty("Debug", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool Debug = false;
        }

        private class VehicleConfigMap
        {
            [JsonProperty("DuoSubmarine")]
            public VehicleConfig DuoSubmarine = new VehicleConfig() { ProtectionMinutesAfterUse = 45 };

            [JsonProperty("HotAirBalloon")]
            public VehicleConfig HotAirBalloon = new VehicleConfig();

            [JsonProperty("Kayak")]
            public VehicleConfig Kayak = new VehicleConfig() { ProtectionMinutesAfterUse = 45 };

            [JsonProperty("Minicopter")]
            public VehicleConfig Minicopter = new VehicleConfig();

            [JsonProperty("ModularCar")]
            public VehicleConfig ModularCar = new VehicleConfig();

            [JsonProperty("RHIB")]
            public VehicleConfig RHIB = new VehicleConfig() { ProtectionMinutesAfterUse = 45 };

            [JsonProperty("RidableHorse")]
            public VehicleConfig RidableHorse = new VehicleConfig();

            [JsonProperty("Rowboat")]
            public VehicleConfig Rowboat = new VehicleConfig() { ProtectionMinutesAfterUse = 45 };

            [JsonProperty("ScrapTransportHelicopter")]
            public VehicleConfig ScrapTransportHelicopter = new VehicleConfig();

            [JsonProperty("Snowmobile")]
            public VehicleConfig Snowmobile = new VehicleConfig() { ProtectionMinutesAfterUse = 45 };

            [JsonProperty("SoloSubmarine")]
            public VehicleConfig SoloSubmarine = new VehicleConfig() { ProtectionMinutesAfterUse = 45 };

            [JsonProperty("Tomaha")]
            public VehicleConfig Tomaha = new VehicleConfig { ProtectionMinutesAfterUse = 45 };
        }

        private class VehicleConfig
        {
            [JsonProperty("DecayMultiplierInside")]
            public float DecayMultiplierInside = 1;

            [JsonProperty("DecayMultiplierNearTC")]
            public float DecayMultiplierNearTC = 1;

            [JsonProperty("ProtectionMinutesAfterUse")]
            public float ProtectionMinutesAfterUse = 10;
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Boilerplate

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion
    }
}

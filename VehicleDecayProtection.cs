using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Decay Protection", "WhiteThunder", "1.4.0")]
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
        private const string Permission_NoDecay_SoloSub = "vehicledecayprotection.nodecay.solosubmarine";

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
            permission.RegisterPermission(Permission_NoDecay_SoloSub, this);
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
            ulong ownerId;

            if (GetSupportedVehicleInformation(entity, out vehicleConfig, out vehicleSpecificNoDecayPerm, out timeSinceLastUsed, out ownerId))
            {
                if (ownerId != 0 && HasPermissionAny(ownerId.ToString(), Permission_NoDecay_AllVehicles, vehicleSpecificNoDecayPerm))
                {
                    if (_pluginConfig.Debug)
                        LogWarning($"{entity.ShortPrefabName}: Nullifying decay damage due to permission. OwnerId: {ownerId}.");

                    damageMultiplier = 0;
                }
                else if (timeSinceLastUsed != 0 && timeSinceLastUsed < 60 * vehicleConfig.ProtectionMinutesAfterUse)
                {
                    if (_pluginConfig.Debug)
                        LogWarning($"{entity.ShortPrefabName}: Nullifying decay damage due to being recently used: {(int)timeSinceLastUsed}s < {60 * vehicleConfig.ProtectionMinutesAfterUse}s.");

                    damageMultiplier = 0;
                }
                else if (vehicleConfig.DecayMultiplierNearTC != 1.0 && entity.GetBuildingPrivilege() != null)
                {
                    if (_pluginConfig.Debug)
                        LogWarning($"{entity.ShortPrefabName}: Multiplying decay damage due to being near TC: x{vehicleConfig.DecayMultiplierNearTC}.");

                    damageMultiplier = vehicleConfig.DecayMultiplierNearTC;
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
        private bool GetSupportedVehicleInformation(BaseCombatEntity entity, out VehicleConfig config, out string noDecayPerm, out float timeSinceLastUsed, out ulong ownerId)
        {
            config = null;
            noDecayPerm = string.Empty;
            timeSinceLastUsed = 0;
            ownerId = entity.OwnerID;

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
                ownerId = car.OwnerID;
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

            return false;
        }

        private bool HasPermissionAny(string userId, params string[] permissionNames)
        {
            foreach (var perm in permissionNames)
                if (permission.UserHasPermission(userId, perm))
                    return true;

            return false;
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

            [JsonProperty("SoloSubmarine")]
            public VehicleConfig SoloSubmarine = new VehicleConfig() { ProtectionMinutesAfterUse = 45 };
        }

        private class VehicleConfig
        {
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

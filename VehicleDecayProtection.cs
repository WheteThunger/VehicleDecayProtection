using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Decay Protection", "WhiteThunder", "1.3.1")]
    [Description("Protects vehicles from decay around tool cupboards and when recently used.")]
    internal class VehicleDecayProtection : CovalencePlugin
    {
        #region Fields

        private VehicleDecayConfig PluginConfig;

        private const string Permission_NoDecay_AllVehicles = "vehicledecayprotection.nodecay.allvehicles";
        private const string Permission_NoDecay_HotAirBalloon = "vehicledecayprotection.nodecay.hotairballoon";
        private const string Permission_NoDecay_Kayak = "vehicledecayprotection.nodecay.kayak";
        private const string Permission_NoDecay_MiniCopter = "vehicledecayprotection.nodecay.minicopter";
        private const string Permission_NoDecay_ModularCar = "vehicledecayprotection.nodecay.modularcar";
        private const string Permission_NoDecay_RHIB = "vehicledecayprotection.nodecay.rhib";
        private const string Permission_NoDecay_RidableHorse = "vehicledecayprotection.nodecay.ridablehorse";
        private const string Permission_NoDecay_Rowboat = "vehicledecayprotection.nodecay.rowboat";
        private const string Permission_NoDecay_ScrapHeli = "vehicledecayprotection.nodecay.scraptransporthelicopter";

        #endregion

        #region Hooks

        private void Init()
        {
            PluginConfig = Config.ReadObject<VehicleDecayConfig>();
            permission.RegisterPermission(Permission_NoDecay_AllVehicles, this);
            permission.RegisterPermission(Permission_NoDecay_HotAirBalloon, this);
            permission.RegisterPermission(Permission_NoDecay_Kayak, this);
            permission.RegisterPermission(Permission_NoDecay_MiniCopter, this);
            permission.RegisterPermission(Permission_NoDecay_ModularCar, this);
            permission.RegisterPermission(Permission_NoDecay_RHIB, this);
            permission.RegisterPermission(Permission_NoDecay_RidableHorse, this);
            permission.RegisterPermission(Permission_NoDecay_Rowboat, this);
            permission.RegisterPermission(Permission_NoDecay_ScrapHeli, this);
        }

        // Using separate hooks to theoretically improve performance by reducing hook calls
        private object OnEntityTakeDamage(BaseVehicle entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        private object OnEntityTakeDamage(HotAirBalloon entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        private object OnEntityTakeDamage(Kayak entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        private object OnEntityTakeDamage(BaseVehicleModule entity, HitInfo hitInfo) =>
            ProcessDecayDamage(entity, hitInfo);

        #endregion

        #region Helper Methods

        private object ProcessDecayDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || !hitInfo.damageTypes.Has(Rust.DamageType.Decay)) return null;

            float multiplier = 1;

            VehicleConfig vehicleConfig = null;
            string vehicleSpecificNoDecayPerm = string.Empty;
            float lastUsedTime = float.NegativeInfinity;
            ulong ownerId = entity.OwnerID;

            if (GetSupportedVehicleInformation(entity, ref vehicleConfig, ref vehicleSpecificNoDecayPerm, ref lastUsedTime, ref ownerId))
            {
                if (ownerId != 0 && HasPermissionAny(ownerId.ToString(), Permission_NoDecay_AllVehicles, vehicleSpecificNoDecayPerm))
                    multiplier = 0;
                else if (lastUsedTime != float.NegativeInfinity && Time.time < lastUsedTime + 60 * vehicleConfig.ProtectionMinutesAfterUse)
                    multiplier = 0;
                else if (vehicleConfig.DecayMultiplierNearTC != 1.0 && entity.GetBuildingPrivilege() != null)
                    multiplier = vehicleConfig.DecayMultiplierNearTC;
            }

            if (multiplier != 1)
            {
                hitInfo.damageTypes.Scale(Rust.DamageType.Decay, multiplier);

                // If no damage, return true to prevent the vehicle being considered attacked (which prevents repair)
                if (!hitInfo.hasDamage)
                    return true;
            }

            return null;
        }

        // Returns false if vehicle is not supported
        private bool GetSupportedVehicleInformation(BaseCombatEntity entity, ref VehicleConfig config, ref string noDecayPerm, ref float lastUsedTime, ref ulong ownerId)
        {
            var hab = entity as HotAirBalloon;
            if (!ReferenceEquals(hab, null))
            {
                config = PluginConfig.Vehicles.HotAirBalloon;
                noDecayPerm = Permission_NoDecay_HotAirBalloon;
                lastUsedTime = hab.lastBlastTime;
                return true;
            }

            var kayak = entity as Kayak;
            if (!ReferenceEquals(kayak, null))
            {
                config = PluginConfig.Vehicles.Kayak;
                noDecayPerm = Permission_NoDecay_Kayak;
                //lastUsedTime = Time.realtimeSinceStartup - kayak.timeSinceLastUsed;
                lastUsedTime = 0;
                return true;
            }

            // Must go before MiniCopter
            var scrapHeli = entity as ScrapTransportHelicopter;
            if (!ReferenceEquals(scrapHeli, null))
            {
                config = PluginConfig.Vehicles.ScrapTransportHelicopter;
                noDecayPerm = Permission_NoDecay_ScrapHeli;
                lastUsedTime = scrapHeli.lastEngineTime;
                return true;
            }

            var minicopter = entity as MiniCopter;
            if (!ReferenceEquals(minicopter, null))
            {
                config = PluginConfig.Vehicles.Minicopter;
                noDecayPerm = Permission_NoDecay_MiniCopter;
                lastUsedTime = minicopter.lastEngineTime;
                return true;
            }

            // Must go before MotorRowboat
            var rhib = entity as RHIB;
            if (!ReferenceEquals(rhib, null))
            {
                config = PluginConfig.Vehicles.RHIB;
                noDecayPerm = Permission_NoDecay_RHIB;
                //lastUsedTime = Time.realtimeSinceStartup - rhib.timeSinceLastUsedFuel;
                lastUsedTime = rhib.lastHadDriverTime;
                return true;
            }

            var horse = entity as RidableHorse;
            if (!ReferenceEquals(horse, null))
            {
                config = PluginConfig.Vehicles.RidableHorse;
                noDecayPerm = Permission_NoDecay_RidableHorse;
                lastUsedTime = horse.lastInputTime;
                return true;
            }

            var rowboat = entity as MotorRowboat;
            if (!ReferenceEquals(rowboat, null))
            {
                config = PluginConfig.Vehicles.Rowboat;
                noDecayPerm = Permission_NoDecay_Rowboat;
                //lastUsedTime = Time.realtimeSinceStartup - rowboat.timeSinceLastUsedFuel;
                lastUsedTime = rowboat.lastHadDriverTime;
                return true;
            }

            var vehicleModule = entity as BaseVehicleModule;
            if (!ReferenceEquals(vehicleModule, null))
            {
                config = PluginConfig.Vehicles.ModularCar;
                noDecayPerm = Permission_NoDecay_ModularCar;
                var car = vehicleModule.Vehicle as ModularCar;
                if (car == null)
                    return false;

                lastUsedTime = car.lastEngineTime;
                ownerId = car.OwnerID;
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

        protected override void LoadDefaultConfig() => Config.WriteObject(new VehicleDecayConfig(), true);

        internal class VehicleDecayConfig
        {
            [JsonProperty("Vehicles")]
            public VehicleConfigMap Vehicles = new VehicleConfigMap();
        }

        internal class VehicleConfigMap
        {
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
        }

        internal class VehicleConfig
        {
            [JsonProperty("DecayMultiplierNearTC")]
            public float DecayMultiplierNearTC = 1;

            [JsonProperty("ProtectionMinutesAfterUse")]
            public float ProtectionMinutesAfterUse = 10;
        }

        #endregion
    }
}

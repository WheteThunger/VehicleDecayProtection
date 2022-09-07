// #define DEBUG_LOG
// #define DEBUG_SHOW

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Libraries;
using Rust;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Decay Protection", "WhiteThunder", "2.2.0")]
    [Description("Protects vehicles from decay based on ownership and other factors.")]
    internal class VehicleDecayProtection : CovalencePlugin
    {
        #region Fields

        private Configuration _pluginConfig;

        private const string Permission_NoDecay_AllVehicles = "vehicledecayprotection.nodecay.allvehicles";

        private const float VanillaDecaySeconds = 60f;
        private const float MaxDrawSeconds = 30f;
        private const float MaxDrawDistanceSquared = 10000f;

        private readonly VehicleInfoManager _vehicleInfoManager;

        public VehicleDecayProtection()
        {
            _vehicleInfoManager = new VehicleInfoManager(this);
        }

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(Permission_NoDecay_AllVehicles, this);
        }

        private void OnServerInitialized()
        {
            _vehicleInfoManager.OnServerInitialized(_pluginConfig);

            foreach (var networkable in BaseNetworkable.serverEntities)
            {
                var entity = networkable as BaseEntity;
                if (entity is BaseVehicle || entity is HotAirBalloon)
                {
                    HandleEntitySpawned(entity);
                }
            }
        }

        private void Unload()
        {
            foreach (var networkable in BaseNetworkable.serverEntities)
            {
                var entity = networkable as BaseEntity;
                if (entity is BaseVehicle || entity is HotAirBalloon)
                {
                    var vehicleInfo = _vehicleInfoManager.GetVehicleInfo(entity);
                    if (vehicleInfo == null)
                        continue;

                    VehicleDecayReplacer.RemoveFromEntity(entity);
                }
            }
        }

        // Using separate hooks to improve performance by reducing hook calls.
        private void OnEntitySpawned(BaseVehicle entity) => HandleEntitySpawned(entity);
        private void OnEntitySpawned(HotAirBalloon entity) => HandleEntitySpawned(entity);

        #endregion

        #region Helper Methods

        private void HandleEntitySpawned(BaseEntity entity)
        {
            var vehicleInfo = _vehicleInfoManager.GetVehicleInfo(entity);
            if (vehicleInfo == null)
                return;

            VehicleDecayReplacer.AddToEntity(entity, vehicleInfo);
        }

        private bool UserHasPermission(UserData userData, string perm)
        {
            return userData.Perms.Contains(perm)
                || permission.GroupsHavePermission(userData.Groups, perm);
        }

        private bool OwnerHasPermission(ulong ownerId, string vehicleSpecificNoDecayPerm)
        {
            if (ownerId == 0)
                return false;

            var userData = permission.GetUserData(ownerId.ToString());

            return UserHasPermission(userData, Permission_NoDecay_AllVehicles)
                || UserHasPermission(userData, vehicleSpecificNoDecayPerm);
        }

        private bool LockOwnerHasPermission(BaseEntity vehicle, string vehicleSpecificNoDecayPerm, out ulong lockOwnerId)
        {
            lockOwnerId = 0;

            var baseLock = vehicle.GetSlot(BaseEntity.Slot.Lock) as BaseLock;
            if (baseLock == null || !baseLock.IsLocked() || baseLock.OwnerID == vehicle.OwnerID)
                return false;

            lockOwnerId = baseLock.OwnerID;
            return OwnerHasPermission(baseLock.OwnerID, vehicleSpecificNoDecayPerm);
        }

        public static void LogInfo(string message) => Interface.Oxide.LogInfo($"[Vehicle Decay Protection] {message}");
        public static void LogError(string message) => Interface.Oxide.LogError($"[Vehicle Decay Protection] {message}");
        public static void LogWarning(string message) => Interface.Oxide.LogWarning($"[Vehicle Decay Protection] {message}");

        private static string[] FindPrefabsOfType<T>() where T : BaseEntity
        {
            var prefabList = new List<string>();

            foreach (var assetPath in GameManifest.Current.entities)
            {
                var entity = GameManager.server.FindPrefab(assetPath)?.GetComponent<T>();
                if (entity == null)
                    continue;

                prefabList.Add(entity.PrefabName);
            }

            return prefabList.ToArray();
        }

        private static bool IsPlayerDrawEligible(BasePlayer player, BaseEntity entity)
        {
            return player.IsAdmin
                && (player.transform.position - entity.transform.position).sqrMagnitude < MaxDrawDistanceSquared;
        }

        private static void DrawVehicleText(BasePlayer player, BaseEntity entity, VehicleInfo vehicleInfo, Color color, string text)
        {
            player.SendConsoleCommand(
                "ddraw.text",
                Mathf.Min(30, vehicleInfo.VehicleConfig.DecayIntervalSeconds - 5f),
                color,
                entity.transform.position + new Vector3(0, entity.WorldSpaceBounds().extents.y * 2, 0),
                $"<size=20>VDP ({vehicleInfo.VehicleConfig.DecayIntervalSeconds}s)\n{text}</size>"
            );
        }

        private static void SetupDecayTick(FacepunchBehaviour component, Action action, float time)
        {
            component.InvokeRandomized(action, UnityEngine.Random.Range(time / 2f, time), time, time / 10f);
        }

        private static bool WasRecentlyUsed(BaseEntity entity, VehicleInfo vehicleInfo)
        {
            var timeSinceLastUsed = vehicleInfo.GetTimeSinceLastUsed(entity);
            var vehicleConfig = vehicleInfo.VehicleConfig;
            if (vehicleConfig.ProtectionMinutesAfterUse != -1 && timeSinceLastUsed >= 60 * vehicleConfig.ProtectionMinutesAfterUse)
                return false;

            #if DEBUG_SHOW
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (IsPlayerDrawEligible(player, entity))
                    {
                        DrawVehicleText(player, entity, vehicleInfo, Color.green, $"{(int)timeSinceLastUsed}s < {60 * vehicleConfig.ProtectionMinutesAfterUse}s");
                    }
                }
            #endif

            #if DEBUG_LOG
                LogWarning($"{entity.ShortPrefabName} :: Recently used :: {(int)timeSinceLastUsed}s < {60 * vehicleConfig.ProtectionMinutesAfterUse}s");
            #endif

            return true;
        }

        private static bool VehicleHasPermission(VehicleDecayProtection pluginInstance, BaseEntity entity, VehicleInfo vehicleInfo)
        {
            if (!pluginInstance._pluginConfig.EnablePermission)
                return false;

            pluginInstance.TrackStart();
            var ownerHasPermission = pluginInstance.OwnerHasPermission(entity.OwnerID, vehicleInfo.Permission);
            pluginInstance.TrackEnd();

            if (ownerHasPermission)
            {
                #if DEBUG_SHOW
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (IsPlayerDrawEligible(player, entity))
                        {
                            DrawVehicleText(player, entity, vehicleInfo, Color.green, "Owner permission");
                        }
                    }
                #endif

                #if DEBUG_LOG
                    LogWarning($"{entity.ShortPrefabName} :: Owner has permission :: {entity.OwnerID}");
                #endif

                return true;
            }

            ulong lockOwnerId;
            pluginInstance.TrackStart();
            var lockOwnerHasPermission = pluginInstance.LockOwnerHasPermission(entity, vehicleInfo.Permission, out lockOwnerId);
            pluginInstance.TrackEnd();

            if (lockOwnerHasPermission)
            {
                #if DEBUG_SHOW
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (IsPlayerDrawEligible(player, entity))
                        {
                            DrawVehicleText(player, entity, vehicleInfo, Color.green, "Lock owner permission");
                        }
                    }
                #endif

                #if DEBUG_LOG
                    LogWarning($"{entity.ShortPrefabName} :: Lock owner has permission :: {lockOwnerId}");
                #endif

                return true;
            }

            return false;
        }

        private static float GetInsideMultiplier(BaseEntity entity, VehicleInfo vehicleInfo, out bool isOutside, bool forceOutsideCheck)
        {
            isOutside = true;

            var vehicleConfig = vehicleInfo.VehicleConfig;
            if (forceOutsideCheck || vehicleConfig.DecayMultiplierInside != 1f)
            {
                isOutside = entity.IsOutside();
            }

            if (vehicleConfig.DecayMultiplierInside == 1f || isOutside)
                return 1f;

            #if DEBUG_SHOW
                if (vehicleConfig.DecayMultiplierInside == 0f)
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (IsPlayerDrawEligible(player, entity))
                        {
                            DrawVehicleText(player, entity, vehicleInfo, Color.green, $"Inside x{vehicleConfig.DecayMultiplierInside}");
                        }
                    }
                }
            #endif

            #if DEBUG_LOG
                LogWarning($"{entity.ShortPrefabName} :: Inside :: x{vehicleConfig.DecayMultiplierInside}");
            #endif

            return vehicleConfig.DecayMultiplierInside;
        }

        private static float GetNearTCMultiplier(VehicleDecayProtection pluginInstance, BaseEntity entity, VehicleInfo vehicleInfo)
        {
            var vehicleConfig = vehicleInfo.VehicleConfig;
            if (vehicleConfig.DecayMultiplierNearTC == 1f)
                return 1f;

            pluginInstance.TrackStart();
            var hasBuildingPrivilege = entity.GetBuildingPrivilege() != null;
            pluginInstance.TrackEnd();

            if (!hasBuildingPrivilege)
                return 1f;

            #if DEBUG_SHOW
                if (vehicleConfig.DecayMultiplierNearTC == 0f)
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (IsPlayerDrawEligible(player, entity))
                        {
                            DrawVehicleText(player, entity, vehicleInfo, Color.green, $"Near TC x{vehicleConfig.DecayMultiplierNearTC}");
                        }
                    }
                }
            #endif

            #if DEBUG_LOG
                LogWarning($"{entity.ShortPrefabName} :: Near TC :: x{vehicleConfig.DecayMultiplierNearTC}");
            #endif

            return vehicleConfig.DecayMultiplierNearTC;
        }

        private static float GetLocationMultiplier(VehicleDecayProtection pluginInstance, BaseEntity entity, VehicleInfo vehicleInfo, out bool isOutside, bool forceOutsideCheck = false)
        {
            var multiplier = GetInsideMultiplier(entity, vehicleInfo, out isOutside, forceOutsideCheck);
            if (multiplier == 0f)
                return 0f;

            multiplier *= GetNearTCMultiplier(pluginInstance, entity, vehicleInfo);
            if (multiplier == 0f)
                return 0f;

            return multiplier;
        }

        private static float GetLocationMultiplier(VehicleDecayProtection pluginInstance, BaseEntity entity, VehicleInfo vehicleInfo)
        {
            bool isOutside;
            return GetLocationMultiplier(pluginInstance, entity, vehicleInfo, out isOutside);
        }

        private static void DoDecayDamage(BaseCombatEntity entity, VehicleInfo vehicleInfo, float fraction, DamageType damageType = DamageType.Decay, bool useProtection = false)
        {
            var amount = entity.MaxHealth() * fraction * vehicleInfo.GetTimeMultiplier();

            if (useProtection && entity.baseProtection != null)
            {
                // Manually scale damage so that we can show the correct amount.
                amount *= (1 - entity.baseProtection.amounts[(int)damageType]);
            }

            #if DEBUG_SHOW
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (IsPlayerDrawEligible(player, entity))
                    {
                        DrawVehicleText(player, entity, vehicleInfo, Color.red, $"-{amount:f2}");
                    }
                }
            #endif

            if (amount == 0)
                return;

            entity.Hurt(amount, damageType, entity, useProtection: false);
        }

        private static void DoCarDecayDamage(ModularCar car, VehicleInfo vehicleInfo, float amount)
        {
            #if DEBUG_SHOW
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (IsPlayerDrawEligible(player, car))
                    {
                        DrawVehicleText(player, car, vehicleInfo, Color.red, $"-{amount:f2}");
                    }
                }
            #endif

            car.DoDecayDamage(amount);
        }

        private static void MinicopterDecay(VehicleDecayProtection pluginInstance, MiniCopter miniCopter, VehicleInfo vehicleInfo)
        {
            if (miniCopter.healthFraction == 0f
                || miniCopter.IsOn()
                || WasRecentlyUsed(miniCopter, vehicleInfo)
                || VehicleHasPermission(pluginInstance, miniCopter, vehicleInfo))
                return;

            bool isOutside;
            var multiplier = GetLocationMultiplier(pluginInstance, miniCopter, vehicleInfo, out isOutside, forceOutsideCheck: true);
            if (multiplier == 0f)
                return;

            var decayMinutes = isOutside ? MiniCopter.outsidedecayminutes : MiniCopter.insidedecayminutes;
            DoDecayDamage(miniCopter, vehicleInfo, multiplier / decayMinutes);
        }

        private static void SnowmobileDecay(VehicleDecayProtection pluginInstance, Snowmobile snowmobile, VehicleInfo vehicleInfo)
        {
            if (snowmobile.IsDead()
                || WasRecentlyUsed(snowmobile, vehicleInfo)
                || VehicleHasPermission(pluginInstance, snowmobile, vehicleInfo))
                return;

            var multiplier = GetLocationMultiplier(pluginInstance, snowmobile, vehicleInfo);
            if (multiplier == 0f)
                return;

            DoDecayDamage(snowmobile, vehicleInfo, multiplier / Snowmobile.outsideDecayMinutes);
        }

        private static void WaterVehicleDecay(VehicleDecayProtection pluginInstance, BaseCombatEntity waterVehicle, VehicleInfo vehicleInfo, float outsideDecayMinutes, float deepWaterDecayMinutes)
        {
            if (waterVehicle.healthFraction == 0f
                || WasRecentlyUsed(waterVehicle, vehicleInfo)
                || VehicleHasPermission(pluginInstance, waterVehicle, vehicleInfo))
                return;

            var multiplier = GetLocationMultiplier(pluginInstance, waterVehicle, vehicleInfo);
            if (multiplier == 0f)
                return;

            var decayMinutes = outsideDecayMinutes;
            var overallWaterDepth = WaterLevel.GetOverallWaterDepth(waterVehicle.transform.position);
            if (overallWaterDepth > 12f)
            {
                var divisor = Mathf.Lerp(0.1f, 1f, Mathf.InverseLerp(12f, 16f, overallWaterDepth));
                decayMinutes = Mathf.Min(decayMinutes, deepWaterDecayMinutes / divisor);
            }

            DoDecayDamage(waterVehicle, vehicleInfo, multiplier / decayMinutes);
        }

        private static void SledDecay(VehicleDecayProtection pluginInstance, Sled sled, VehicleInfo vehicleInfo)
        {
            if (sled.DecayAmount == 0f
                || sled.AnyMounted()
                || VehicleHasPermission(pluginInstance, sled, vehicleInfo))
                return;

            var multiplier = GetLocationMultiplier(pluginInstance, sled, vehicleInfo);
            if (multiplier == 0f)
                return;

            DoDecayDamage(sled, vehicleInfo, multiplier * sled.DecayAmount / sled.MaxHealth(), DamageType.Generic, useProtection: true);
        }

        #endregion

        #region Vehicle Decay Component

        private class VehicleDecayReplacer : FacepunchBehaviour
        {
            public static void AddToEntity(BaseEntity entity, VehicleInfo vehicleInfo)
            {
                var component = entity.gameObject.AddComponent<VehicleDecayReplacer>();
                component._entity = entity;
                component._vehicleInfo = vehicleInfo;

                // Cancel vanilla decay.
                entity.CancelInvoke(vehicleInfo.GetVanillaDecayMethod(entity));

                // Enable custom decay.
                SetupDecayTick(component, component.DecayTick, vehicleInfo.VehicleConfig.DecayIntervalSeconds);
            }

            public static void RemoveFromEntity(BaseEntity entity)
            {
                var component = entity.gameObject.GetComponent<VehicleDecayReplacer>();
                if (component == null)
                    return;

                // Enable vanilla decay.
                SetupDecayTick(entity, component._vehicleInfo.GetVanillaDecayMethod(entity), VanillaDecaySeconds);

                // Cancel custom decay.
                DestroyImmediate(component);
            }

            private BaseEntity _entity;
            private VehicleInfo _vehicleInfo;

            private void DecayTick()
            {
                _vehicleInfo.DecayTick(_entity, _vehicleInfo);
            }
        }

        #endregion

        #region Vehicle Info

        private class VehicleInfo
        {
            public string VehicleType;
            public VehicleConfig VehicleConfig;
            public string[] PrefabPaths;
            public string Permission { get; private set; }
            public uint[] PrefabIds { get; private set; }

            public Action<BaseEntity, VehicleInfo> DecayTick = (entity, vehicleInfo) =>
            {
                throw new NotImplementedException();
            };

            public Func<BaseEntity, bool> IsCorrectType = (entity) =>
            {
                throw new NotImplementedException();
            };

            public Func<BaseEntity, float> GetTimeSinceLastUsed = (entity) =>
            {
                throw new NotImplementedException();
            };

            public Func<BaseEntity, Action> GetVanillaDecayMethod = (enitty) =>
            {
                throw new NotImplementedException();
            };

            public void OnServerInitialized(VehicleDecayProtection pluginInstance)
            {
                Permission = $"{nameof(VehicleDecayProtection)}.nodecay.{VehicleType}".ToLower();
                pluginInstance.permission.RegisterPermission(Permission, pluginInstance);

                var prefabIds = new List<uint>(PrefabPaths.Length);

                foreach (var prefabName in PrefabPaths)
                {
                    var prefabId = StringPool.Get(prefabName);
                    if (prefabId != 0)
                    {
                        prefabIds.Add(prefabId);
                    }
                    else
                    {
                        LogError($"Invalid prefab. Please alert the plugin maintainer -- {prefabName}");
                    }
                }

                PrefabIds = prefabIds.ToArray();
            }

            public float GetTimeMultiplier()
            {
                return VehicleConfig.DecayIntervalSeconds / VanillaDecaySeconds;
            }
        }

        private class VehicleInfoManager
        {
            private VehicleDecayProtection _pluginInstance;

            private readonly Dictionary<uint, VehicleInfo> _prefabIdToVehicleInfo = new Dictionary<uint, VehicleInfo>();

            public VehicleInfoManager(VehicleDecayProtection pluginInstance)
            {
                _pluginInstance = pluginInstance;
            }

            public void OnServerInitialized(Configuration pluginConfig)
            {
                var allVehicles = new VehicleInfo[]
                {
                    new VehicleInfo
                    {
                        VehicleType = "duosubmarine",
                        PrefabPaths = new string[] { "assets/content/vehicles/submarine/submarineduo.entity.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.DuoSubmarine,
                        IsCorrectType = (entity) => entity is SubmarineDuo,
                        GetTimeSinceLastUsed = (entity) => (entity as SubmarineDuo).timeSinceLastUsed,
                        GetVanillaDecayMethod = (entity) => (entity as SubmarineDuo).SubmarineDecay,
                        DecayTick = (entity, vehicleInfo) => WaterVehicleDecay(
                            _pluginInstance,
                            entity as SubmarineDuo,
                            vehicleInfo,
                            BaseSubmarine.outsidedecayminutes,
                            BaseSubmarine.deepwaterdecayminutes
                        )
                    },
                    new VehicleInfo
                    {
                        VehicleType = "hotairballoon",
                        PrefabPaths = new string[] { "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.HotAirBalloon,
                        IsCorrectType = (entity) => entity is HotAirBalloon,
                        GetTimeSinceLastUsed = (entity) => UnityEngine.Time.time - (entity as HotAirBalloon).lastBlastTime,
                        GetVanillaDecayMethod = (entity) => (entity as HotAirBalloon).DecayTick,
                        DecayTick = (entity, vehicleInfo) =>
                        {
                            var hab = entity as HotAirBalloon;

                            if (hab.healthFraction == 0f
                                || hab.IsFullyInflated
                                || WasRecentlyUsed(hab, vehicleInfo)
                                || VehicleHasPermission(_pluginInstance, hab, vehicleInfo))
                                return;

                            var multiplier = GetLocationMultiplier(_pluginInstance, hab, vehicleInfo);
                            if (multiplier == 0f)
                                return;

                            DoDecayDamage(hab, vehicleInfo, multiplier / HotAirBalloon.outsidedecayminutes);
                        }
                    },
                    new VehicleInfo
                    {
                        VehicleType = "kayak",
                        PrefabPaths = new string[] { "assets/content/vehicles/boats/kayak/kayak.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.Kayak,
                        IsCorrectType = (entity) => entity is Kayak,
                        GetTimeSinceLastUsed = (entity) => (entity as Kayak).timeSinceLastUsed,
                        GetVanillaDecayMethod = (entity) => (entity as Kayak).BoatDecay,
                        DecayTick = (entity, vehicleInfo) => WaterVehicleDecay(
                            _pluginInstance,
                            entity as Kayak,
                            vehicleInfo,
                            MotorRowboat.outsidedecayminutes,
                            MotorRowboat.deepwaterdecayminutes
                        ),
                    },
                    new VehicleInfo
                    {
                        VehicleType = "minicopter",
                        PrefabPaths = new string[] { "assets/content/vehicles/minicopter/minicopter.entity.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.Minicopter,
                        IsCorrectType = (entity) => entity is MiniCopter,
                        GetTimeSinceLastUsed = (entity) => UnityEngine.Time.time - (entity as MiniCopter).lastEngineOnTime,
                        GetVanillaDecayMethod = (entity) => (entity as MiniCopter).DecayTick,
                        DecayTick = (entity, vehicleInfo) => MinicopterDecay(_pluginInstance, entity as MiniCopter, vehicleInfo),
                    },
                    new VehicleInfo
                    {
                        VehicleType = "modularcar",
                        // There are at least 37 valid Modular Car prefabs.
                        PrefabPaths = FindPrefabsOfType<ModularCar>(),
                        VehicleConfig = pluginConfig.Vehicles.ModularCar,
                        IsCorrectType = (entity) => entity is ModularCar,
                        GetTimeSinceLastUsed = (entity) => UnityEngine.Time.time - (entity as ModularCar).lastEngineOnTime,
                        GetVanillaDecayMethod = (entity) => (entity as ModularCar).DecayTick,
                        DecayTick = (entity, vehicleInfo) =>
                        {
                            var car = entity as ModularCar;

                            if (car.IsDestroyed
                                || car.IsOn()
                                || car.immuneToDecay
                                || WasRecentlyUsed(car, vehicleInfo)
                                || VehicleHasPermission(_pluginInstance, car, vehicleInfo))
                                return;

                            if (car.IsDead())
                            {
                                var numModules = Mathf.Max(1, car.AttachedModuleEntities.Count);
                                DoCarDecayDamage(car, vehicleInfo, 120f / numModules * vehicleInfo.GetTimeMultiplier());
                                return;
                            }

                            var multiplier = GetLocationMultiplier(_pluginInstance, car, vehicleInfo);
                            if (multiplier == 0f)
                                return;

                            var health = car.HasAnyModules
                                ? car.AttachedModuleEntities.Max(module => module.MaxHealth())
                                : car.MaxHealth();

                            DoCarDecayDamage(car, vehicleInfo, health * vehicleInfo.GetTimeMultiplier() * multiplier / ModularCar.outsidedecayminutes);
                        }
                    },
                    new VehicleInfo
                    {
                        VehicleType = "rhib",
                        PrefabPaths = new string[] { "assets/content/vehicles/boats/rhib/rhib.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.RHIB,
                        IsCorrectType = (entity) => entity is RHIB,
                        GetTimeSinceLastUsed = (entity) => (entity as RHIB).timeSinceLastUsedFuel,
                        GetVanillaDecayMethod = (entity) => (entity as RHIB).BoatDecay,
                        DecayTick = (entity, vehicleInfo) =>
                        {
                            var rhib = entity as MotorRowboat;
                            if (rhib.dying)
                                return;

                            WaterVehicleDecay(
                                _pluginInstance,
                                rhib,
                                vehicleInfo,
                                MotorRowboat.outsidedecayminutes,
                                MotorRowboat.deepwaterdecayminutes
                            );
                        }
                    },
                    new VehicleInfo
                    {
                        VehicleType = "ridablehorse",
                        PrefabPaths = new string[] { "assets/rust.ai/nextai/testridablehorse.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.RidableHorse,
                        IsCorrectType = (entity) => entity is RidableHorse,
                        GetTimeSinceLastUsed = (entity) => UnityEngine.Time.time - (entity as RidableHorse).lastInputTime,
                        GetVanillaDecayMethod = (entity) => (entity as RidableHorse).AnimalDecay,
                        DecayTick = (entity, vehicleInfo) =>
                        {
                            var horse = entity as RidableHorse;

                            if (horse.healthFraction == 0f
                                || horse.IsDestroyed
                                || WasRecentlyUsed(horse, vehicleInfo)
                                || UnityEngine.Time.time < horse.lastEatTime + 600f
                                || horse.IsForSale()
                                || UnityEngine.Time.time < horse.nextDecayTime
                                || VehicleHasPermission(_pluginInstance, horse, vehicleInfo))
                                return;

                            var multiplier = GetLocationMultiplier(_pluginInstance, horse, vehicleInfo);
                            if (multiplier == 0f)
                                return;

                            DoDecayDamage(horse, vehicleInfo, multiplier / BaseRidableAnimal.decayminutes);
                        }
                    },
                    new VehicleInfo
                    {
                        VehicleType = "rowboat",
                        PrefabPaths = new string[] { "assets/content/vehicles/boats/rowboat/rowboat.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.Rowboat,
                        IsCorrectType = (entity) => entity is MotorRowboat,
                        GetTimeSinceLastUsed = (entity) => (entity as MotorRowboat).timeSinceLastUsedFuel,
                        GetVanillaDecayMethod = (entity) => (entity as MotorRowboat).BoatDecay,
                        DecayTick = (entity, vehicleInfo) =>
                        {
                            var rowBoat = entity as MotorRowboat;
                            if (rowBoat.dying)
                                return;

                            WaterVehicleDecay(
                                _pluginInstance,
                                rowBoat,
                                vehicleInfo,
                                MotorRowboat.outsidedecayminutes,
                                MotorRowboat.deepwaterdecayminutes
                            );
                        }
                    },
                    new VehicleInfo
                    {
                        VehicleType = "scraptransporthelicopter",
                        PrefabPaths = new string[] { "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.ScrapTransportHelicopter,
                        IsCorrectType = (entity) => entity is ScrapTransportHelicopter,
                        GetTimeSinceLastUsed = (entity) => UnityEngine.Time.time - (entity as ScrapTransportHelicopter).lastEngineOnTime,
                        GetVanillaDecayMethod = (entity) => (entity as ScrapTransportHelicopter).DecayTick,
                        DecayTick = (entity, vehicleInfo) => MinicopterDecay(_pluginInstance, entity as ScrapTransportHelicopter, vehicleInfo),
                    },
                    new VehicleInfo
                    {
                        VehicleType = "sled",
                        PrefabPaths = new string[] { "assets/prefabs/misc/xmas/sled/sled.deployed.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.Sled,
                        IsCorrectType = (entity) => entity is Sled,
                        GetTimeSinceLastUsed = (entity) => float.MaxValue,
                        GetVanillaDecayMethod = (entity) => (entity as Sled).DecayOverTime,
                        DecayTick = (entity, vehicleInfo) => SledDecay(_pluginInstance, entity as Sled, vehicleInfo)
                    },
                    new VehicleInfo
                    {
                        VehicleType = "sled.xmas",
                        PrefabPaths = new string[] { "assets/prefabs/misc/xmas/sled/skins/sled.deployed.xmas.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.SledXmas,
                        IsCorrectType = (entity) => entity is Sled,
                        GetTimeSinceLastUsed = (entity) => float.MaxValue,
                        GetVanillaDecayMethod = (entity) => (entity as Sled).DecayOverTime,
                        DecayTick = (entity, vehicleInfo) => SledDecay(_pluginInstance, entity as Sled, vehicleInfo)
                    },
                    new VehicleInfo
                    {
                        VehicleType = "snowmobile",
                        PrefabPaths = new string[] { "assets/content/vehicles/snowmobiles/snowmobile.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.Snowmobile,
                        IsCorrectType = (entity) => entity is Snowmobile,
                        GetTimeSinceLastUsed = (entity) => (entity as Snowmobile).timeSinceLastUsed,
                        GetVanillaDecayMethod = (entity) => (entity as Snowmobile).SnowmobileDecay,
                        DecayTick = (entity, vehicleInfo) => SnowmobileDecay(_pluginInstance, entity as Snowmobile, vehicleInfo),
                    },
                    new VehicleInfo
                    {
                        VehicleType = "solosubmarine",
                        PrefabPaths = new string[] { "assets/content/vehicles/submarine/submarinesolo.entity.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.SoloSubmarine,
                        IsCorrectType = (entity) => entity is BaseSubmarine,
                        GetTimeSinceLastUsed = (entity) => (entity as BaseSubmarine).timeSinceLastUsed,
                        GetVanillaDecayMethod = (entity) => (entity as BaseSubmarine).SubmarineDecay,
                        DecayTick = (entity, vehicleInfo) => WaterVehicleDecay(
                            _pluginInstance,
                            entity as BaseSubmarine,
                            vehicleInfo,
                            BaseSubmarine.outsidedecayminutes,
                            BaseSubmarine.deepwaterdecayminutes
                        ),
                    },
                    new VehicleInfo
                    {
                        VehicleType = "tomaha",
                        PrefabPaths = new string[] { "assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab" },
                        VehicleConfig = pluginConfig.Vehicles.Tomaha,
                        IsCorrectType = (entity) => entity is Snowmobile,
                        GetTimeSinceLastUsed = (entity) => (entity as Snowmobile).timeSinceLastUsed,
                        GetVanillaDecayMethod = (entity) => (entity as Snowmobile).SnowmobileDecay,
                        DecayTick = (entity, vehicleInfo) => SnowmobileDecay(_pluginInstance, entity as Snowmobile, vehicleInfo),
                    },
                };

                foreach (var vehicleInfo in allVehicles)
                {
                    vehicleInfo.OnServerInitialized(_pluginInstance);

                    foreach (var prefabId in vehicleInfo.PrefabIds)
                    {
                        _prefabIdToVehicleInfo[prefabId] = vehicleInfo;
                    }
                }
            }

            public VehicleInfo GetVehicleInfo(BaseEntity entity)
            {
                VehicleInfo vehicleInfo;
                return _prefabIdToVehicleInfo.TryGetValue(entity.prefabID, out vehicleInfo) && vehicleInfo.IsCorrectType(entity)
                    ? vehicleInfo
                    : null;
            }
        }

        #endregion

        #region Configuration

        private class VehicleConfig
        {
            [JsonProperty("DecayMultiplierInside")]
            public float DecayMultiplierInside = 1;

            [JsonProperty("DecayMultiplierNearTC")]
            public float DecayMultiplierNearTC = 1;

            [JsonProperty("ProtectionMinutesAfterUse", DefaultValueHandling = DefaultValueHandling.Ignore)]
            [DefaultValue(-1f)]
            public float ProtectionMinutesAfterUse = 10;

            [JsonProperty("DecayIntervalSeconds")]
            public float DecayIntervalSeconds = 60;
        }

        private class VehicleConfigMap
        {
            [JsonProperty("DuoSubmarine")]
            public VehicleConfig DuoSubmarine = new VehicleConfig
            {
                DecayMultiplierInside = 0f,
                ProtectionMinutesAfterUse = 45,
            };

            [JsonProperty("HotAirBalloon")]
            public VehicleConfig HotAirBalloon = new VehicleConfig
            {
                DecayMultiplierInside = 0f,
                ProtectionMinutesAfterUse = 10,
            };

            [JsonProperty("Kayak")]
            public VehicleConfig Kayak = new VehicleConfig
            {
                DecayMultiplierInside = 0f,
                ProtectionMinutesAfterUse = 45,
            };

            [JsonProperty("Minicopter")]
            public VehicleConfig Minicopter = new VehicleConfig
            {
                DecayMultiplierInside = 1f,
                ProtectionMinutesAfterUse = 10,
            };

            [JsonProperty("ModularCar")]
            public VehicleConfig ModularCar = new VehicleConfig
            {
                DecayMultiplierInside = 0.1f,
                ProtectionMinutesAfterUse = 10,
            };

            [JsonProperty("RHIB")]
            public VehicleConfig RHIB = new VehicleConfig
            {
                DecayMultiplierInside = 0f,
                ProtectionMinutesAfterUse = 45,
            };

            [JsonProperty("RidableHorse")]
            public VehicleConfig RidableHorse = new VehicleConfig
            {
                DecayMultiplierInside = 0.5f,
                ProtectionMinutesAfterUse = 10,
            };

            [JsonProperty("Rowboat")]
            public VehicleConfig Rowboat = new VehicleConfig
            {
                DecayMultiplierInside = 0f,
                ProtectionMinutesAfterUse = 45,
            };

            [JsonProperty("ScrapTransportHelicopter")]
            public VehicleConfig ScrapTransportHelicopter = new VehicleConfig
            {
                DecayMultiplierInside = 1f,
                ProtectionMinutesAfterUse = 10,
            };

            [JsonProperty("Sled")]
            public VehicleConfig Sled = new VehicleConfig
            {
                DecayMultiplierInside = 1f,
                ProtectionMinutesAfterUse = -1,
            };

            [JsonProperty("Sled.Xmas")]
            public VehicleConfig SledXmas = new VehicleConfig
            {
                DecayMultiplierInside = 1f,
                ProtectionMinutesAfterUse = -1,
            };

            [JsonProperty("Snowmobile")]
            public VehicleConfig Snowmobile = new VehicleConfig
            {
                DecayMultiplierInside = 0f,
                ProtectionMinutesAfterUse = 45,
            };

            [JsonProperty("SoloSubmarine")]
            public VehicleConfig SoloSubmarine = new VehicleConfig
            {
                DecayMultiplierInside = 0f,
                ProtectionMinutesAfterUse = 45,
            };

            [JsonProperty("Tomaha")]
            public VehicleConfig Tomaha = new VehicleConfig
            {
                DecayMultiplierInside = 0f,
                ProtectionMinutesAfterUse = 45,
            };
        }

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("EnablePermission")]
            public bool EnablePermission = true;

            [JsonProperty("Vehicles")]
            public VehicleConfigMap Vehicles = new VehicleConfigMap();
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

﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFTReflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global

namespace EFTApi.Helpers
{
    public class PlayerHelper
    {
        private static readonly Lazy<PlayerHelper> Lazy = new Lazy<PlayerHelper>(() => new PlayerHelper());

        public static PlayerHelper Instance => Lazy.Value;

        public Player Player { get; private set; }

        public FirearmControllerData FirearmControllerHelper => FirearmControllerData.Instance;

        public WeaponData WeaponHelper => WeaponData.Instance;

        public ArmorComponentData ArmorComponentHelper => ArmorComponentData.Instance;

        public RoleData RoleHelper => RoleData.Instance;

        public InventoryData InventoryHelper => InventoryData.Instance;

        public DamageInfoData DamageInfoHelper => DamageInfoData.Instance;

        public SpeakerData SpeakerHelper => SpeakerData.Instance;

        public HealthControllerData HealthControllerHelper => HealthControllerData.Instance;

        public GamePlayerOwnerData GamePlayerOwnerHelper => GamePlayerOwnerData.Instance;

        public MovementContextData MovementContextHelper => MovementContextData.Instance;

        public QuestControllerData QuestControllerHelper => QuestControllerData.Instance;

        public InventoryControllerData InventoryControllerHelper => InventoryControllerData.Instance;

        public SearchControllerData SearchControllerHelper => SearchControllerData.Instance;

        public InfoClassData InfoClassHelper => InfoClassData.Instance;

        /// <summary>
        ///     Init Action
        /// </summary>
        public readonly RefHelper.HookRef Init;

        /// <summary>
        ///     Dispose Action
        /// </summary>
        public readonly RefHelper.HookRef Dispose;

        public readonly RefHelper.HookRef OnDead;

        public readonly RefHelper.HookRef ApplyDamageInfo;

        private readonly Func<Player, int, bool> _refGetBleedBlock;

        /// <summary>
        ///     Fika.Core.Coop.Players.ObservedCoopPlayer.ApplyShot
        /// </summary>
        [CanBeNull] public readonly RefHelper.HookRef ObservedCoopApplyShot;

        public readonly RefHelper.HookRef OnBeenKilledByAggressor;

        public readonly RefHelper.HookRef OnPhraseTold;

        /// <summary>
        ///     Fika.Core.Coop.Players.ObservedCoopPlayer.OnPhraseTold
        /// </summary>
        [CanBeNull] public readonly RefHelper.HookRef ObservedCoopOnPhraseTold;

        public readonly RefHelper.HookRef SetPropVisibility;

        /// <summary>
        ///     InfoClass.Settings
        /// </summary>
        public readonly RefHelper.IRef<InfoClass, object> RefSettings;

        /// <summary>
        ///     InfoClass.Settings.Role
        /// </summary>
        public readonly RefHelper.FieldRef<object, WildSpawnType> RefRole;

        /// <summary>
        ///     InfoClass.Settings.Experience
        /// </summary>
        public readonly RefHelper.FieldRef<object, int> RefExperience;

        public readonly RefHelper.PropertyRef<Player, object> RefSkills;

        public object Settings => RefSettings.GetValue(Player?.Profile.Info);

        public WildSpawnType Role => RefRole.GetValue(Settings);

        public int Experience => RefExperience.GetValue(Settings);

        private PlayerHelper()
        {
            var playerType = typeof(Player);

            if (EFTVersion.AkiVersion > EFTVersion.Parse("3.9.8"))
            {
                RefSettings = RefHelper.PropertyRef<InfoClass, object>.Create("Settings");
            }
            else
            {
                RefSettings = RefHelper.FieldRef<InfoClass, object>.Create("Settings");
            }

            RefRole = RefHelper.FieldRef<object, WildSpawnType>.Create(RefSettings.RefType, "Role");
            RefExperience = RefHelper.FieldRef<object, int>.Create(RefSettings.RefType, "Experience");
            RefSkills = RefHelper.PropertyRef<Player, object>.Create("Skills");

            Init = RefHelper.HookRef.Create(playerType, "Init");
            Dispose = RefHelper.HookRef.Create(playerType, "Dispose");
            OnDead = RefHelper.HookRef.Create(playerType, "OnDead");
            ApplyDamageInfo = RefHelper.HookRef.Create(playerType, "ApplyDamageInfo");
            OnBeenKilledByAggressor = RefHelper.HookRef.Create(playerType, "OnBeenKilledByAggressor");
            OnPhraseTold = RefHelper.HookRef.Create(playerType, "OnPhraseTold");
            SetPropVisibility = RefHelper.HookRef.Create(playerType, "SetPropVisibility");

            if (EFTVersion.AkiVersion > EFTVersion.Parse("3.4.1"))
            {
                _refGetBleedBlock =
                    AccessTools.MethodDelegate<Func<Player, int, bool>>(
                        RefTool.GetEftMethod(playerType, AccessTools.allDeclared,
                            x => x.ReturnType == typeof(bool) && x.ReadMethodBody().ContainsIL(OpCodes.Ldfld,
                                AccessTools.Field(RefSkills.PropertyType, "LightVestBleedingProtection"))));
            }

            if (EFTVersion.IsFika)
            {
                var observedCoopPlayerType = RefTool.GetPluginType(EFTPlugins.FikaCore,
                    "Fika.Core.Coop.Players.ObservedCoopPlayer");

                ObservedCoopApplyShot = RefHelper.HookRef.Create(observedCoopPlayerType, "ApplyShot");
                ObservedCoopOnPhraseTold = RefHelper.HookRef.Create(observedCoopPlayerType, "OnPhraseTold");
            }
        }

        [EFTHelperHook]
        private void Hook()
        {
            Init.Add(this, nameof(OnInit));
        }

        private static async void OnInit(Player __instance, Task __result)
        {
            await __result;

            if (Instance.IsYourPlayer(__instance))
            {
                Instance.Player = __instance;
            }
        }

        public bool CoopGetBleedBlock(Player instance, int colliderType)
        {
            return _refGetBleedBlock(instance, colliderType);
        }

        public bool IsYourPlayer(Player player)
        {
            return EFTVersion.AkiVersion > EFTVersion.Parse("2.3.1") ? player.IsYourPlayer : player.Id == 1;
        }

        public class FirearmControllerData
        {
            private static readonly Lazy<FirearmControllerData> Lazy =
                new Lazy<FirearmControllerData>(() => new FirearmControllerData());

            public static FirearmControllerData Instance => Lazy.Value;

            public Player.FirearmController FirearmController =>
                PlayerHelper.Instance.Player?.HandsController as Player.FirearmController;

            public readonly RefHelper.HookRef InitiateShot;

            private FirearmControllerData()
            {
                var playerFirearmControllerType = typeof(Player.FirearmController);

                InitiateShot = RefHelper.HookRef.Create(playerFirearmControllerType, "InitiateShot");
            }
        }

        public class ArmorComponentData
        {
            private static readonly Lazy<ArmorComponentData> Lazy =
                new Lazy<ArmorComponentData>(() => new ArmorComponentData());

            public static ArmorComponentData Instance => Lazy.Value;

            public readonly RefHelper.HookRef ApplyDamage;

            private ArmorComponentData()
            {
                var armorComponentType = typeof(ArmorComponent);

                ApplyDamage = RefHelper.HookRef.Create(armorComponentType, "ApplyDamage");
            }
        }

        public class RoleData
        {
            private static readonly Lazy<RoleData> Lazy = new Lazy<RoleData>(() => new RoleData());

            public static RoleData Instance => Lazy.Value;

            private readonly Func<WildSpawnType, bool> _refIsBoss;

            private readonly Func<WildSpawnType, bool> _refIsFollower;

            private readonly Func<WildSpawnType, bool> _refIsBossOrFollower;

            private readonly Func<WildSpawnType, string> _refGetScavRoleKey;

            private RoleData()
            {
                const BindingFlags flags = BindingFlags.Static | RefTool.Public;

                var roleType = RefTool.GetEftType(x =>
                    x.GetMethod("IsBoss", flags) != null && x.GetMethod("Init", flags) != null);

                _refIsBoss = AccessTools.MethodDelegate<Func<WildSpawnType, bool>>(roleType.GetMethod("IsBoss", flags));

                _refIsFollower =
                    AccessTools.MethodDelegate<Func<WildSpawnType, bool>>(roleType.GetMethod("IsFollower", flags));

                _refIsBossOrFollower =
                    AccessTools.MethodDelegate<Func<WildSpawnType, bool>>(roleType.GetMethod("IsBossOrFollower",
                        flags));

                _refGetScavRoleKey =
                    AccessTools.MethodDelegate<Func<WildSpawnType, string>>(roleType.GetMethod("GetScavRoleKey",
                        flags));
            }

            public bool IsBoss(WildSpawnType role)
            {
                return _refIsBoss(role);
            }

            public bool IsFollower(WildSpawnType role)
            {
                return _refIsFollower(role);
            }

            public bool IsBossOrFollower(WildSpawnType role)
            {
                return _refIsBossOrFollower(role);
            }

            public string GetScavRoleKey(WildSpawnType role)
            {
                return _refGetScavRoleKey(role);
            }
        }

        public class InventoryData
        {
            private static readonly Lazy<InventoryData> Lazy = new Lazy<InventoryData>(() => new InventoryData());

            public static InventoryData Instance => Lazy.Value;

            public object Inventory => RefInventory.GetValue(PlayerHelper.Instance.Player?.Profile);

            public object Equipment => RefEquipment.GetValue(Inventory);

            public object QuestRaidItems =>
                RefQuestRaidItems.GetValue(Inventory);

            public Slot[] EquipmentSlots => RefSlots.GetValue(Equipment);

            public List<object> EquipmentGrids
            {
                get
                {
                    var equipmentSlots = EquipmentSlots;

                    if (equipmentSlots == null)
                        return null;

                    var list = new List<object>();

                    foreach (var slot in new[]
                                 { equipmentSlots[6], equipmentSlots[7], equipmentSlots[8], equipmentSlots[10] })
                    {
                        var gear = slot.ContainedItem;

                        if (gear == null)
                            continue;

                        foreach (var grid in RefGrids.GetValue(gear))
                        {
                            list.Add(grid);
                        }
                    }

                    return list;
                }
            }

            public List<Item> EquipmentItems
            {
                get
                {
                    var equipmentGrids = EquipmentGrids;

                    if (equipmentGrids == null)
                        return null;

                    var list = new List<Item>();

                    foreach (var grid in equipmentGrids)
                    {
                        foreach (var item in RefItems.GetValue(grid))
                        {
                            list.Add(item);
                        }
                    }

                    return list;
                }
            }

            public HashSet<object> EquipmentItemHashSet
            {
                get
                {
                    var itemMongoIDHelper = MongoIDHelper.ItemMongoIDData.Instance;

                    var equipmentGrids = EquipmentGrids;

                    if (equipmentGrids == null)
                        return null;

                    var hashSet = new HashSet<object>();

                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var grid in equipmentGrids)
                    {
                        foreach (var item in RefItems.GetValue(grid))
                        {
                            hashSet.Add(itemMongoIDHelper.RefTemplateId.GetValue(item));
                        }
                    }

                    return hashSet;
                }
            }

            public List<object> QuestRaidItemsGrids
            {
                get
                {
                    var questRaidItems = QuestRaidItems;

                    if (questRaidItems == null)
                        return null;

                    var list = new List<object>();

                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var grid in RefGrids.GetValue(questRaidItems))
                    {
                        list.Add(grid);
                    }

                    return list;
                }
            }

            public List<Item> QuestRaidItemsItems
            {
                get
                {
                    var questRaidItemsGrids = QuestRaidItemsGrids;

                    if (questRaidItemsGrids == null)
                        return null;

                    var list = new List<Item>();

                    foreach (var grid in questRaidItemsGrids)
                    {
                        foreach (var item in RefItems.GetValue(grid))
                        {
                            list.Add(item);
                        }
                    }

                    return list;
                }
            }

            public HashSet<object> QuestRaidItemHashSet
            {
                get
                {
                    var itemMongoIDHelper = MongoIDHelper.ItemMongoIDData.Instance;

                    var questRaidItemsGrids = QuestRaidItemsGrids;

                    if (questRaidItemsGrids == null)
                        return null;

                    var hashSet = new HashSet<object>();

                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var grid in questRaidItemsGrids)
                    {
                        foreach (var item in RefItems.GetValue(grid))
                        {
                            hashSet.Add(itemMongoIDHelper.RefTemplateId.GetValue(item));
                        }
                    }

                    return hashSet;
                }
            }

            /// <summary>
            ///     Profile.Inventory
            /// </summary>
            public readonly RefHelper.FieldRef<Profile, object> RefInventory;

            /// <summary>
            ///     InventoryClass.Equipment
            /// </summary>
            public readonly RefHelper.FieldRef<object, object> RefEquipment;

            /// <summary>
            ///     InventoryClass.QuestRaidItems
            /// </summary>
            public readonly RefHelper.FieldRef<object, object> RefQuestRaidItems;

            /// <summary>
            ///     InventoryClass.Equipment.Slots
            /// </summary>
            public readonly RefHelper.FieldRef<object, Slot[]> RefSlots;

            /// <summary>
            ///     InventoryClass.Equipment.Slots.Grids
            /// </summary>
            public readonly RefHelper.FieldRef<object, object[]> RefGrids;

            /// <summary>
            ///     InventoryClass.Equipment.Slots.Grids.Items
            /// </summary>
            public readonly RefHelper.PropertyRef<object, IEnumerable<Item>> RefItems;

            private InventoryData()
            {
                RefInventory = RefHelper.FieldRef<Profile, object>.Create("Inventory");
                RefEquipment = RefHelper.FieldRef<object, object>.Create(RefInventory.FieldType, "Equipment");
                RefQuestRaidItems = RefHelper.FieldRef<object, object>.Create(RefInventory.FieldType, "QuestRaidItems");
                RefSlots = RefHelper.FieldRef<object, Slot[]>.Create(RefEquipment.FieldType, "Slots");
                RefGrids = RefHelper.FieldRef<object, object[]>.Create(
                    RefTool.GetEftType(x =>
                        x.GetMethod("TryGetLastForbiddenItem", BindingFlags.DeclaredOnly | RefTool.Public) != null),
                    "Grids");

                RefItems = RefHelper.PropertyRef<object, IEnumerable<Item>>.Create(RefGrids.FieldType.GetElementType(),
                    "Items");
            }
        }

        public class WeaponData
        {
            private static readonly Lazy<WeaponData> Lazy = new Lazy<WeaponData>(() => new WeaponData());

            public static WeaponData Instance => Lazy.Value;

            public Weapon Weapon => FirearmControllerData.Instance.FirearmController?.Item;

            public object CurrentMagazine => GetCurrentMagazine(Weapon);

            public Slot[] Chambers => RefChambers.GetValue(Weapon);

            public Item UnderbarrelWeapon =>
                RefUnderbarrelWeapon?.GetValue(FirearmControllerData.Instance.FirearmController);

            public Animator WeaponAnimator =>
                RefAnimator.GetValue(RefWeaponIAnimator.GetValue(PlayerHelper.Instance.Player));

            public Animator LauncherAnimator =>
                RefAnimator.GetValue(RefUnderbarrelWeaponIAnimator?.GetValue(PlayerHelper.Instance.Player));

            public Slot[] UnderbarrelChambers => RefUnderbarrelChambers?.GetValue(UnderbarrelWeapon);

            public WeaponTemplate UnderbarrelWeaponTemplate =>
                RefUnderbarrelWeaponTemplate?.GetValue(UnderbarrelWeapon);

            public int UnderbarrelChamberAmmoCount => RefUnderbarrelChamberAmmoCount?.GetValue(UnderbarrelWeapon) ?? 0;

            private readonly Func<Weapon, object> _refGetCurrentMagazine;

            /// <summary>
            ///     Weapon.Chambers
            /// </summary>
            public readonly RefHelper.IRef<Weapon, Slot[]> RefChambers;

            /// <summary>
            ///     Player.FirearmController.UnderbarrelWeapon
            /// </summary>
            [CanBeNull] public readonly RefHelper.FieldRef<Player.FirearmController, Item> RefUnderbarrelWeapon;

            /// <summary>
            ///     Player.ArmsAnimatorCommon
            /// </summary>
            public readonly RefHelper.PropertyRef<Player, object> RefWeaponIAnimator;

            /// <summary>
            ///     Player.UnderbarrelWeaponArmsAnimator
            /// </summary>
            [CanBeNull] public readonly RefHelper.PropertyRef<Player, object> RefUnderbarrelWeaponIAnimator;

            /// <summary>
            ///     IAnimator.Animator
            /// </summary>
            public readonly RefHelper.PropertyRef<object, Animator> RefAnimator;

            /// <summary>
            ///     Player.FirearmController.UnderbarrelWeapon.Chambers
            /// </summary>
            [CanBeNull] public readonly RefHelper.IRef<object, Slot[]> RefUnderbarrelChambers;

            /// <summary>
            ///     Player.FirearmController.UnderbarrelWeapon.WeaponTemplate
            /// </summary>
            [CanBeNull] public readonly RefHelper.PropertyRef<object, WeaponTemplate> RefUnderbarrelWeaponTemplate;

            /// <summary>
            ///     Player.FirearmController.UnderbarrelWeapon.ChamberAmmoCount
            /// </summary>
            [CanBeNull] public readonly RefHelper.PropertyRef<object, int> RefUnderbarrelChamberAmmoCount;

            private WeaponData()
            {
                var weaponType = typeof(Weapon);

                _refGetCurrentMagazine =
                    AccessTools.MethodDelegate<Func<Weapon, object>>(
                        weaponType.GetMethod("GetCurrentMagazine", RefTool.Public));

                RefWeaponIAnimator = RefHelper.PropertyRef<Player, object>.Create("ArmsAnimatorCommon");
                RefAnimator = RefHelper.PropertyRef<object, Animator>.Create(
                    RefTool.GetEftType(x =>
                        x.GetMethod("CreateAnimatorStateInfoWrapper", RefTool.Public | BindingFlags.Static) != null),
                    "Animator");

                if (EFTVersion.AkiVersion > EFTVersion.Parse("3.4.1"))
                {
                    RefUnderbarrelWeapon =
                        RefHelper.FieldRef<Player.FirearmController, Item>.Create("UnderbarrelWeapon");
                    RefUnderbarrelWeaponIAnimator =
                        RefHelper.PropertyRef<Player, object>.Create("UnderbarrelWeaponArmsAnimator");

                    var launcherType =
                        RefTool.GetEftType(x => x.GetMethod("GetCenterOfImpact", RefTool.Public) != null);

                    if (EFTVersion.AkiVersion > EFTVersion.Parse("3.6.1"))
                    {
                        RefUnderbarrelChambers = RefHelper.PropertyRef<object, Slot[]>.Create(launcherType, "Chambers");
                    }
                    else
                    {
                        RefUnderbarrelChambers = RefHelper.FieldRef<object, Slot[]>.Create(launcherType, "Chambers");
                    }

                    RefUnderbarrelWeaponTemplate =
                        RefHelper.PropertyRef<object, WeaponTemplate>.Create(launcherType, "WeaponTemplate");
                    RefUnderbarrelChamberAmmoCount =
                        RefHelper.PropertyRef<object, int>.Create(launcherType, "ChamberAmmoCount");
                }

                if (EFTVersion.AkiVersion > EFTVersion.Parse("3.6.1"))
                {
                    RefChambers = RefHelper.PropertyRef<Weapon, Slot[]>.Create("Chambers");
                }
                else
                {
                    RefChambers = RefHelper.FieldRef<Weapon, Slot[]>.Create("Chambers");
                }
            }

            public object GetCurrentMagazine(Weapon weapon)
            {
                return _refGetCurrentMagazine(weapon);
            }
        }

        public class DamageInfoData
        {
            private static readonly Lazy<DamageInfoData> Lazy = new Lazy<DamageInfoData>(() => new DamageInfoData());

            public static DamageInfoData Instance => Lazy.Value;

            /// <summary>
            ///     DamageInfo.Player
            /// </summary>
            public readonly RefHelper.FieldRef<object, object> RefPlayer;

            /// <summary>
            ///     DamageInfo.Player.iPlayer
            /// </summary>
            [CanBeNull] public readonly RefHelper.PropertyRef<object, object> RefIPlayer;

            /// <summary>
            ///     DamageInfo.Damage
            /// </summary>
            public readonly RefHelper.FieldRef<object, float> RefDamage;

            /// <summary>
            ///     DamageInfo.Direction
            /// </summary>
            public readonly RefHelper.FieldRef<object, Vector3> RefDirection;

            /// <summary>
            ///     DamageInfo.HitPoint
            /// </summary>
            public readonly RefHelper.FieldRef<object, Vector3> RefHitPoint;

            /// <summary>
            ///     DamageInfo.DidBodyDamage
            /// </summary>
            public readonly RefHelper.FieldRef<object, float> RefDidBodyDamage;

            /// <summary>
            ///     DamageInfo.Weapon
            /// </summary>
            public RefHelper.FieldRef<object, Item> RefWeapon;

            /// <summary>
            ///     DamageInfo.BleedBlock
            /// </summary>
            [CanBeNull] public readonly RefHelper.FieldRef<object, bool> RefBleedBlock;

            private DamageInfoData()
            {
                var damageInfoType = EFTVersion.AkiVersion > EFTVersion.Parse("3.9.8")
                    ? RefTool.GetEftType(x => x.Name == "DamageInfoStruct")
                    : RefTool.GetEftType(x => x.Name == "DamageInfo");

                RefPlayer = RefHelper.FieldRef<object, object>.Create(damageInfoType, "Player");

                if (EFTVersion.AkiVersion > EFTVersion.Parse("3.5.8"))
                {
                    RefIPlayer = RefHelper.PropertyRef<object, object>.Create(RefPlayer.FieldType, "iPlayer");
                }

                RefDamage = RefHelper.FieldRef<object, float>.Create(damageInfoType, "Damage");
                RefDirection = RefHelper.FieldRef<object, Vector3>.Create(damageInfoType, "Direction");
                RefHitPoint = RefHelper.FieldRef<object, Vector3>.Create(damageInfoType, "HitPoint");
                RefDidBodyDamage = RefHelper.FieldRef<object, float>.Create(damageInfoType, "DidBodyDamage");
                RefWeapon = RefHelper.FieldRef<object, Item>.Create(damageInfoType, "Weapon");

                if (EFTVersion.AkiVersion > EFTVersion.Parse("3.4.1"))
                {
                    RefBleedBlock = RefHelper.FieldRef<object, bool>.Create(damageInfoType, "BleedBlock");
                }
            }

            public Player GetPlayer(object damageInfo)
            {
                if (EFTVersion.AkiVersion > EFTVersion.Parse("3.5.8"))
                    return (Player)RefIPlayer?.GetValue(RefPlayer.GetValue(damageInfo));

                return (Player)RefPlayer.GetValue(damageInfo);
            }
        }

        public class SpeakerData
        {
            private static readonly Lazy<SpeakerData> Lazy = new Lazy<SpeakerData>(() => new SpeakerData());

            public static SpeakerData Instance => Lazy.Value;

            /// <summary>
            ///     Player.Speaker
            /// </summary>
            public readonly RefHelper.FieldRef<Player, object> RefSpeaker;

            /// <summary>
            ///     Player.Speaker.Speaking
            /// </summary>
            public readonly RefHelper.FieldRef<object, bool> RefSpeaking;

            /// <summary>
            ///     Player.Speaker.Clip
            /// </summary>
            public readonly RefHelper.FieldRef<object, TaggedClip> RefClip;

            /// <summary>
            ///     Player.Speaker.PlayerVoice
            /// </summary>
            public readonly RefHelper.PropertyRef<object, string> RefPlayerVoice;

            public object Speaker => RefSpeaker.GetValue(PlayerHelper.Instance.Player);

            public bool Speaking => RefSpeaking.GetValue(Speaker);

            public TaggedClip Clip => RefClip.GetValue(Speaker);

            public string PlayerVoice => RefPlayerVoice.GetValue(Speaker);

            private readonly Action<object, EPlayerSide, int, string, bool> _refInit;

            private SpeakerData()
            {
                var playerType = typeof(Player);

                RefSpeaker = RefHelper.FieldRef<Player, object>.Create(playerType, "Speaker");
                RefSpeaking = RefHelper.FieldRef<object, bool>.Create(RefSpeaker.FieldType, "Speaking");
                RefClip = RefHelper.FieldRef<object, TaggedClip>.Create(RefSpeaker.FieldType, "Clip");

                RefPlayerVoice = RefHelper.PropertyRef<object, string>.Create(RefSpeaker.FieldType, "PlayerVoice");

                _refInit = RefHelper.ObjectMethodDelegate<Action<object, EPlayerSide, int, string, bool>>(
                    RefSpeaker.FieldType.GetMethod("Init", RefTool.Public));
            }

            public void Init(object instance, EPlayerSide side, int id, string playerVoice,
                bool registerInSpeakerManager = true)
            {
                _refInit(instance, side, id, playerVoice, registerInSpeakerManager);
            }
        }

        public class HealthControllerData
        {
            private static readonly Lazy<HealthControllerData> Lazy =
                new Lazy<HealthControllerData>(() => new HealthControllerData());

            public static HealthControllerData Instance => Lazy.Value;

            public object HealthController => RefHealthController.GetValue(PlayerHelper.Instance.Player);

            private readonly Func<object, EBodyPart, bool, ValueStruct> _refGetBodyPartHealth;

            /// <summary>
            ///     Player.HealthController
            /// </summary>
            public readonly RefHelper.PropertyRef<Player, object> RefHealthController;

            /// <summary>
            ///     Player.HealthController.Hydration
            /// </summary>
            public readonly RefHelper.PropertyRef<object, ValueStruct> RefHydration;

            /// <summary>
            ///     Player.HealthController.Energy
            /// </summary>
            public readonly RefHelper.PropertyRef<object, ValueStruct> RefEnergy;

            /// <summary>
            ///     Player.HealthController.HealthRate
            /// </summary>
            public readonly RefHelper.PropertyRef<object, float> RefHealthRate;

            /// <summary>
            ///     Player.HealthController.HydrationRate
            /// </summary>
            public readonly RefHelper.PropertyRef<object, float> RefHydrationRate;

            /// <summary>
            ///     Player.HealthController.EnergyRate
            /// </summary>
            public readonly RefHelper.PropertyRef<object, float> RefEnergyRate;

            /// <summary>
            ///     Player.HealthController.IsAlive
            /// </summary>
            public readonly RefHelper.PropertyRef<object, bool> RefIsAlive;

            /// <summary>
            ///     Player.ActiveHealthController.Hydration
            /// </summary>
            public readonly RefHelper.PropertyRef<object, ValueStruct> RefActiveHydration;

            /// <summary>
            ///     Player.ActiveHealthController.Energy
            /// </summary>
            public readonly RefHelper.PropertyRef<object, ValueStruct> RefActiveEnergy;

            private readonly Action<object, float, EBodyPart> _refDoWoundRelapse;

            private readonly Action<object, EBodyPart, float> _refBluntContusion;

            private readonly TryApplySideEffectsDelegate _refTryApplySideEffects;

            public delegate bool TryApplySideEffectsDelegate(object instance, object damage, EBodyPart bodyPart,
                out SideEffectComponent sideEffectComponent);

            /// <summary>
            ///     Fika.Core.Coop.ClientClasses.CoopClientHealthController.ApplyDamage
            /// </summary>
            private readonly Func<object, EBodyPart, float, object, float> _refCoopApplyDamage;

            /// <summary>
            ///     Fika.Core.Coop.ObservedClasses.ObservedHealthController.Store
            /// </summary>
            private readonly Func<object, object, object> _refObservedCoopStore;

            private readonly Type _coopHealthControllerType;

            private readonly Type _activeHealthControllerType;

            public bool IsActiveHealthController => HealthController != null &&
                                                    _activeHealthControllerType.IsInstanceOfType(HealthController);

            public ValueStruct Hydration => IsActiveHealthController
                ? RefActiveHydration.GetValue(HealthController)
                : RefHydration.GetValue(HealthController);

            public ValueStruct Energy => IsActiveHealthController
                ? RefActiveEnergy.GetValue(HealthController)
                : RefEnergy.GetValue(HealthController);

            public float HealthRate => RefHealthRate.GetValue(HealthController);

            public float HydrationRate => RefHydrationRate.GetValue(HealthController);

            public float EnergyRate => RefEnergyRate.GetValue(HealthController);

            public bool IsAlive => RefIsAlive.GetValue(HealthController);

            private HealthControllerData()
            {
                _refGetBodyPartHealth = RefHelper.ObjectMethodDelegate<Func<object, EBodyPart, bool, ValueStruct>>(
                    RefTool.GetEftMethod(x => x.GetMethod("GetBodyPartHealth", RefTool.Public) != null && x.IsInterface,
                        RefTool.Public,
                        x => x.Name == "GetBodyPartHealth"));

                _activeHealthControllerType = RefTool.GetEftType(x =>
                    x.GetMethod("SetDamageCoeff", BindingFlags.DeclaredOnly | RefTool.Public) != null);

                _refDoWoundRelapse =
                    RefHelper.ObjectMethodDelegate<Action<object, float, EBodyPart>>(
                        _activeHealthControllerType.GetMethod("DoWoundRelapse", RefTool.Public));
                _refBluntContusion = RefHelper.ObjectMethodDelegate<Action<object, EBodyPart, float>>(
                    _activeHealthControllerType.GetMethod("BluntContusion", RefTool.Public));
                _refTryApplySideEffects = RefHelper.ObjectMethodDelegate<TryApplySideEffectsDelegate>(
                    _activeHealthControllerType.GetMethod("TryApplySideEffects", RefTool.Public));

                RefHealthController = RefHelper.PropertyRef<Player, object>.Create("HealthController");

                var healthControllerType =
                    RefTool.GetEftType(x => x.GetMethod("RecalculateRegeneration", RefTool.Public) != null);

                RefHydration =
                    RefHelper.PropertyRef<object, ValueStruct>.Create(healthControllerType, "Hydration");
                RefEnergy = RefHelper.PropertyRef<object, ValueStruct>.Create(healthControllerType,
                    "Energy");

                RefHealthRate =
                    RefHelper.PropertyRef<object, float>.Create(RefHealthController.PropertyType, "HealthRate");
                RefHydrationRate =
                    RefHelper.PropertyRef<object, float>.Create(RefHealthController.PropertyType, "HydrationRate");
                RefEnergyRate =
                    RefHelper.PropertyRef<object, float>.Create(RefHealthController.PropertyType, "EnergyRate");
                RefIsAlive = RefHelper.PropertyRef<object, bool>.Create(RefHealthController.PropertyType, "IsAlive");

                RefActiveHydration =
                    RefHelper.PropertyRef<object, ValueStruct>.Create(_activeHealthControllerType, "Hydration");
                RefActiveEnergy = RefHelper.PropertyRef<object, ValueStruct>.Create(_activeHealthControllerType,
                    "Energy");

                if (!EFTVersion.IsFika)
                    return;

                _coopHealthControllerType = RefTool.GetPluginType(EFTPlugins.FikaCore,
                    "Fika.Core.Coop.ClientClasses.CoopClientHealthController");

                _refObservedCoopStore =
                    RefHelper.ObjectMethodDelegate<Func<object, object, object>>(RefTool
                        .GetPluginType(EFTPlugins.FikaCore,
                            "Fika.Core.Coop.ObservedClasses.ObservedHealthController")
                        .GetMethod("Store", RefTool.Public));

                _refCoopApplyDamage =
                    RefHelper.ObjectMethodDelegate<Func<object, EBodyPart, float, object, float>>(
                        _coopHealthControllerType.GetMethod("ApplyDamage", RefTool.Public));
            }

            public ValueStruct GetBodyPartHealth(object instance, EBodyPart bodyPart, bool rounded = false)
            {
                return _refGetBodyPartHealth(instance, bodyPart, rounded);
            }

            public void DoWoundRelapse(object instance, float relapseValue, EBodyPart bodyPart)
            {
                _refDoWoundRelapse(instance, relapseValue, bodyPart);
            }

            public void BluntContusion(object instance, EBodyPart bodyPartType, float absorbed)
            {
                _refBluntContusion(instance, bodyPartType, absorbed);
            }

            public bool TryApplySideEffects(object instance, object damage, EBodyPart bodyPart,
                out SideEffectComponent sideEffectComponent)
            {
                return _refTryApplySideEffects(instance, damage, bodyPart, out sideEffectComponent);
            }

            public float CoopApplyDamage(object instance, EBodyPart bodyPart, float damage, object damageInfo)
            {
                return _refCoopApplyDamage(instance, bodyPart, damage, damageInfo);
            }

            public object CoopHealthControllerCreate(object healthInfo, Player player, object inventoryController,
                object skillManager, bool aiHealth)
            {
                return Activator.CreateInstance(_coopHealthControllerType, healthInfo, player, inventoryController,
                    skillManager, aiHealth);
            }

            public object ObservedCoopStore(object instance, object healthInfo = null)
            {
                return _refObservedCoopStore(instance, healthInfo);
            }
        }

        public class GamePlayerOwnerData
        {
            private static readonly Lazy<GamePlayerOwnerData> Lazy =
                new Lazy<GamePlayerOwnerData>(() => new GamePlayerOwnerData());

            public static GamePlayerOwnerData Instance => Lazy.Value;

            public GamePlayerOwner GamePlayerOwner { get; internal set; }

            private readonly TranslateAxesDelegate _refTranslateAxes;

            public delegate void TranslateAxesDelegate(GamePlayerOwner instance, ref float[] axes);

            private GamePlayerOwnerData()
            {
                var gamePlayerOwnerType = typeof(GamePlayerOwner);

                _refTranslateAxes =
                    AccessTools.MethodDelegate<TranslateAxesDelegate>(
                        gamePlayerOwnerType.GetMethod("TranslateAxes", AccessTools.all));
            }

            public void TranslateAxes(GamePlayerOwner instance, ref float[] axes)
            {
                _refTranslateAxes(instance, ref axes);
            }
        }

        public class MovementContextData
        {
            private static readonly Lazy<MovementContextData> Lazy =
                new Lazy<MovementContextData>(() => new MovementContextData());

            public static MovementContextData Instance => Lazy.Value;

            public object MovementContext => RefMovementContext.GetValue(PlayerHelper.Instance.Player);

            public readonly RefHelper.PropertyRef<Player, object> RefMovementContext;

            /// <summary>
            ///     MovementContext.Rotation
            /// </summary>
            public readonly RefHelper.PropertyRef<object, Vector2> RefRotation;

            private MovementContextData()
            {
                RefMovementContext = RefHelper.PropertyRef<Player, object>.Create("MovementContext");
                RefRotation =
                    RefHelper.PropertyRef<object, Vector2>.Create(RefMovementContext.PropertyType, "Rotation");
            }
        }

        public class QuestControllerData
        {
            private static readonly Lazy<QuestControllerData> Lazy =
                new Lazy<QuestControllerData>(() => new QuestControllerData());

            public static QuestControllerData Instance => Lazy.Value;

            public readonly RefHelper.FieldRef<Player, object> RefQuestController;

            public object QuestController => RefQuestController.GetValue(PlayerHelper.Instance.Player);

            private QuestControllerData()
            {
                RefQuestController = RefHelper.FieldRef<Player, object>.Create("_questController");
            }
        }

        public class InventoryControllerData
        {
            private static readonly Lazy<InventoryControllerData> Lazy =
                new Lazy<InventoryControllerData>(() => new InventoryControllerData());

            public static InventoryControllerData Instance => Lazy.Value;

            public readonly RefHelper.FieldRef<Player, object> RefInventoryController;

            public object InventoryController => RefInventoryController.GetValue(PlayerHelper.Instance.Player);

            private InventoryControllerData()
            {
                RefInventoryController = RefHelper.FieldRef<Player, object>.Create("_inventoryController");
            }
        }

        public class SearchControllerData
        {
            private static readonly Lazy<SearchControllerData> Lazy =
                new Lazy<SearchControllerData>(() => new SearchControllerData());

            public static SearchControllerData Instance => Lazy.Value;

            /// <summary>
            ///     Player.SearchController
            /// </summary>
            [CanBeNull] public readonly RefHelper.PropertyRef<Player, object> RefSearchController;

            public object SearchController => RefSearchController?.GetValue(PlayerHelper.Instance.Player);

            private readonly Func<object, Item, bool> _refIsSearched;

            private SearchControllerData()
            {
                if (EFTVersion.AkiVersion > EFTVersion.Parse("3.9.8"))
                {
                    RefSearchController = RefHelper.PropertyRef<Player, object>.Create("SearchController");

                    var searchControllerType = RefTool.GetEftType(x =>
                        x.IsInterface && x.GetMethod("IsSearched", RefTool.Public) != null);

                    _refIsSearched =
                        RefHelper.ObjectMethodDelegate<Func<object, Item, bool>>(
                            searchControllerType.GetMethod("IsSearched", RefTool.Public));
                }
            }

            public bool GetIsSearched(object instance, Item item)
            {
                return _refIsSearched(instance, item);
            }
        }

        public class InfoClassData
        {
            private static readonly Lazy<InfoClassData> Lazy =
                new Lazy<InfoClassData>(() => new InfoClassData());

            public static InfoClassData Instance => Lazy.Value;

            public RefHelper.IRef<InfoClass, EPlayerSide> RefSide;

            private InfoClassData()
            {
                if (EFTVersion.AkiVersion > EFTVersion.Parse("3.9.8"))
                {
                    RefSide = RefHelper.PropertyRef<InfoClass, EPlayerSide>.Create("Side");
                }
                else
                {
                    RefSide = RefHelper.FieldRef<InfoClass, EPlayerSide>.Create("Side");
                }
            }
        }
    }
}
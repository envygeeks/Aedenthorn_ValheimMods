// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable FieldTypo

using HarmonyLib;
using System.Linq;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System.Reflection;
using UnityEngine;
using BepInEx;

namespace AutoFuel
{
    [
        BepInPlugin(
            "aedenthorn.AutoFuel", "Auto Fuel", "1.2.0"
        )
    ]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        private static BepInExPlugin context;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<float> dropRange;
        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<string> toggleString;
        public static ConfigEntry<float> containerRange;
        public static ConfigEntry<float> fireplaceRange;
        public static ConfigEntry<float> smelterOreRange;
        public static ConfigEntry<float> smelterFuelRange;
        public static ConfigEntry<string> oreDisallowTypes;
        public static ConfigEntry<bool> refuelStandingTorches;
        public static ConfigEntry<int> restrictKilnOutputAmount;
        public static ConfigEntry<string> fuelDisallowTypes;
        public static ConfigEntry<bool> restrictKilnOutput;
        public static ConfigEntry<bool> distributedFilling;
        public static ConfigEntry<bool> refuelWallTorches;
        public static ConfigEntry<bool> refuelFirePits;
        public static ConfigEntry<bool> leaveLastItem;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isOn;
        private static float lastFuel;
        private static int fuelCount;


        // TODO: Swap this to using BepInEx Logging
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log(
                    (pref ? typeof(BepInExPlugin).Namespace + " " : "") + str
                );
        }

        private void Awake()
        {
            context = this;

            // --
            // DropRange
            // Float
            // --
            dropRange = Config.Bind(
                "General", "DropRange", 5f,
                "The maximum range to pull dropped fuel"
            );

            // --
            // FirePlaceRange
            // Float
            // --
            fireplaceRange = Config.Bind(
                "General", "FireplaceRange", 5f,
                "The maximum range to pull fuel from containers for fireplaces"
            );

            // --
            // SmelterOreRange
            // Float
            // --
            smelterOreRange = Config.Bind(
                "General", "SmelterOreRange", 5f,
                "The maximum range to pull fuel from containers for smelters"
            );

            // --
            // SmelterFuelRange
            // Float
            // --
            smelterFuelRange = Config.Bind(
                "General", "SmelterFuelRange", 5f,
                "The maximum range to pull ore from containers for smelters"
            );

            // --
            // FuelDisallowTypes
            // String
            // --
            fuelDisallowTypes = Config.Bind(
                "General", "FuelDisallowTypes", "RoundLog,FineWood",
                "Types of item to disallow as fuel (i.e. anything that " +
                    "is consumed), comma-separated."
            );

            // --
            // OreDisallowTypes
            // String
            // --
            oreDisallowTypes = Config.Bind(
                "General", "OreDisallowTypes", "RoundLog,FineWood",
                "Types of item to disallow as ore (i.e. anything that " +
                    "is transformed), comma-separated)."
            );

            // --
            // ToggleString
            // String
            // --
            toggleString = Config.Bind(
                "General", "ToggleString", "Auto Fuel: {0}",
                "Text to show on toggle. {0} is replaced with true/false"
            );

            // --
            // ToggleKey
            // String
            // --
            toggleKey = Config.Bind(
                "General", "ToggleKey", "",
                "Key to toggle behaviour. Leave blank to " +
                    "disable the toggle key."
            );

            // --
            // RefuelStandingTorches
            // Bool
            // --
            refuelStandingTorches = Config.Bind(
                "General", "RefuelStandingTorches", true,
                "Refuel standing torches"
            );

            // --
            // RefuelWallTorches
            // Bool
            // --
            refuelWallTorches = Config.Bind(
                "General", "RefuelWallTorches", true,
                "Refuel wall torches"
            );

            // --
            // RefuelFirePits
            // Bool
            // --
            refuelFirePits = Config.Bind(
                "General", "RefuelFirePits", true,
                "Refuel fire pits"
            );

            // --
            // RestrictKilnOutput
            // Bool
            // --
            restrictKilnOutput = Config.Bind(
                "General", "RestrictKilnOutput", false,
                "Restrict kiln output"
            );

            // --
            // RestrictKilnOutputAmount
            // Int
            // --
            restrictKilnOutputAmount = Config.Bind(
                "General", "RestrictKilnOutputAmount", 10,
                "Amount of coal to shut off kiln fueling"
            );

            // --
            // isOn
            // Bool
            // --
            isOn = Config.Bind(
                "General", "IsOn", true,
                "Behaviour is currently on or not"
            );

            // --
            // DistributedFilling
            // Bool
            // --
            distributedFilling = Config.Bind(
                "General", "distributedFueling", false,
                "If true, refilling will occur one piece of " +
                "fuel or ore at a time, making filling take longer " +
                "but be better distributed between objects."
            );

            // --
            // LeaveLastItem
            // Bool
            // --
            leaveLastItem = Config.Bind(
                "General", "LeaveLastItem", false,
                "Don't use last of item in chest"
            );

            // --
            // ModEnabled
            // Bool
            // --
            modEnabled = Config.Bind(
                "General", "Enabled", true,
                "Enable this mod"
            );

            // --
            // NexusID
            // Int
            // --
            nexusID = Config.Bind(
                "General", "NexusID", 159,
                "Nexus mod ID for updates"
            );

            if (!modEnabled.Value) return;
            Harmony.CreateAndPatchAll(
                Assembly.GetExecutingAssembly()
            );
        }

        private void Update()
        {
            if (
                !AedenthornUtils.IgnoreKeyPresses(true) &&
                AedenthornUtils.CheckKeyDown(
                    toggleKey.Value
                )
            ) {
                isOn.Value = !isOn.Value;

                Config.Save();
                Player.m_localPlayer.Message(
                    MessageHud.MessageType.Center,
                    string.Format(
                        toggleString.Value, isOn.Value
                    )
                );
            }
        }

        private static string GetPrefabName(string name)
        {
            var num = name.IndexOfAny(
                new[]{'(',' '}
            );

            string result;
            if (num >= 0)
                result = name.Substring(
                    0, num
                );
            else
                result = name;

            return result;
        }

        public static List<Container> GetNearbyContainers(
            Vector3 center,
            float range
        ) {
            try {
                var containers = new List<Container>();
                foreach (var collider in Physics.OverlapSphere(
                    center, Mathf.Max(range, 0),
                    LayerMask.GetMask(
                        new string[] { "piece" }
                    )
                )) {
                    var container = GetContainer(collider.transform);
                    var valid = container?.GetComponent<ZNetView>()?.IsValid();
                    if (container is null || valid != true) continue;
                    if (container.GetInventory() != null)
                        containers.Add(container);
                }

                return containers;
            }
            catch
            {
                // BUG: Log the error
                return new List<Container>();
            }
        }

        private static Container GetContainer(Transform transform)
        {
            while(transform != null)
            {
                var container = transform.GetComponent<Container>();
                if (container != null) return container;
                transform = transform.parent;
            }

            return null;
        }

        [
            HarmonyPatch(
                typeof(Fireplace),
                "UpdateFireplace"
            )
        ]
        private static class FireplaceUpdateFireplacePatch
        {
            [UsedImplicitly]
            private static void Postfix(
                Fireplace __instance, ZNetView ___m_nview
            ) {
                if (
                    !Player.m_localPlayer || !isOn.Value || !___m_nview.IsOwner() ||
                    (__instance.name.Contains("walltorch") && !refuelWallTorches.Value) ||
                    (__instance.name.Contains("groundtorch") && !refuelStandingTorches.Value) ||
                    (__instance.name.Contains("fire_pit") && !refuelFirePits.Value)
                ) return;

                if (Time.time - lastFuel < 0.1)
                {
                    fuelCount++;
                    RefuelTorch(
                        __instance, ___m_nview, fuelCount * 33
                    );
                }
                else
                {
                    fuelCount = 0;
                    lastFuel = Time.time;
                    RefuelTorch(
                        __instance, ___m_nview, 0
                    );
                }
            }
        }

        private static async void RefuelTorch(
            Fireplace fireplace, ZNetView znview, int delay
        ) {
            try
            {
                await Task.Delay(delay);
                if (!modEnabled.Value) return;
                if (!fireplace || !znview || !znview.IsValid()) return;
                var currentFuel = Mathf.CeilToInt(znview.GetZDO().GetFloat("fuel"));
                var maxFuel = (int)(fireplace.m_maxFuel - currentFuel);
                var fireplacePosition = fireplace.transform.position;
                var position = fireplacePosition + Vector3.up;
                var nearbyContainers = GetNearbyContainers(
                    fireplacePosition, fireplaceRange.Value
                );

                // --
                // If there is no reason to fuel
                // there is no reason to stay going
                // why waste CPU cycles?
                // --
                if (0 >= maxFuel)
                    return;

                // --
                // Add Fuel
                // Ground
                // --
                foreach (
                    Collider collider in Physics.OverlapSphere(
                        position, dropRange.Value, LayerMask.GetMask(
                            new string[] { "item" }
                        )
                    )
                )
                {
                    if (collider?.attachedRigidbody)
                    {
                        var item = collider.attachedRigidbody.GetComponent<ItemDrop>();
                        if (item?.GetComponent<ZNetView>()?.IsValid() != true)
                            continue;

                        var name = GetPrefabName(item.gameObject.name);
                        var itemName = item.m_itemData.m_shared.m_name;
                        var fuelItemName = fireplace.m_fuelItem.
                            m_itemData.m_shared.m_name;

                        if (itemName == fuelItemName)
                        {
                            if (
                                // BUG: This should either be /,\s+/ or strip
                                fuelDisallowTypes.Value.Split(',').Contains(
                                    name
                                )
                            ) {
                                continue;
                            }

                            Dbgl($"auto adding fuel {name} from the ground");
                            int amount = Mathf.Min(item.m_itemData.m_stack, maxFuel);
                            maxFuel -= amount;

                            for (int i = 0; i < amount; i++)
                            {
                                if (item.m_itemData.m_stack <= 1)
                                {
                                    if (znview.GetZDO() == null)
                                    {
                                        Destroy(
                                            item.gameObject
                                        );
                                    }
                                    else
                                    {
                                        ZNetScene.instance.Destroy(
                                            item.gameObject
                                        );
                                    }

                                    znview.InvokeRPC("RPC_AddFuel", new object[] { });
                                    if (distributedFilling.Value)
                                        return;
                                    break;
                                }

                                item.m_itemData.m_stack--;
                                znview.InvokeRPC("RPC_AddFuel", new object[] {});
                                Traverse.Create(item).Method("Save").GetValue();
                                if (distributedFilling.Value)
                                    return;
                            }
                        }
                    }
                }

                foreach (var c in nearbyContainers)
                {
                    if (fireplace.m_fuelItem && maxFuel > 0)
                    {
                        var itemList = new List<ItemDrop.ItemData>();
                        c.GetInventory().GetAllItems(
                            fireplace.m_fuelItem.m_itemData.m_shared.m_name,
                            itemList
                        );

                        foreach (var fuelItem in itemList)
                        {
                            if (
                                fuelItem != null && (
                                    !leaveLastItem.Value || fuelItem.m_stack > 1
                                )
                            ) {
                                if (
                                    fuelDisallowTypes.Value.Split(',')
                                        .Contains(
                                            fuelItem.m_dropPrefab.name
                                        )
                                    )
                                    continue;
                                maxFuel--;

                                Dbgl(
                                    $"container at {c.transform.position} " +
                                    $"has {fuelItem.m_stack} {fuelItem.m_dropPrefab.name}, " +
                                    "taking one"
                                );

                                znview.InvokeRPC("RPC_AddFuel", new object[] {});
                                var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                                c.GetInventory().RemoveItem(fireplace.m_fuelItem.m_itemData.m_shared.m_name, 1);
                                typeof(Container).GetMethod("Save", bindingFlags)?.Invoke(c, new object[] { });
                                typeof(Inventory).GetMethod("Changed", bindingFlags)?.Invoke(c.GetInventory(), new object[] { });
                                if (distributedFilling.Value)
                                    return;
                            }
                        }
                    }
                }
            }
            catch
            {
                // --
                // BUG: Log the error
                // insanity
                // --
            }
        }

        [
            HarmonyPatch(
                typeof(Smelter),
                "UpdateSmelter"
            )
        ]
        private static class SmelterFixedUpdatePatch
        {
            [UsedImplicitly]
            private static void Postfix(
                Smelter __instance, ZNetView ___m_nview
            ) {
                if (
                    !Player.m_localPlayer ||
                    !isOn.Value || ___m_nview == null ||
                    !___m_nview.IsOwner()
                ) {
                    return;
                }

                if (Time.time - lastFuel < 0.1)
                {
                    fuelCount++;
                    RefuelSmelter(
                        __instance, ___m_nview,
                        fuelCount * 33
                    );
                }
                else
                {
                    fuelCount = 0;
                    lastFuel = Time.time;
                    RefuelSmelter(
                        __instance, ___m_nview, 0
                    );
                }
            }
        }

        /**
         * TODO: Refactor this so that we can
         *   actually have a maximum amount of fuel,
         *   or at least so that we can have it fill up
         *   a container and then stop entirely
         */
        public static async void RefuelSmelter(
            Smelter __instance, ZNetView ___m_nview, int delay
        ) {
            await Task.Delay(delay);
            if (
                !__instance ||
                !___m_nview ||
                !___m_nview.IsValid() ||
                !modEnabled.Value
            ) {
                return;
            }

            var queueSizeM = Traverse.Create(__instance).Method("GetQueueSize");
            var maxOre = __instance.m_maxOre - queueSizeM.GetValue<int>();
            var maxFuel = __instance.m_maxFuel - Mathf.CeilToInt(
                ___m_nview.GetZDO().GetFloat(
                    "fuel"
                )
            );

            var instanceP = __instance.transform.position;
            var nearbyOreContainers = GetNearbyContainers(instanceP, smelterOreRange.Value);
            var nearbyFuelContainers = GetNearbyContainers(
                instanceP, smelterFuelRange.Value
            );

            if (
                restrictKilnOutput.Value &&
                __instance.name.Contains(
                    "charcoal_kiln"
                )
            ) {
                var mQueueSize = Traverse.Create(__instance).Method("GetQueueSize");
                var outputName = __instance.m_conversion[0].m_to.m_itemData.m_shared.m_name;
                var maxOutput = restrictKilnOutputAmount.Value - mQueueSize.GetValue<int>();
                foreach (var c in nearbyOreContainers)
                {
                    var itemList = new List<ItemDrop.ItemData>();
                    c.GetInventory().GetAllItems(outputName, itemList);
                    foreach (var outputItem in itemList)
                    {
                        if (outputItem != null)
                            maxOutput -= outputItem.m_stack;
                    }
                }

                if (maxOutput < 0) maxOutput = 0;
                if (maxOre > maxOutput)
                    maxOre = maxOutput;
            }

            var fueled = false; var ored = false;
            var position = __instance.transform.position + Vector3.up;
            foreach (
                var collider in Physics.OverlapSphere(
                    position, dropRange.Value, LayerMask.GetMask(
                        new string[] { "item" }
                    )
                )
            ) {
                if (collider?.attachedRigidbody)
                {
                    var item = collider.attachedRigidbody.GetComponent<ItemDrop>();
                    if (item?.GetComponent<ZNetView>()?.IsValid() != true)
                        continue;

                    var name = GetPrefabName(item.gameObject.name);
                    foreach (var itemConversion in __instance.m_conversion)
                    {
                        if (ored) break;
                        if (
                            item.m_itemData.m_shared.m_name ==
                                itemConversion.m_from.m_itemData.m_shared.m_name &&
                            maxOre > 0
                        ) {
                            // BUG: This should either be /,\s+/ or strip
                            if (oreDisallowTypes.Value.Split(',').Contains(name)) continue;
                            int amount = Mathf.Min(item.m_itemData.m_stack, maxOre);
                            maxOre -= amount;

                            for (int i = 0; i < amount; i++)
                            {
                                if (item.m_itemData.m_stack <= 1)
                                {
                                    if (___m_nview.GetZDO() == null)
                                        Destroy(
                                            item.gameObject
                                        );
                                    else
                                        ZNetScene.instance.Destroy(
                                            item.gameObject
                                        );

                                    ___m_nview.InvokeRPC("RPC_AddOre", new object[] { name });
                                    if (distributedFilling.Value)
                                        ored = true;
                                    break;
                                }

                                item.m_itemData.m_stack--;
                                ___m_nview.InvokeRPC("RPC_AddOre", new object[] { name });
                                Traverse.Create(item).Method("Save").GetValue();
                                if (distributedFilling.Value)
                                    ored = true;
                            }
                        }
                    }

                    if (
                        __instance.m_fuelItem &&
                        item.m_itemData.m_shared.m_name ==
                            __instance.m_fuelItem.m_itemData.m_shared.m_name &&
                        maxFuel > 0 && !fueled
                    ) {
                        // BUG: This should either be /,\s+/ or strip
                        if (fuelDisallowTypes.Value.Split(',').Contains(name)) continue;
                        var amount = Mathf.Min(item.m_itemData.m_stack, maxFuel);
                        maxFuel -= amount;

                        for (var i = 0; i < amount; i++)
                        {
                            if (item.m_itemData.m_stack <= 1)
                            {
                                if (___m_nview.GetZDO() == null)
                                    Destroy(
                                        item.gameObject
                                    );
                                else
                                    ZNetScene.instance.Destroy(
                                        item.gameObject
                                    );

                                ___m_nview.InvokeRPC("RPC_AddFuel", new object[] { });
                                if (distributedFilling.Value)
                                    fueled = true;
                                break;

                            }

                            item.m_itemData.m_stack--;
                            ___m_nview.InvokeRPC("RPC_AddFuel", new object[] { });
                            Traverse.Create(item).Method("Save").GetValue();
                            if (distributedFilling.Value)
                            {
                                fueled = true;
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var c in nearbyOreContainers)
            {
                foreach (var itemConversion in __instance.m_conversion)
                {
                    if (ored) break;
                    var itemList = new List<ItemDrop.ItemData>();
                    var itemName = itemConversion.m_from.m_itemData.m_shared.m_name;
                    c.GetInventory().GetAllItems(itemName, itemList);
                    foreach (var oreItem in itemList)
                    {
                        if (
                            oreItem != null &&
                            maxOre > 0 && (
                                !leaveLastItem.Value || oreItem.m_stack > 1
                            )
                        ) {
                            // BUG: This should either be /,\s+/ or strip
                            if (
                                oreDisallowTypes.Value.Split(',').Contains(
                                    oreItem.m_dropPrefab.name
                                )
                            ) continue;
                            maxOre--;

                            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                            ___m_nview.InvokeRPC("RPC_AddOre", new object[] { oreItem.m_dropPrefab?.name });
                            c.GetInventory().RemoveItem(itemConversion.m_from.m_itemData.m_shared.m_name, 1);
                            typeof(Container).GetMethod("Save", bindingFlags).Invoke(c, new object[] { });
                            typeof(Inventory).GetMethod("Changed", bindingFlags).Invoke(
                                c.GetInventory(),
                                new object[] { }
                            );

                            if (distributedFilling.Value)
                            {
                                ored = true;
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var c in nearbyFuelContainers)
            {
                if (
                    !__instance.m_fuelItem ||
                    maxFuel <= 0 || fueled
                )
                {
                    break;
                }

                var itemList = new List<ItemDrop.ItemData>();
                var fuelItemName = __instance.m_fuelItem.m_itemData.m_shared.m_name;
                c.GetInventory().GetAllItems(fuelItemName, itemList);
                foreach (var fuelItem in itemList)
                {
                    if (
                        fuelItem != null && (
                            !leaveLastItem.Value || fuelItem.m_stack > 1
                        )
                    ) {
                        maxFuel--;
                        if (
                            fuelDisallowTypes.Value.Split(',').Contains(
                                fuelItem.m_dropPrefab.name
                            )
                        )
                        {
                            continue;
                        }

                        ___m_nview.InvokeRPC("RPC_AddFuel", new object[] { });
                        var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                        c.GetInventory().RemoveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name, 1);
                        typeof(Container).GetMethod("Save", bindingFlags).Invoke(c, new object[] { });
                        typeof(Inventory).GetMethod("Changed", bindingFlags).Invoke(
                            c.GetInventory(),
                            new object[] { }
                        );

                        if (distributedFilling.Value)
                        {
                            fueled = true;
                            break;
                        }
                    }
                }
            }
        }

        [
            HarmonyPatch(
                typeof(Terminal),
                "InputText"
            )
        ]
        private static class InputTextPatch
        {
            [UsedImplicitly]
            private static bool Prefix(Terminal __instance)
            {
                if (!modEnabled.Value) return true;
                var text = __instance.m_input.text;
                var ns = typeof(BepInExPlugin).Namespace?.ToLower();
                if (ns != null && text.ToLower().Equals($"{ns} reset"))
                {
                    context.Config.Reload();
                    __instance.AddString(text);
                    __instance.AddString(
                        $"{context.Info.Metadata.Name} config reloaded"
                    );

                    return false;
                }

                return true;
            }
        }
    }
}

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ComeBackEquipment;

[BepInPlugin("HappyCats.ComeBackEquipment", "ComeBackEquipment", "2.0.2")]
public class ComeBackEquipment : BaseUnityPlugin
{
    internal static ComeBackEquipment Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }
    internal static ConfigEntry<bool> TeleportItemsToTruck { get; private set; } = null!;
    internal static ConfigEntry<bool> TeleportHaulersToTruck { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;

        TeleportItemsToTruck = Config.Bind(
            "General",
            "Teleport Items To Truck",
            false,
            "When a dead player loses equipment in a death pit, return the item to the truck instead of bouncing it back out.");

        TeleportHaulersToTruck = Config.Bind(
            "General",
            "Teleport Haulers To Truck",
            false,
            "When a hauler falls into a death pit, return it to the truck instead of letting it be destroyed.");
        
        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    private void Update()
    {
        // Code that runs every frame goes here
    }
}
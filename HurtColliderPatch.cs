using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace ComeBackEquipment;

[HarmonyPatch]
internal static class HurtColliderPatch
{
    private static bool IsHaulerVehicle(PhysGrabObject physGrabObject)
    {
        ItemVehicle? itemVehicle = physGrabObject.GetComponentInParent<ItemVehicle>();
        return itemVehicle?.valuableBox != null;
    }

    private static bool TryTeleportToTruck(PhysGrabObject physGrabObject)
    {
        if (!TruckSafetySpawnPoint.instance)
        {
            return false;
        }

        physGrabObject.rb.velocity = Vector3.zero;
        physGrabObject.rb.angularVelocity = Vector3.zero;
        physGrabObject.Teleport(TruckSafetySpawnPoint.instance.transform.position, TruckSafetySpawnPoint.instance.transform.rotation);
        physGrabObject.DeathPitEffectCreate();
        return true;
    }

    private static bool ShouldTeleportToTruck(PhysGrabObject physGrabObject)
    {
        if (physGrabObject.GetComponentInParent<ItemVehicle>())
        {
            return ComeBackEquipment.TeleportHaulersToTruck.Value;
        }

        return physGrabObject.GetComponentInParent<ItemEquippable>() != null && ComeBackEquipment.TeleportItemsToTruck.Value;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "Start")]
    private static void ImpactDetectorStartPostfix(PhysGrabObjectImpactDetector __instance)
    {
        bool isEquippable = __instance.GetComponentInParent<ItemEquippable>() != null;
        bool isHaulerVehicle = __instance.GetComponentInParent<ItemVehicle>() != null;

        if (!isEquippable && !isHaulerVehicle)
        {
            return;
        }

        if (isHaulerVehicle && ComeBackEquipment.TeleportHaulersToTruck.Value)
        {
            __instance.destroyDisable = true;
            return;
        }

        if (!isEquippable)
        {
            return;
        }

        if (__instance.GetComponentInParent<ItemGrenade>() != null
            || __instance.GetComponentInParent<ItemMine>() != null
            || __instance.GetComponentInParent<ItemReviveItem>() != null
            || __instance.GetComponentInParent<ItemHealthPack>() != null)
        {
            return;
        }

        __instance.destroyDisable = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemEquippable), "RPC_CompleteUnequip")]
    private static void RPCCompleteUnequipPostfix(ItemEquippable __instance, int physGrabberPhotonViewID, Vector3 teleportPos, bool isForceUnequip)
    {
        if (!isForceUnequip || !__instance)
        {
            return;
        }

        PhysGrabber? physGrabber = null;
        if (SemiFunc.IsMultiplayer())
        {
            physGrabber = PhotonView.Find(physGrabberPhotonViewID)?.GetComponent<PhysGrabber>();
        }
        else
        {
            physGrabber = PhysGrabber.instance;
        }

        if (physGrabber?.playerAvatar?.deadSet != true)
        {
            return;
        }

        PhysGrabObject physGrabObject = __instance.GetComponent<PhysGrabObject>();
        if (!physGrabObject || !physGrabObject.rb || physGrabObject.rb.isKinematic || !SemiFunc.IsMasterClientOrSingleplayer())
        {
            return;
        }

        bool isHaulerVehicle = IsHaulerVehicle(physGrabObject);

        if (isHaulerVehicle && ComeBackEquipment.TeleportHaulersToTruck.Value)
        {
            if (TryTeleportToTruck(physGrabObject))
            {
                return;
            }
        }

        if (ComeBackEquipment.TeleportItemsToTruck.Value)
        {
            if (TryTeleportToTruck(physGrabObject))
            {
                return;
            }
        }

        Vector3 randomHorizontal = Random.insideUnitSphere.normalized * 4f;
        randomHorizontal.y = 0f;

        Vector3 launchForce = (Vector3.up * 20f + randomHorizontal) * physGrabObject.rb.mass;
        Vector3 launchTorque = Random.insideUnitSphere.normalized * 0.25f * physGrabObject.rb.mass;

        physGrabObject.rb.AddForce(launchForce, ForceMode.Impulse);
        physGrabObject.rb.AddTorque(launchTorque, ForceMode.Impulse);
        physGrabObject.DeathPitEffectCreate();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HurtCollider), "PhysObjectHurt")]
    private static bool PhysObjectHurtPrefix(HurtCollider __instance, PhysGrabObject physGrabObject, HurtCollider.BreakImpact impact, float hitForce, float hitTorque, bool apply, bool destroyLaunch, ref bool __result, Enemy? enemy = null)
    {
        if (!__instance.deathPit || !SemiFunc.IsMasterClientOrSingleplayer() || !physGrabObject || !physGrabObject.rb)
        {
            return true;
        }

        if (!ShouldTeleportToTruck(physGrabObject))
        {
            return true;
        }

        if (TryTeleportToTruck(physGrabObject))
        {
            __result = true;
            return false;
        }

        return true;
    }
}

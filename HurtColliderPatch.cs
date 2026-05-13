using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace ComeBackEquipment;

[HarmonyPatch]
internal static class HurtColliderPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "Start")]
    private static void ImpactDetectorStartPostfix(PhysGrabObjectImpactDetector __instance)
    {
        if (__instance.GetComponent<ItemEquippable>() != null)
        {
            __instance.destroyDisable = true;
        }
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

        physGrabObject.rb.velocity = Vector3.zero;
        physGrabObject.rb.angularVelocity = Vector3.zero;

        Vector3 randomHorizontal = Random.insideUnitSphere.normalized * 4f;
        randomHorizontal.y = 0f;

        Vector3 launchForce = (Vector3.up * 20f + randomHorizontal) * physGrabObject.rb.mass;
        Vector3 launchTorque = Random.insideUnitSphere.normalized * 0.25f * physGrabObject.rb.mass;

        physGrabObject.rb.AddForce(launchForce, ForceMode.Impulse);
        physGrabObject.rb.AddTorque(launchTorque, ForceMode.Impulse);
        physGrabObject.DeathPitEffectCreate();
    }
}

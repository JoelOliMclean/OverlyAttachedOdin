using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace OverlyAttachedOdin
{
    [BepInPlugin("uk.co.oliapps.valheim.overlyattachedodin", "Overly Attached Odin", "0.0.1")]
    public class OverlyAttachedOdin : BaseUnityPlugin
	{
		private static bool spawnedOnPlayer;
		public void Awake() => Harmony.CreateAndPatchAll(typeof(OverlyAttachedOdin), null);

		[HarmonyPatch(typeof(Odin), "Awake")]
		[HarmonyPostfix]
		public static void Awake(ref Odin __instance)
		{
			__instance.m_despawnCloseDistance = 10f;
			__instance.m_despawnFarDistance = 30f;
		}

		[HarmonyPatch(typeof(Odin), "Update")]
		[HarmonyPrefix]
		public static bool Update(ref Odin __instance)
		{
			ZNetView m_nview = (ZNetView)AccessTools.Field(typeof(Odin), "m_nview").GetValue(__instance);
			if (!m_nview.IsOwner())
			{
				return false;
			}
			Player closestPlayer = Player.GetClosestPlayer(__instance.transform.position, __instance.m_despawnFarDistance);
			if (closestPlayer == null)
			{
				DespawnOdin(ref __instance, ref m_nview);
				return false;
			}
			float distanceToPlayer = Vector3.Distance(__instance.transform.position, closestPlayer.transform.position);
			Vector3 forward = closestPlayer.transform.position - __instance.transform.position;
			forward.y = 0f;
			forward.Normalize();
			if (distanceToPlayer > __instance.m_despawnFarDistance)
			{
				__instance.transform.position = Vector3.MoveTowards(__instance.transform.position, __instance.transform.position + (forward.normalized * 50f), 0.1f);
			}
			else if (distanceToPlayer < __instance.m_despawnCloseDistance) 
			{
				DespawnOdin(ref __instance, ref m_nview);
				return false;
			} 
			else
			{
				__instance.transform.position = Vector3.MoveTowards(__instance.transform.position, __instance.transform.position + (-forward.normalized * 50f), 0.1f);
			}
			__instance.transform.rotation = Quaternion.LookRotation(forward);
			return false;
		}
		
		private static void DespawnOdin(ref Odin __instance, ref ZNetView m_nview)
        {
			__instance.m_despawn.Create(__instance.transform.position, __instance.transform.rotation, null, 1f);
			AccessTools.Method(typeof(ZNetView), "Destroy").Invoke(m_nview, null);
		}

		private static bool TimeoutOdin(ref Odin __instance, ref ZNetView m_nview)
		{
			float m_time = (float)AccessTools.Field(typeof(Odin), "m_time").GetValue(__instance);
			m_time += Time.deltaTime;
			if (m_time > __instance.m_ttl)
			{
				__instance.m_despawn.Create(__instance.transform.position, __instance.transform.rotation, null, 1f);
				spawnedOnPlayer = false;
				m_nview.Destroy();
				ZLog.Log("timeout " + m_time + " , despawning");
				return false;
			}
			AccessTools.Field(typeof(Odin), "m_time").SetValue(__instance, m_time);
			return true;
		}
	}
}

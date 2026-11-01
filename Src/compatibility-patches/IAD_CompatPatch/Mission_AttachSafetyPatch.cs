using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace MissileAttachSafety
{
    [HarmonyPatch]
    public static class Mission_AttachSafetyPatch
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Logs",
            "missile_attach_safety.log");

        static MethodBase TargetMethod()
        {
            // We patch by name to avoid signature brittleness across versions.
            // This method exists in Bannerlord 1.4.5.
            MethodBase method = null;
            try
            {
                method = AccessTools.Method(typeof(Mission), "CalculateAttachedLocalFrame");
            }
            catch
            {
            }

            SafeLog("TargetMethod lookup Mission.CalculateAttachedLocalFrame => " +
                    (method == null ? "NULL" : method.ToString()));

            return method;
        }

        // IMPORTANT: use object for hitEntity so we don't need TaleWorlds.Engine.GameEntity reference.
        static bool Prefix(
            ref MatrixFrame attachedGlobalFrame,
            AttackCollisionData collisionData,
            WeaponComponentData missileWeapon,
            Agent affectedAgent,
            object hitEntity,
            Vec3 missileMovementVelocity,
            Vec3 missileRotationSpeed,
            MatrixFrame shieldGlobalFrame,
            bool shouldMissilePenetrate,
            ref bool isAttachedFrameLocal)
        {
            try
            {
                // If weapon data is missing, attachment calculations can nullref.
                if (missileWeapon == null)
                {
                    isAttachedFrameLocal = false;
                    SafeLog("Skipped CalculateAttachedLocalFrame: missileWeapon == null");
                    return false;
                }

                // In some edge cases both targets are null; skip attachment.
                if (hitEntity == null && affectedAgent == null)
                {
                    isAttachedFrameLocal = false;
                    SafeLog("Skipped CalculateAttachedLocalFrame: hitEntity and affectedAgent are null");
                    return false;
                }

                return true; // allow vanilla attachment logic
            }
            catch (Exception ex)
            {
                isAttachedFrameLocal = false;
                SafeLog("Prefix exception, skipped CalculateAttachedLocalFrame: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                SafeLog("Suppressed exception in CalculateAttachedLocalFrame: " + __exception);
                return null;
            }

            return null;
        }

        private static void SafeLog(string msg)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(
                    LogPath,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + msg + Environment.NewLine
                );
            }
            catch
            {
            }
        }
    }
}

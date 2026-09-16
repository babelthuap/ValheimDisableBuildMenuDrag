using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DisableBuildMenuDrag
{
    [BepInPlugin("com.babelthuap.disablebuildmenudrag", "Disable Build Menu Drag", "1.0.1")]
    public class DisableBuildMenuDragPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("com.babelthuap.disablebuildmenudrag");

        public static bool WaitingForMouseReleaseAfterPieceSelect = false;
        public static readonly FieldInfo PlacePressedField = AccessTools.Field(typeof(Player), "m_placePressed");

        private static readonly MethodInfo GetMouseButtonMethod = GetGetMouseButtonMethod();

        void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo("Disable Build Menu Drag Loaded!");
        }

        private static MethodInfo GetGetMouseButtonMethod()
        {
            Type inputType = Type.GetType("UnityEngine.Input, UnityEngine") ?? 
                             Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule");
            return inputType?.GetMethod("GetMouseButton", new[] { typeof(int) });
        }

        public static bool IsLeftMouseHeld()
        {
            if (GetMouseButtonMethod != null)
            {
                try
                {
                    return (bool)GetMouseButtonMethod.Invoke(null, new object[] { 0 });
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }


    // Fire UI button actions immediately on PointerDown
    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnPointerDown))]
    public static class Selectable_OnPointerDown_Patch
    {
        public static void Postfix(Selectable __instance, PointerEventData eventData)
        {
            if (Player.m_localPlayer == null || Hud.instance == null)
                return;

            if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            {
                if (__instance is Button btn)
                {
                    btn.onClick?.Invoke();
                }
            }
        }
    }


    // Intercept piece selection on Player.SetSelectedPiece
    [HarmonyPatch(typeof(Player), nameof(Player.SetSelectedPiece), new Type[] { typeof(Vector2Int) })]
    public static class Player_SetSelectedPiece_Patch
    {
        public static void Postfix(Player __instance)
        {
            DisableBuildMenuDragPlugin.WaitingForMouseReleaseAfterPieceSelect = true;
            if (__instance != null && DisableBuildMenuDragPlugin.PlacePressedField != null)
            {
                DisableBuildMenuDragPlugin.PlacePressedField.SetValue(__instance, false);
            }
        }
    }


    // Block placement execution in Player.UpdatePlacement until key release
    [HarmonyPatch(typeof(Player), "UpdatePlacement")]
    public static class Player_UpdatePlacement_Patch
    {
        public static bool Prefix(Player __instance)
        {
            if (DisableBuildMenuDragPlugin.WaitingForMouseReleaseAfterPieceSelect)
            {
                bool attackHeld = DisableBuildMenuDragPlugin.IsLeftMouseHeld();
                if (attackHeld)
                {
                    if (DisableBuildMenuDragPlugin.PlacePressedField != null)
                    {
                        DisableBuildMenuDragPlugin.PlacePressedField.SetValue(__instance, false);
                    }
                    return false;
                }
                else
                {
                    DisableBuildMenuDragPlugin.WaitingForMouseReleaseAfterPieceSelect = false;
                }
            }
            return true;
        }
    }
}

using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DisableBuildMenuDrag
{
    [BepInPlugin("com.babelthuap.disablebuildmenudrag", "Disable Build Menu Drag", "1.0.2")]
    public class DisableBuildMenuDragPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new("com.babelthuap.disablebuildmenudrag");

        public static bool WaitingForMouseReleaseAfterPieceSelect = false;
        public static readonly FieldInfo PlacePressedField = AccessTools.Field(typeof(Player), "m_placePressed");

        private static readonly MethodInfo GetMouseButtonMethod = GetGetMouseButtonMethod();

        void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo("DisableBuildMenuDrag Loaded!");
        }

        private static MethodInfo GetGetMouseButtonMethod()
        {
            Type inputType = Type.GetType("UnityEngine.Input, UnityEngine") ??
                             Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule");
            return inputType?.GetMethod("GetMouseButton", [typeof(int)]);
        }

        public static bool IsLeftMouseHeld()
        {
            if (GetMouseButtonMethod != null)
            {
                try
                {
                    return (bool)GetMouseButtonMethod.Invoke(null, [0]);
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }


    // Fire build piece selection actions immediately on PointerDown.
    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnPointerDown))]
    public static class Selectable_OnPointerDown_Patch
    {
        public static void Postfix(Selectable __instance, PointerEventData eventData)
        {
            if (Player.m_localPlayer == null || Hud.instance == null)
                return;

            if (!Hud.instance.m_buildHud.activeInHierarchy)
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


    // Don't immediately place piece after selecting it.
    // Part 1: Intercept piece selection.
    [HarmonyPatch(typeof(Player), nameof(Player.SetSelectedPiece), [typeof(Vector2Int)])]
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


    // Don't immediately place piece after selecting it.
    // Part 2: Block placement execution until button release.
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
                    DisableBuildMenuDragPlugin.PlacePressedField?.SetValue(__instance, false);
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

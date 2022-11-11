﻿using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterMouseControls
{
    [BepInPlugin(
        "de.benediktwerner.stacklands.bettermousecontrols",
        PluginInfo.PLUGIN_NAME,
        PluginInfo.PLUGIN_VERSION
    )]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableRightclickDrag;
        public static ConfigEntry<bool> enableDoubleclick;
        public static ConfigEntry<float> doubleclickMaxDelay;
        public static ConfigEntry<float> doubleclickMaxDistance;
        public static ConfigEntry<float> doubleclickRestackRange;

        private void Awake()
        {
            enableRightclickDrag = Config.Bind(
                "General",
                "EnableRightclickDrag",
                true,
                "Enables dragging whole stacks by holding the right mouse button"
            );
            enableDoubleclick = Config.Bind(
                "General",
                "EnableDoubleclickRestack",
                true,
                "Enables restacking same cards by doubleclicking"
            );
            doubleclickMaxDelay = Config.Bind(
                "General",
                "DoubleclickMaxDelay",
                0.5f,
                "How much time (in seconds) can pass at most between two clicks for them to be recognized as a doubleclick"
            );
            doubleclickMaxDistance = Config.Bind(
                "General",
                "DoubleclickMaxDistance",
                5f,
                "How far the mouse can move at most between clicks for them to be recognized as a doubleclick"
            );
            doubleclickRestackRange = Config.Bind(
                "General",
                "DoubleclickRestackRange",
                4f,
                "How far away cards can be to still be pulled onto the stack"
            );

            new ToggleableHarmonySetup(enableRightclickDrag, typeof(RightclickDragPatches));
            new ToggleableHarmonySetup(enableDoubleclick, typeof(DoubleclickRestackPatches));
        }
    }

    public class ToggleableHarmonySetup
    {
        ConfigEntry<bool> enabled;
        Harmony harmony;
        Type patches;

        public ToggleableHarmonySetup(ConfigEntry<bool> enabled, Type patches)
        {
            this.enabled = enabled;
            this.patches = patches;
            if (enabled.Value)
                SyncPatchState();
            enabled.SettingChanged += (_, _) => SyncPatchState();
        }

        void SyncPatchState()
        {
            if (enabled.Value)
            {
                if (harmony == null)
                {
                    harmony = Harmony.CreateAndPatchAll(patches);
                }
            }
            else if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }
    }

    public class DoubleclickRestackPatches
    {
        static float previousClick = 0;
        static Vector2 previousMousePos;
        static Draggable previousDraggable = null;

        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.Update))]
        [HarmonyPostfix]
        public static void HandleDoubleClick(WorldManager __instance)
        {
            if (
                __instance.CanInteract
                && InputController.instance.GetInputBegan(0)
                && !GameCanvas.instance.PositionIsOverUI(
                    InputController.instance.GetInputPosition(0)
                )
            )
            {
                var mouse = Mouse.current.position.ReadValue();
                if (
                    previousDraggable != null
                    && __instance.HoveredDraggable == previousDraggable
                    && (Time.time - previousClick) < Plugin.doubleclickMaxDelay.Value
                    && (previousMousePos - mouse).sqrMagnitude
                        <= Plugin.doubleclickMaxDistance.Value * Plugin.doubleclickMaxDistance.Value
                    && previousDraggable is GameCard root
                    && ((root.Child == null && AllParentsSameCard(root)) || InConflict(root))
                    && root.CardData.CanBeDragged
                )
                {
                    var last = root.GetLeafCard();
                    foreach (var card in __instance.AllCards)
                    {
                        if (
                            card != root
                            && card.MyBoard.IsCurrent
                            && card.Parent == null
                            && !InConflict(card)
                            && SameCard(card, root)
                        )
                        {
                            Vector3 dist = root.transform.position - card.transform.position;
                            dist.y = 0f;
                            if (
                                dist.sqrMagnitude
                                <= Plugin.doubleclickRestackRange.Value
                                    * Plugin.doubleclickRestackRange.Value
                            )
                            {
                                var leaf = GetLeafIfAllSame(card);
                                if (leaf != null)
                                {
                                    last.Child = card;
                                    card.Parent = last;
                                    last = leaf;
                                }
                            }
                        }
                    }
                }
                previousMousePos = mouse;
                previousDraggable = __instance.HoveredDraggable;
                previousClick = Time.time;
            }
        }

        static bool SameCard(GameCard a, GameCard b)
        {
            var ad = a.CardData;
            var bd = b.CardData;
            return ad.Id == bd.Id
                || (ad is Villager && bd is Villager)
                || (ad is Equipable && bd is Equipable);
        }

        static GameCard GetLeafIfAllSame(GameCard card)
        {
            while (card.Child != null)
            {
                if (!SameCard(card, card.Child))
                    return null;
                card = card.Child;
            }
            return card;
        }

        static bool AllParentsSameCard(GameCard card)
        {
            while (card.Parent != null)
            {
                if (!SameCard(card, card.Parent))
                    return false;
                card = card.Parent;
            }
            return true;
        }

        static bool InConflict(GameCard card)
        {
            if (card.CardData is Combatable c)
                return c.InConflict;
            return false;
        }
    }

    public class RightclickDragPatches
    {
        [HarmonyPatch(typeof(InputController), nameof(InputController.GetKey))]
        [HarmonyPostfix]
        public static void ShiftIsPressedIfRightclick(ref bool __result, Key key)
        {
            if (key == Key.LeftShift && Mouse.current.rightButton.isPressed)
                __result = true;
        }

        [HarmonyPatch(typeof(InputController), nameof(InputController.StartedGrabbing))]
        [HarmonyPostfix]
        public static void StartedGrabbing(InputController __instance, ref bool __result)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
                __result = true;
        }

        [HarmonyPatch(typeof(InputController), nameof(InputController.StoppedGrabbing))]
        [HarmonyPostfix]
        public static void StoppedGrabbing(InputController __instance, ref bool __result)
        {
            if (Mouse.current.rightButton.wasReleasedThisFrame)
                __result = true;
        }
    }
}
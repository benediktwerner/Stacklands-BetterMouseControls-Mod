using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterMouseControls
{
    [BepInPlugin("de.benediktwerner.stacklands.bettermousecontrols", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableRightclickDrag;
        public static ConfigEntry<bool> enableDoubleclick;
        public static ConfigEntry<float> doubleclickMaxDelay;
        public static ConfigEntry<float> doubleclickMaxDistance;
        public static ConfigEntry<float> doubleclickRestackRange;

        public static ManualLogSource L;

        private void Awake()
        {
            L = Logger;

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
                    try
                    {
                        harmony = Harmony.CreateAndPatchAll(patches);
                    }
                    catch (Exception e)
                    {
                        Plugin.L.LogError("Patch " + patches + " failed:\n" + e);
                    }
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
                && !GameCanvas.instance.PositionIsOverUI(InputController.instance.GetInputPosition(0))
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
                    && (
                        (root.Child == null && AllParentsSameCard(root))
                        || (root.Parent == null && AllChildrenSameCard(root))
                        || InConflict(root)
                    )
                    && root.CardData.CanBeDragged
                    && !root.IsEquipped
                )
                {
                    var conflict = root.Combatable?.MyConflict;
                    var last = root.GetLeafCard();
                    if (root.Parent != null)
                    {
                        var realRoot = root.GetRootCard();
                        var newLast = root.Parent;
                        newLast.Child = null;
                        root.Parent = null;
                        last.Child = realRoot;
                        realRoot.Parent = last;
                        last = newLast;
                    }
                    var length = root.GetChildCount() + 1;
                    if (length < 30)
                    {
                        foreach (var card in __instance.AllCards)
                        {
                            if (
                                card != root
                                && card.MyBoard.IsCurrent
                                && card.Parent == null
                                && card.CanBeDragged()
                                && !InConflict(card)
                                && SameCard(card, root)
                                && !card.IsEquipped
                            )
                            {
                                Vector3 dist = root.transform.position - card.transform.position;
                                dist.y = 0f;
                                if (
                                    dist.sqrMagnitude
                                    <= Plugin.doubleclickRestackRange.Value * Plugin.doubleclickRestackRange.Value
                                )
                                {
                                    var leaf = GetLeafIfAllSameAndSpace(card, ref length);
                                    if (leaf != null)
                                    {
                                        if (conflict != null)
                                        {
                                            var cards = card.GetChildCards();
                                            conflict.JoinConflict(card.CardData as Combatable);
                                            foreach (var child in cards)
                                                conflict.JoinConflict(child.CardData as Combatable);
                                            length = 0;
                                        }
                                        else
                                        {
                                            last.Child = card;
                                            card.Parent = last;
                                            last = leaf;
                                            if (length >= 30)
                                                break;
                                        }
                                    }
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
            return ad.Id == bd.Id || (ad is Villager && bd is Villager) || (ad is Equipable && bd is Equipable);
        }

        static GameCard GetLeafIfAllSameAndSpace(GameCard card, ref int length)
        {
            var count = 1;
            while (card.Child != null)
            {
                if (!SameCard(card, card.Child) || ++count + length > 30)
                    return null;
                card = card.Child;
            }
            length += count;
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

        static bool AllChildrenSameCard(GameCard card)
        {
            while (card.Child != null)
            {
                if (!SameCard(card, card.Child))
                    return false;
                card = card.Child;
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

        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.Update))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DontStopDragIfRightclicking(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(
                        OpCodes.Callvirt,
                        AccessTools.Method(typeof(InputController), nameof(InputController.StoppedGrabbing))
                    ),
                    new CodeMatch(OpCodes.Stloc_S),
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Or),
                    new CodeMatch(OpCodes.Brtrue),
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(
                        OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(InputController), nameof(InputController.InputCount))
                    ),
                    new CodeMatch(OpCodes.Brfalse)
                )
                .ThrowIfInvalid("Didn't find InputCount == 0 check")
                .Advance(10)
                .Insert(
                    Transpilers.EmitDelegate(() => Mouse.current.rightButton.isPressed),
                    new CodeInstruction(OpCodes.Or)
                )
                .InstructionEnumeration();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.StartDragging))]
        public static void StartDragging(out bool __runOriginal)
        {
            __runOriginal = !Mouse.current.rightButton.wasPressedThisFrame;
        }
    }
}

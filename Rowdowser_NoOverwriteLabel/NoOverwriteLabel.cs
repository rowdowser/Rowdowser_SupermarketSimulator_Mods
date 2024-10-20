using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace Rowdowser_NoOverwriteLabel
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class NoOverwriteLabel : BaseUnityPlugin
    {
        private static readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
        private static ManualLogSource logger = new(MyPluginInfo.PLUGIN_NAME);

        private static ConfigEntry<bool> configEnableDebugging;

        private void Awake()
        {
            Logger.LogInfo($"PluginName: {MyPluginInfo.PLUGIN_NAME}, VersionString: {MyPluginInfo.PLUGIN_VERSION} is loaded.");
            logger = Logger;

            configEnableDebugging = Config.Bind("Debugging", "EnableDebugging", false, "Whether or not to show debugging messages in the console.");

            harmony.PatchAll();
        }

        private static void LogIfDebuggingIsOn(ManualLogSource logger, bool isDebugEnabled, string message)
        {
            if (isDebugEnabled)
            {
                logger.LogInfo($"{DateTime.Now:T}: {message}");
            }
        }

        [HarmonyPatch(typeof(BoxInteraction))]
        private class MyBoxInteraction
        {
            [HarmonyPrefix]
            [HarmonyPatch(methodName: nameof(BoxInteraction.PlaceProductToDisplay))]
            private static bool PreFix_PlaceProductToDisplay(BoxInteraction __instance)
            {
                if (__instance.m_CurrentDisplaySlot == null || __instance.m_Box == null || __instance.m_Box.Product == null || __instance.m_CurrentDisplaySlot.m_Label == null)
                {
                    LogIfDebuggingIsOn(logger, configEnableDebugging.Value, "return true");
                    return true;
                }

                if (__instance.m_Box.Product.ID != __instance.m_CurrentDisplaySlot.ProductID)
                {
                    LogIfDebuggingIsOn(logger, configEnableDebugging.Value, "return false");
                    return false;
                }

                return true;
            }
        }
    }
}
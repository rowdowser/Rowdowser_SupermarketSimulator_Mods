using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MyBox;
using System;

namespace SupermarketSimulator
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class AllLightsTurnOnAutomatically : BaseUnityPlugin
    {
        private static readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
        private static ManualLogSource logger = new(MyPluginInfo.PLUGIN_NAME);

        private ConfigEntry<string> configTimeChangeStore;
        private ConfigEntry<string> configTimeChangeStorage;
        private static ConfigEntry<bool> configEnableDebugging;
        private static UpdateTime updateTimeStore;
        private static UpdateTime updateTimeStorage;

        private void Awake()
        {
            Logger.LogInfo($"PluginName: {MyPluginInfo.PLUGIN_NAME}, VersionString: {MyPluginInfo.PLUGIN_VERSION} is loaded.");
            logger = Logger;

            configEnableDebugging = Config.Bind("Debugging", "EnableDebugging", false,
                "Whether or not to show debugging messages in the console.");
            configTimeChangeStore = Config.Bind("Features", "TimeChange_Store", "18:00",
                "The time to turn on the store lights. Should be in the format hh:mm such as '18:00'.");
            configTimeChangeStorage = Config.Bind("Features", "TimeChange_Storage", "18:00",
                "The time to turn on the storage lights. Should be in the format hh:mm such as '18:00'.");

            if (!DateTime.TryParse(configTimeChangeStore.Value, out DateTime timeChangeStore))
            {
                logger.LogWarning($"Invalid configuration value for {configTimeChangeStore.Definition.Key}: {configTimeChangeStore.Value}");
            }
            else
            {
                updateTimeStore = new UpdateTime(timeChangeStore);
                LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Store light set for values {updateTimeStore.GetDescription()}");
            }

            if (!DateTime.TryParse(configTimeChangeStorage.Value, out DateTime timeChangeStorage))
            {
                logger.LogWarning($"Invalid configuration value for {configTimeChangeStorage.Definition.Key}: {configTimeChangeStorage.Value}");
            }
            else
            {
                updateTimeStorage = new UpdateTime(timeChangeStorage);
                LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Storage light set for for values {updateTimeStorage.GetDescription()}");
            }

            LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Configuration value for {configTimeChangeStore.Definition.Key}: {configTimeChangeStore.Value}");
            LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Configuration value for {configTimeChangeStorage.Definition.Key}: {configTimeChangeStorage.Value}");

            if (updateTimeStore != null || updateTimeStorage != null)
            {
                harmony.PatchAll();
            }
            else
            {
                logger.LogWarning("No valid time values in configuration, skipping plugin.");
            }
        }

        private static void LogIfDebuggingIsOn(ManualLogSource logger, bool isDebugEnabled, string message)
        {
            if (isDebugEnabled)
            {
                logger.LogInfo($"{DateTime.Now:T}: {message}");
            }
        }

        [HarmonyPatch(typeof(DayCycleManager))]
        private class MyDayCycleManager
        {
            /// <summary>
            /// Turn on store and/or storage lights automatically when time from config is reached.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(methodName: nameof(DayCycleManager.UpdateGameTime))]
            public static void PostFix_UpdateGameTime(DayCycleManager __instance)
            {
                if (updateTimeStore?.DoesTimeMatch(__instance) ?? false)
                {
                    bool turnOn_Store = Singleton<StoreLightManager>.Instance.TurnOn;
                    if (!turnOn_Store)
                    {
                        LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Store light automatically turned on for values {updateTimeStore.GetDescription()}");
                        Singleton<StoreLightManager>.Instance.TurnOn = true;
                        Singleton<SFXManager>.Instance.PlaySwitchSFX(turnOn_Store);
                    }
                }

                if (updateTimeStorage?.DoesTimeMatch(__instance) ?? false)
                {
                    bool turnOn_Storage = Singleton<StorageLightManager>.Instance.TurnOn;
                    if (!turnOn_Storage)
                    {
                        LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Storage light automatically turned on for values {updateTimeStorage.GetDescription()}");
                        Singleton<StorageLightManager>.Instance.TurnOn = true;
                        Singleton<SFXManager>.Instance.PlaySwitchSFX(turnOn_Storage);
                    }
                }
            }
        }
    }
}
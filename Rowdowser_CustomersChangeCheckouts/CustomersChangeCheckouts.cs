using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MyBox;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rowdowser_CustomersChangeCheckouts
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class CustomersChangeCheckouts : BaseUnityPlugin
    {
        private static readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
        public static ManualLogSource logger = new(MyPluginInfo.PLUGIN_NAME);

        private static ConfigEntry<bool> configEnableDebugging;
        private static List<CheckoutPriority> orderedCheckoutPriority;

        private void Awake()
        {
            Logger.LogInfo($"PluginName: {MyPluginInfo.PLUGIN_NAME}, VersionString: {MyPluginInfo.PLUGIN_VERSION} is loaded.");
            logger = Logger;

            configEnableDebugging = Config.Bind("Debugging", "EnableDebugging", false,
                "Whether or not to show debugging messages in the console.");
            ConfigEntry<int> configCheckoutPriority_Cashier = Config.Bind("CheckoutPriority", "CheckoutPriority_Cashier", 1,
                "The checkout priority of cashier (non-player) checkouts. Set to 0 to not use priority for (will use vanilla behavior).");
            ConfigEntry<int> configCheckoutPriority_Self = Config.Bind("CheckoutPriority", "CheckoutPriority_SelfCheckout", 2,
                "The checkout priority of self checkouts. Set to 0 to prioritize. Set to 0 to not use priority for (will use vanilla behavior).");
            ConfigEntry<int> configCheckoutPriority_Player = Config.Bind("CheckoutPriority", "CheckoutPriority_Player", 3,
                "The checkout priority of player (non-npc) checkouts. Set to 0 to prioritize. Set to 0 to not use priority for (will use vanilla behavior).");

            List<KeyValuePair<CheckoutPriority, int>> unorderedCheckout =
            [
                new(CheckoutPriority.CashierCheckout, configCheckoutPriority_Cashier.Value),
                new(CheckoutPriority.SelfCheckout, configCheckoutPriority_Self.Value),
                new(CheckoutPriority.PlayerCheckout, configCheckoutPriority_Player.Value)
            ];
            orderedCheckoutPriority = unorderedCheckout.Where(x => x.Value != 0).OrderBy(x => x.Value).Select(x => x.Key).ToList();

            harmony.PatchAll();
        }

        internal enum CheckoutPriority
        {
            CashierCheckout,
            SelfCheckout,
            PlayerCheckout,
        };

        private static void LogIfDebuggingIsOn(ManualLogSource logger, bool isDebugEnabled, string message, UnityEngine.Object unityObject)
        {
            if (isDebugEnabled)
            {
                logger.LogInfo($"{DateTime.Now:T} - [{unityObject.GetInstanceID()} {unityObject.name}]: {message}");
            }
        }

        private static void LogIfDebuggingIsOn(ManualLogSource logger, bool isDebugEnabled, string message)
        {
            if (isDebugEnabled)
            {
                logger.LogInfo($"{DateTime.Now:T}: {message}");
            }
        }

        [HarmonyPatch(typeof(Checkout))]
        private class MyCheckout
        {
            /// <summary>
            /// Ask for up to 2 customers for the checkout if it has less than other checkouts.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(methodName: nameof(Checkout.AskForCustomer))]
            public static void Postfix_AskForCustomer(Checkout __instance)
            {
                Checkout checkoutToStealFrom = Singleton<CheckoutManager>.Instance.m_Checkouts.OrderByDescending(x => x.Customers.Count())
                        .FirstOrDefault(x => x.Customers.Count > 1);
                if (checkoutToStealFrom != null)
                {
                    int currentCustomerCount = __instance.Customers.Count;
                    int customerDiff = checkoutToStealFrom.Customers.Count - currentCustomerCount - 1;//skip person scanning
                    if (currentCustomerCount == 0 || customerDiff > 1)
                    {
                        int numToSteal = customerDiff > 2 ? 2 : customerDiff;
                        LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Stealing a total of {numToSteal} from other checkout", __instance);
                        for (int i = 0; i < numToSteal; i++)
                        {
                            LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Stealing customer {i} from other checkout", __instance);
                            Customer lastCustomer = checkoutToStealFrom.Customers.LastOrDefault();
                            if (lastCustomer != null && checkoutToStealFrom.Customers.Count() > 1)
                            {
                                LogIfDebuggingIsOn(logger, configEnableDebugging.Value, "Customer is changing checkout", lastCustomer);
                                checkoutToStealFrom.Unsubscribe(lastCustomer);
                                __instance.Subscribe(lastCustomer);
                            }
                            else
                            {
                                LogIfDebuggingIsOn(logger, configEnableDebugging.Value, $"Did not end up stealing customer {i} from other checkout");
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CheckoutManager))]
        private class MyCheckoutManager
        {
            /// <summary>
            /// When checkouts are initially setup, order them by priority.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(methodName: nameof(CheckoutManager.SetupCheckouts))]
            public static void Postfix_SetupCheckouts(CheckoutManager __instance)
            {
                __instance.m_Checkouts = OrderCheckoutsByPriority(__instance.m_Checkouts);
            }

            /// <summary>
            /// When a new checkout is added, order them by priority.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(methodName: nameof(CheckoutManager.AddCheckout))]
            public static void Postfix_AddCheckout(CheckoutManager __instance)
            {
                __instance.m_Checkouts = OrderCheckoutsByPriority(__instance.m_Checkouts);
            }

            private static List<Checkout> OrderCheckoutsByPriority(List<Checkout> checkouts)
            {
                LogIfDebuggingIsOn(logger, configEnableDebugging.Value,
                    $"Before modifying checkout order, order is [{string.Join(";", checkouts.Select(x => GetCheckoutDescription(x)))}]");

                IOrderedEnumerable<Checkout> checkoutQuery = checkouts.OrderBy(x => 0);
                foreach (CheckoutPriority checkoutPriority in orderedCheckoutPriority)
                {
                    switch (checkoutPriority)
                    {
                        default:
                            break;

                        case CheckoutPriority.CashierCheckout:
                            checkoutQuery = checkoutQuery.ThenBy(x => x.HasCashier ? 0 : 1);
                            break;

                        case CheckoutPriority.SelfCheckout:
                            checkoutQuery = checkoutQuery.ThenBy(x => x.IsSelfCheckout ? 0 : 1);
                            break;

                        case CheckoutPriority.PlayerCheckout:
                            checkoutQuery = checkoutQuery.ThenBy(x => !x.IsSelfCheckout && !x.HasCashier ? 0 : 1);
                            break;
                    }
                }

                List<Checkout> checkoutsOrdered = checkoutQuery.ToList();
                LogIfDebuggingIsOn(logger, configEnableDebugging.Value,
                        $"After modifying checkout order with priority [{string.Join(";", orderedCheckoutPriority.Select(x => x))}], " +
                        $"order is [{string.Join(";", checkoutsOrdered.Select(x => GetCheckoutDescription(x)))}]");
                return checkoutsOrdered;
            }

            private static string GetCheckoutDescription(Checkout checkout)
            {
                return JsonConvert.SerializeObject(new
                {
                    Id = checkout.GetInstanceID(),
                    checkout.name,
                    checkout.IsSelfCheckout,
                    checkout.HasCashier
                });
            }
        }
    }
}
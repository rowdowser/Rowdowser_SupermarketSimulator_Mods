//using BepInEx;
//using BepInEx.Logging;
//using HarmonyLib;
//using MyBox;
//using ParadoxNotion;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//namespace SupermarketSimulator
//{
//    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
//    public class Plugin : BaseUnityPlugin
//    {
//        private static readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
//        public static ManualLogSource logger = new(MyPluginInfo.PLUGIN_NAME);

//        private void Awake()
//        {
//            Logger.LogInfo($"PluginName: {MyPluginInfo.PLUGIN_NAME}, VersionString: {MyPluginInfo.PLUGIN_VERSION} is loaded.");
//            logger = Logger;

//            harmony.PatchAll();
//        }

        
//        //if customers wait too long, make them drop their products on the floor and leave and be unsatisfied
//        [HarmonyPatch(typeof(Customer))]
//        private class MyCustomer
//        {
//            [HarmonyPrefix]
//            [HarmonyPatch(methodName: nameof(Customer.WaitForAvailableCheckout))]
//            private static void Prefix_WaitForAvailableCheckout(Customer __instance)
//            {
//                LogMessage("Customer is waiting for available checkout.", __instance);
//                __instance.StartCoroutine(DoSomething(__instance));
//            }

//            private static IEnumerator DoSomething(Customer customer)
//            {
//                int totalWait = 5;
//                for (int i = 0; i < totalWait; i++)
//                {
//                    LogMessage($"Customer is waiting for checkout {i + 1} out of {totalWait} times.", customer);
//                    //from Customer.WaitForAvailableCheckout
//                    customer.MoveTo(Singleton<DisplayManager>.Instance.GetRandomDisplaySlot().InteractionPosition);
//                    yield return new WaitForSeconds(UnityEngine.Random.Range(customer.m_WaitingIdleRange.x, customer.m_WaitingIdleRange.y));

//                    if (Singleton<CheckoutManager>.Instance.GetAvailableCheckout != null)
//                    {
//                        LogMessage("Checkout opened up.", customer);
//                        yield break;
//                    }
//                }
//                LogMessage("Customer waited too long and is leaving.", customer);
//                //put the boxes the customer had back on the street, 1 product in a box for now
//                int boxCount = 0;
//                foreach (KeyValuePair<int, int> product in customer.m_ShoppingCart.Products)
//                {
//                    int itemId = product.Key;
//                    int quantity = product.Value;
//                    for (int i = 0; i < quantity; i++)
//                    {
//                        CreateBox(itemId, boxCount++);
//                    }
//                }

//                //todo: have customer say something?
//                ExitStore(customer);
//            }

//            private static void CreateBox(int itemId, int boxCount)
//            {
//                //from DeliveryManager.Delivery()
//                DeliveryManager deliveryManager = Singleton<DeliveryManager>.Instance;
//                Box box = Singleton<BoxGenerator>.Instance.SpawnBox(Singleton<IDManager>.Instance.ProductSO(itemId),
//                     deliveryManager.m_DeliveryPosition.position + Vector3.up * (deliveryManager.space * (float)boxCount), Quaternion.identity, deliveryManager.transform);

//                //from Box.Setup()
//                box.m_Data.ProductID = itemId;
//                box.m_Data.Size = box.m_Data.Product.GridLayoutInBox.boxSize;
//                Singleton<ProductAtlasManager>.Instance.SetLabelData(box.m_Data.ProductID, box.m_ProductIconImage);
//                Singleton<InventoryManager>.Instance.AddBox(box.m_Data);
//                //add 1 product at a time for now
//                box.m_Data.ProductCount = 1;
//                box.m_IconUI.SetActive(true);
//                Singleton<StorageStreet>.Instance.SubscribeBox(box);
//                box.SetOccupy(false, null);
//            }

//            private static void ExitStore(Customer customer)
//            {
//                int pointsToLose = 3;
//                LogMessage($"Removing {pointsToLose} points because of customer waiting too long.", customer);
//                Singleton<StoreLevelManager>.Instance.RemovePoint(pointsToLose);

//                customer.m_IsSatisfiedCustomer = false;
//                customer.IsShopping = false;
//                Singleton<CustomerManager>.Instance.AwaitingCustomers.Remove(customer);

//                customer.StartCoroutine(customer.ExitStore());
//            }

//            private static void LogMessage(string message, Customer customer)
//            {
//                if (true)
//                {
//                    string name = customer.name.CapLength(34).PadRight(34);

//                    logger.LogInfo($"{DateTime.Now:T} - [{customer.GetInstanceID()} {name}]: {message}");
//                }
//            }
//        }
//    }
//}
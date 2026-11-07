using UnityEngine;
using TwoLadles.Kitchen;
using TwoLadles.Core;
using TwoLadles.Networking;
using TwoLadles.UI;

namespace TwoLadles.Core
{
    /// <summary>
    /// Guarantees that even on an empty scene or fresh template,
    /// entering Play Mode instantly instantiates the full Vietnam-Korea
    /// Kitchen environment, game managers, physics colliders, and chefs.
    /// </summary>
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            if (KitchenController.Instance == null)
            {
                Debug.Log("[TwoLadles] AutoBootstrap initializing kitchen environment and singletons...");

                // 1. Create Core Game Managers
                if (GameData.Instance == null)
                {
                    GameObject mgr = new GameObject("GameManagers");
                    Object.DontDestroyOnLoad(mgr);
                    mgr.AddComponent<GameData>();
                    mgr.AddComponent<ProgressionManager>();
                    mgr.AddComponent<ShiftManager>();
                    mgr.AddComponent<NetworkManager>();
                }

                // 2. Create KitchenController
                GameObject kcObj = new GameObject("KitchenController");
                var kc = kcObj.AddComponent<KitchenController>();
                kc.autoBuildKitchenOnStart = true;

                // 3. Create UI Canvas & EventSystem if missing
                if (Object.FindObjectOfType<Canvas>() == null)
                {
                    GameObject canvasObj = new GameObject("Canvas");
                    var canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1280, 720);
                    canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                    if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                    {
                        GameObject esObj = new GameObject("EventSystem");
                        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    }

                    // Kitchen HUD
                    GameObject hudObj = new GameObject("KitchenHUD");
                    hudObj.transform.SetParent(canvasObj.transform, false);
                    hudObj.AddComponent<KitchenHUD>();

                    // Lobby UI
                    GameObject lobbyObj = new GameObject("LobbyUI");
                    lobbyObj.transform.SetParent(canvasObj.transform, false);
                    lobbyObj.AddComponent<LobbyUI>();
                    lobbyObj.SetActive(false);
                }
            }
        }
    }
}

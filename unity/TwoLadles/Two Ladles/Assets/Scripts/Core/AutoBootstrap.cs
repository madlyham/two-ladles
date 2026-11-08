using UnityEngine;
using TwoLadles.Kitchen;
using TwoLadles.Core;
using TwoLadles.Networking;
using TwoLadles.UI;

namespace TwoLadles.Core
{
    /// <summary>
    /// Guarantees that even on an empty scene or pre-populated Editor scene,
    /// entering Play Mode instantly instantiates or verifies the full Vietnam-Korea
    /// Kitchen environment, game managers, physics colliders, UI, and chefs.
    /// </summary>
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Debug.Log("[TwoLadles] AutoBootstrap verifying kitchen environment, managers, and UI...");

            // 1. Create Core Game Managers
            if (GameData.Instance == null)
            {
                GameObject mgr = new GameObject("GameManagers");
                Object.DontDestroyOnLoad(mgr);
                mgr.AddComponent<GameData>();
                mgr.AddComponent<ProgressionManager>();
                mgr.AddComponent<ShiftManager>();
                mgr.AddComponent<NetworkManager>();
                mgr.AddComponent<AudioManager>();
            }
            else if (AudioManager.Instance == null)
            {
                GameData.Instance.gameObject.AddComponent<AudioManager>();
            }

            // 2. Create KitchenController if missing
            if (KitchenController.Instance == null && Object.FindObjectOfType<KitchenController>() == null)
            {
                GameObject kcObj = new GameObject("KitchenController");
                var kc = kcObj.AddComponent<KitchenController>();
                kc.autoBuildKitchenOnStart = true;
            }

            // 3. Create UI Canvas, EventSystem, KitchenHUD & LobbyUI if missing
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = true;
                var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            if (Object.FindObjectOfType<KitchenHUD>() == null)
            {
                GameObject hudObj = new GameObject("KitchenHUD", typeof(RectTransform));
                hudObj.transform.SetParent(canvas.transform, false);
                var hud = hudObj.AddComponent<KitchenHUD>();
                hud.AutoBuildUI();
            }

            if (Object.FindObjectOfType<LobbyUI>() == null)
            {
                GameObject lobbyObj = new GameObject("LobbyUI", typeof(RectTransform));
                lobbyObj.transform.SetParent(canvas.transform, false);
                var lobby = lobbyObj.AddComponent<LobbyUI>();
                lobby.AutoBuildUI();
                lobbyObj.SetActive(true);
            }
        }
    }
}

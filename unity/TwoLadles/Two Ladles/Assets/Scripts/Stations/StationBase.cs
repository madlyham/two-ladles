using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Player;

namespace TwoLadles.Stations
{
    [SelectionBase]
    public abstract class StationBase : MonoBehaviour
    {
        public const float MAX_INTERACT_DISTANCE = 2.6f;

        [Header("Station Identity")]
        public string stationId;
        public string displayName;
        public string zone; // "linh_west", "jiwon_east", or "shared"

        [Header("Current Item State")]
        public KitchenItem heldItem = null;

        protected SpriteRenderer _frontItemRenderer;
        protected SpriteRenderer _barBgRenderer;
        protected SpriteRenderer _barFillRenderer;
        private Transform _applianceSpriteChild;
        private static Sprite _whiteBarSprite;

        public static Sprite GetWhiteBarSprite()
        {
            if (_whiteBarSprite == null)
            {
                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] cols = new Color[16];
                for (int i = 0; i < 16; i++) cols[i] = Color.white;
                tex.SetPixels(cols);
                tex.Apply();
                _whiteBarSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            return _whiteBarSprite;
        }

        protected virtual void LateUpdate()
        {
            SyncSpriteAndColliderPosition();
            RefreshStationVisuals();
        }

        /// <summary>
        /// Ensures that if a user drags the child 'ApplianceSprite' in the Unity Editor,
        /// the parent Station (and its BoxCollider + interaction center) moves with it automatically.
        /// </summary>
        public void SyncSpriteAndColliderPosition()
        {
            if (_applianceSpriteChild == null)
            {
                _applianceSpriteChild = transform.Find("ApplianceSprite");
            }
            if (_applianceSpriteChild != null)
            {
                Vector3 localPos = _applianceSpriteChild.localPosition;
                if (Mathf.Abs(localPos.x) > 0.001f || Mathf.Abs(localPos.z) > 0.001f)
                {
                    transform.position += new Vector3(localPos.x, 0.0f, localPos.z);
                    _applianceSpriteChild.localPosition = new Vector3(0.0f, localPos.y, 0.0f);
                }
            }
        }

        protected void EnsureFrontVisuals()
        {
            if (_frontItemRenderer == null)
            {
                Transform existing = transform.Find("FrontHeldItemSprite");
                if (existing != null)
                {
                    _frontItemRenderer = existing.GetComponent<SpriteRenderer>();
                }
                else
                {
                    GameObject go = new GameObject("FrontHeldItemSprite");
                    go.transform.SetParent(transform, false);
                    // Negative Z (-0.22f) places the ingredient in front towards the camera, high Y above counter surface
                    go.transform.localPosition = new Vector3(0.0f, 0.92f, -0.22f);
                    go.transform.localRotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
                    go.transform.localScale = Vector3.one * 0.72f;
                    _frontItemRenderer = go.AddComponent<SpriteRenderer>();
                    _frontItemRenderer.sortingOrder = 1200; // Always above every table, prep counter, and appliance
                }
            }

            if (_barBgRenderer == null)
            {
                Transform existingBar = transform.Find("StationProgressBarBg");
                if (existingBar != null)
                {
                    _barBgRenderer = existingBar.GetComponent<SpriteRenderer>();
                    Transform fillTr = existingBar.Find("StationProgressBarFill");
                    if (fillTr != null) _barFillRenderer = fillTr.GetComponent<SpriteRenderer>();
                }
                else
                {
                    GameObject bgGo = new GameObject("StationProgressBarBg");
                    bgGo.transform.SetParent(transform, false);
                    bgGo.transform.localPosition = new Vector3(0.0f, 1.48f, -0.25f);
                    bgGo.transform.localRotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
                    bgGo.transform.localScale = new Vector3(1.15f, 0.18f, 1.0f);
                    _barBgRenderer = bgGo.AddComponent<SpriteRenderer>();
                    _barBgRenderer.sprite = GetWhiteBarSprite();
                    _barBgRenderer.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
                    _barBgRenderer.sortingOrder = 1210;

                    GameObject fillGo = new GameObject("StationProgressBarFill");
                    fillGo.transform.SetParent(bgGo.transform, false);
                    fillGo.transform.localPosition = Vector3.zero;
                    fillGo.transform.localRotation = Quaternion.identity;
                    fillGo.transform.localScale = new Vector3(0.94f, 0.68f, 1.0f);
                    _barFillRenderer = fillGo.AddComponent<SpriteRenderer>();
                    _barFillRenderer.sprite = GetWhiteBarSprite();
                    _barFillRenderer.color = new Color(0.25f, 0.95f, 0.45f, 1.0f);
                    _barFillRenderer.sortingOrder = 1212;
                    bgGo.SetActive(false);
                }
            }
        }

        protected void SetProgressBar(bool visible, float normalizedProgress, Color fillColor)
        {
            EnsureFrontVisuals();
            if (_barBgRenderer != null)
            {
                if (_barBgRenderer.gameObject.activeSelf != visible)
                    _barBgRenderer.gameObject.SetActive(visible);

                if (visible && _barFillRenderer != null)
                {
                    float pct = Mathf.Clamp01(normalizedProgress);
                    _barFillRenderer.transform.localScale = new Vector3(0.94f * pct, 0.68f, 1.0f);
                    _barFillRenderer.transform.localPosition = new Vector3(-0.47f * (1.0f - pct), 0.0f, -0.01f);
                    _barFillRenderer.color = fillColor;
                }
            }
        }

        public virtual void RefreshStationVisuals()
        {
            EnsureFrontVisuals();
            if (_frontItemRenderer != null)
            {
                if (heldItem != null && !string.IsNullOrEmpty(heldItem.itemId))
                {
                    _frontItemRenderer.gameObject.SetActive(true);
                    Sprite spr = PlayerController.LoadItemSprite(heldItem.itemId);
                    if (spr != null)
                    {
                        _frontItemRenderer.sprite = spr;
                    }
                    _frontItemRenderer.sortingOrder = 1200;
                }
                else
                {
                    _frontItemRenderer.gameObject.SetActive(false);
                }
            }
        }

        public virtual bool CanInteract(Vector3 playerPosition)
        {
            float dist = Vector3.Distance(transform.position, playerPosition);
            return dist <= MAX_INTERACT_DISTANCE;
        }

        public abstract (bool ok, string reason) InteractPrimary(ref KitchenItem playerItem, int peerId, string characterId);
        public abstract (bool ok, string reason) InteractSecondary(ref KitchenItem playerItem, int peerId, string characterId);
    }
}

using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XicesBetterExperience
{
    internal sealed class AutoRenewContactButton : MonoBehaviour
    {
        private ContactElement contact;
        private RectTransform original;
        private Vector2 originalSize, originalPosition;
        private Button extra;
        private TextMeshProUGUI label;
        private float nextRefresh;
        private bool split;
        private RectState titleState, statusState;
        private bool titleAutoSize;
        private float titleMin, titleMax;

        private sealed class RectState
        {
            internal RectTransform rect;
            private Vector2 min, max, size, position;
            internal RectState(RectTransform value)
            {
                rect = value; min = rect.anchorMin; max = rect.anchorMax; size = rect.sizeDelta; position = rect.anchoredPosition;
            }
            internal void Restore() { if (rect != null) { rect.anchorMin = min; rect.anchorMax = max; rect.sizeDelta = size; rect.anchoredPosition = position; } }
        }

        internal void Init(ContactElement value)
        {
            contact = value;
            Refresh();
        }
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.25f;
            Refresh();
        }
        private void Refresh()
        {
            if (contact == null) return;
            bool visible = AutoRenew.Enabled && AutoRenew.Supported(contact.number);
            if (visible && extra == null) Build();
            if (extra == null) return;
            extra.gameObject.SetActive(visible);
            if (visible != split)
            {
                float width = ((RectTransform)extra.transform).sizeDelta.x + 6f;
                original.sizeDelta = visible ? originalSize - new Vector2(width, 0) : originalSize;
                original.anchoredPosition = visible ? originalPosition - new Vector2(width * original.pivot.x, 0) : originalPosition;
                if (titleState != null)
                {
                    if (visible)
                    {
                        // The native quick-dial button contains BOTH name and status.
                        // Reserve its rightmost 58 pixels for the status, then the new sibling for Auto.
                        titleState.rect.anchorMin = new Vector2(0f, 0.5f);
                        titleState.rect.anchorMax = new Vector2(1f, 0.5f);
                        titleState.rect.sizeDelta = new Vector2(-64f, 20f);
                        titleState.rect.anchoredPosition = new Vector2(-30f, 0);
                        statusState.rect.anchorMin = statusState.rect.anchorMax = new Vector2(1f, 0.5f);
                        statusState.rect.sizeDelta = new Vector2(54f, 20f);
                        statusState.rect.anchoredPosition = new Vector2(-29f, 0);
                        contact.titleTMP.enableAutoSizing = true;
                        contact.titleTMP.fontSizeMin = 10f;
                        contact.titleTMP.fontSizeMax = contact.titleTMP.fontSize;
                    }
                    else RestoreText();
                }
                split = visible;
            }
            if (!visible) return;
            AutoRenew.Validate(contact.number);
            AutoRenew.TryOrder(contact.number);
            label.text = AutoRenew.Text(AutoRenew.Active(contact.number) ? "auto_renew_stop" : "auto_renew_start");
            // Keep the original native readiness/cooldown text current after an order.
            StorePhoneClient client = StorePhoneClient.GetPhoneClientByNumber(contact.number);
            if (client != null && contact.statusTMP != null)
                contact.statusTMP.text = client.phoneState == StorePhoneClient.PhoneState.Cooldown
                    ? LocHelper.GetLocalizedMechanic(client.currentCooldown == 1 ? "mech_contact_cooldown_1" : "mech_contact_cooldown_n", client.currentCooldown)
                    : LocHelper.GetLocalizedMechanic(client.phoneState == StorePhoneClient.PhoneState.Regular ? "mech_contact_ready" : "mech_contact_unknown");
        }
        private void Build()
        {
            Button native = contact.statusTMP == null ? null : contact.statusTMP.GetComponentInParent<Button>();
            if (native == null) native = contact.GetComponentInChildren<Button>(true);
            if (native == null) return;
            original = native.transform as RectTransform;
            if (original == null) return;
            originalSize = original.sizeDelta;
            originalPosition = original.anchoredPosition;
            // In the native prefab the whole 248x24 row is one quick-dial button.
            // Splitting it requires reflowing both labels, not just shrinking its hitbox.
            if (contact.titleTMP != null && contact.titleTMP.transform.IsChildOf(original))
            {
                titleState = new RectState(contact.titleTMP.rectTransform);
                statusState = new RectState(contact.statusTMP.rectTransform);
                titleAutoSize = contact.titleTMP.enableAutoSizing;
                titleMin = contact.titleTMP.fontSizeMin; titleMax = contact.titleTMP.fontSizeMax;
            }
            const float width = 48f;
            GameObject go = new GameObject("XiceAutoRenew", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(original.parent, false);
            rect.anchorMin = original.anchorMin;
            rect.anchorMax = original.anchorMax;
            rect.pivot = new Vector2(0.5f, original.pivot.y);
            // Fixed width even when the native button stretches across its row.
            rect.anchorMin = new Vector2(original.anchorMax.x, original.anchorMin.y);
            rect.sizeDelta = new Vector2(width, originalSize.y);
            rect.anchoredPosition = new Vector2(original.offsetMax.x - width / 2f, originalPosition.y);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            Image image = go.GetComponent<Image>();
            Image sourceImage = native.targetGraphic as Image;
            if (sourceImage != null) { image.sprite = sourceImage.sprite; image.type = sourceImage.type; }
            image.color = new Color(0.38f, 0.08f, 0.08f, 1f);
            extra = go.GetComponent<Button>();
            extra.targetGraphic = image;
            Navigation navigation = extra.navigation;
            navigation.mode = Navigation.Mode.None;
            extra.navigation = navigation;
            extra.onClick.AddListener(delegate { AutoRenew.Toggle(contact.number); Refresh(); });
            GameObject text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform tr = (RectTransform)text.transform;
            tr.SetParent(rect, false);
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(2f, 0); tr.offsetMax = new Vector2(-2f, 0);
            label = text.GetComponent<TextMeshProUGUI>();
            label.font = contact.statusTMP.font;
            label.fontSharedMaterial = contact.statusTMP.fontSharedMaterial;
            label.fontSize = contact.statusTMP.fontSize;
            label.enableAutoSizing = true; label.fontSizeMin = 10f; label.fontSizeMax = contact.statusTMP.fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.3f, 0.3f, 1f);
            label.raycastTarget = false;
        }
        private void OnDestroy()
        {
            if (split && original != null) { original.sizeDelta = originalSize; original.anchoredPosition = originalPosition; }
            RestoreText();
            if (extra != null) Destroy(extra.gameObject);
        }
        private void RestoreText()
        {
            if (titleState == null || contact == null || contact.titleTMP == null) return;
            titleState.Restore(); statusState.Restore();
            contact.titleTMP.enableAutoSizing = titleAutoSize;
            contact.titleTMP.fontSizeMin = titleMin; contact.titleTMP.fontSizeMax = titleMax;
        }

        [HarmonyPatch(typeof(ContactElement), "OnInit")]
        internal static class ContactInit
        {
            static void Postfix(ContactElement __instance)
            {
                if (!AutoRenew.Supported(__instance.number)) return;
                AutoRenewContactButton component = __instance.GetComponent<AutoRenewContactButton>();
                if (component == null) component = __instance.gameObject.AddComponent<AutoRenewContactButton>();
                component.Init(__instance);
            }
        }
    }
}

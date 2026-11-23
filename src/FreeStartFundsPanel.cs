using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XicesBetterExperience
{
    internal sealed class FreeStartFundsPanel : MonoBehaviour
    {
        private static FreeStartFundsPanel current;
        private CustomUIElement slider, input;
        private readonly CustomUIElement[] reputationInputs = new CustomUIElement[5], reputationSliders = new CustomUIElement[5];
        private readonly System.Collections.Generic.List<System.Action> numberCommits = new System.Collections.Generic.List<System.Action>();
        private int amount;
        private float saveAt;
        private bool dirty;
        internal static void CommitCurrent() { if (current != null) current.Commit(); }
        internal static void HideCurrent()
        {
            if (current == null) return;
            current.Commit();
            current.gameObject.SetActive(false);
            current = null;
        }
        private const float PanelWidth = 902, ViewHeight = 264, RowHeight = 44;
        private readonly GameObject[] pages = new GameObject[4];
        private readonly Button[] tabs = new Button[4];

        internal static void Build(NewGameSpecializationFrame frame, CustomUIWindow window)
        {
            RectTransform host = NewGameUIElements.NewRect(frame.bulletsHost, "XiceFreeStartFunds", 0, 0, PanelWidth, 48 + ViewHeight);
            var panel = host.gameObject.AddComponent<FreeStartFundsPanel>();
            current = panel;
            panel.amount = ExperienceSettings.FreeStartCash;
            string[] names = { "free_start_tab_basic", "free_start_tab_supplies", "free_start_tab_contacts", "free_start_tab_events" };
            var headers = new CustomUIBuilder(CustomUIManager.Instance, window, host);
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                headers.AddButton(FreeStartScenario.Text(names[i]), () => panel.SelectTab(index));
                WildNetworkUI.SetRect(headers.Last.Rect, i * 180, 0, 172, 40);
                panel.tabs[i] = headers.Last.button;
            }

            RectTransform basic = panel.NewPage(host, 0, 17);
            var builder = new CustomUIBuilder(CustomUIManager.Instance, window, basic);
            var label = NewGameUIElements.NewText(frame, basic, "label", 16, 0, 176, 40,
                FontLoader.FontRole.Noto_Sans_Normal, 22, NewGameStyle.BULLET_TEXT, TextAlignmentOptions.MidlineLeft, false);
            label.text = FreeStartScenario.Text("free_start_cash_label");
            builder.AddSlider(0, 10000, panel.amount, panel.Slide, true);
            panel.slider = builder.Last;
            WildNetworkUI.SetRect(panel.slider.Rect, 230, 0, 420, 40);
            builder.AddInput("600", panel.amount.ToString(CultureInfo.InvariantCulture), value => panel.Commit(), null, panel.Type);
            panel.input = builder.Last;
            WildNetworkUI.SetRect(panel.input.Rect, 714, 0, 140, 40);
            if (panel.input.input != null) panel.input.input.contentType = TMP_InputField.ContentType.IntegerNumber;
            var unit = NewGameUIElements.NewText(frame, basic, "credits", 856, 0, 22, 40,
                FontLoader.FontRole.Noto_Sans_Normal, 22, NewGameStyle.BULLET_TEXT, TextAlignmentOptions.MidlineLeft, false);
            unit.spriteAsset = frame.startWithLabel.spriteAsset;
            unit.text = GeneralHelper.CreditIcon();
            panel.AddNumber(frame, basic, builder, "free_start_rent_label", RowHeight, 10, 2000,
                () => ExperienceSettings.FreeStartRent, ExperienceSettings.SaveFreeStartRent, true);
            panel.AddNumber(frame, basic, builder, "free_start_rent_growth_label", RowHeight * 2, 0, 100,
                () => ExperienceSettings.FreeStartRentGrowth, ExperienceSettings.SaveFreeStartRentGrowth, false, "%");
            panel.AddNumber(frame, basic, builder, "free_start_property_price_label", RowHeight * 3, 5000, 100000,
                () => ExperienceSettings.FreeStartPropertyPrice, ExperienceSettings.SaveFreeStartPropertyPrice, true);
            panel.AddNumber(frame, basic, builder, "free_start_property_growth_label", RowHeight * 4, 0, 1000,
                () => ExperienceSettings.FreeStartPropertyGrowth, ExperienceSettings.SaveFreeStartPropertyGrowth, true);
            panel.AddNumber(frame, basic, builder, "free_start_property_cap_label", RowHeight * 5, 0, 900000,
                () => ExperienceSettings.FreeStartPropertyCap, ExperienceSettings.SaveFreeStartPropertyCap, true);
            panel.AddNumber(frame, basic, builder, "free_start_retail_markup_label", RowHeight * 6, 0, 100,
                () => ExperienceSettings.FreeStartRetailMarkup, ExperienceSettings.SaveFreeStartRetailMarkup, false, "%");
            panel.AddNumber(frame, basic, builder, "free_start_power_multiplier_label", RowHeight * 7, 0, 10,
                () => ExperienceSettings.FreeStartPowerMultiplier, ExperienceSettings.SaveFreeStartPowerMultiplier, false,
                FreeStartScenario.Text("free_start_multiplier_unit"));
            panel.AddNumber(frame, basic, builder, "free_start_insurance_multiplier_label", RowHeight * 8, 0, 10,
                () => ExperienceSettings.FreeStartInsuranceMultiplier, ExperienceSettings.SaveFreeStartInsuranceMultiplier, false,
                FreeStartScenario.Text("free_start_multiplier_unit"));
            panel.AddNumber(frame, basic, builder, "free_start_vendor_permit_fee_label", RowHeight * 9, 0, 10000,
                () => ExperienceSettings.FreeStartVendorPermitFee, ExperienceSettings.SaveFreeStartVendorPermitFee, true);
            panel.AddNumber(frame, basic, builder, "free_start_appraisal_join_multiplier_label", RowHeight * 10, 0, 10,
                () => ExperienceSettings.FreeStartAppraisalJoinMultiplier, ExperienceSettings.SaveFreeStartAppraisalJoinMultiplier, false,
                FreeStartScenario.Text("free_start_multiplier_unit"));
            panel.AddNumber(frame, basic, builder, "free_start_runner_markup_label", RowHeight * 11, 0, 1000,
                () => ExperienceSettings.FreeStartRunnerMarkup, ExperienceSettings.SaveFreeStartRunnerMarkup, false, "%");
            panel.AddNumber(frame, basic, builder, "free_start_hypertech_markup_label", RowHeight * 12, 0, 1000,
                () => ExperienceSettings.FreeStartHypertechMarkup, ExperienceSettings.SaveFreeStartHypertechMarkup, false, "%");
            panel.AddNumber(frame, basic, builder, "free_start_doctor_markup_label", RowHeight * 13, 0, 1000,
                () => ExperienceSettings.FreeStartDoctorMarkup, ExperienceSettings.SaveFreeStartDoctorMarkup, false, "%");
            panel.AddNumber(frame, basic, builder, "free_start_retired_markup_label", RowHeight * 14, 0, 1000,
                () => ExperienceSettings.FreeStartRetiredMarkup, ExperienceSettings.SaveFreeStartRetiredMarkup, false, "%");
            AddOption(frame, basic, builder, "free_start_force_skip_label", RowHeight * 15,
                ExperienceSettings.FreeStartForceSkip, ExperienceSettings.SaveFreeStartForceSkip);
            AddOption(frame, basic, builder, "free_start_no_skip_supplies_label", RowHeight * 16,
                ExperienceSettings.FreeStartNoSkipSupplies, ExperienceSettings.SaveFreeStartNoSkipSupplies);

            RectTransform supplies = panel.NewPage(host, 1, 12);
            builder = new CustomUIBuilder(CustomUIManager.Instance, window, supplies);
            AddOption(frame, supplies, builder, "free_start_storage_label", 0,
                ExperienceSettings.FreeStartStorage, ExperienceSettings.SaveFreeStartStorage);
            AddOption(frame, supplies, builder, "free_start_smuggler_storage_label", RowHeight,
                ExperienceSettings.FreeStartSmugglerStorage, ExperienceSettings.SaveFreeStartSmugglerStorage);
            AddOption(frame, supplies, builder, "free_start_water_tools_label", RowHeight * 2,
                ExperienceSettings.FreeStartWaterTools, ExperienceSettings.SaveFreeStartWaterTools);
            AddOption(frame, supplies, builder, "free_start_hydroponics_label", RowHeight * 3,
                ExperienceSettings.FreeStartHydroponics, ExperienceSettings.SaveFreeStartHydroponics);
            AddOption(frame, supplies, builder, "free_start_ranching_label", RowHeight * 4,
                ExperienceSettings.FreeStartRanching, ExperienceSettings.SaveFreeStartRanching);
            AddOption(frame, supplies, builder, "free_start_aquaculture_label", RowHeight * 5,
                ExperienceSettings.FreeStartAquaculture, ExperienceSettings.SaveFreeStartAquaculture);
            AddOption(frame, supplies, builder, "free_start_chemistry_label", RowHeight * 6,
                ExperienceSettings.FreeStartChemistry, ExperienceSettings.SaveFreeStartChemistry);
            AddOption(frame, supplies, builder, "free_start_black_lamps_label", RowHeight * 7,
                ExperienceSettings.FreeStartBlackLamps, ExperienceSettings.SaveFreeStartBlackLamps);
            AddOption(frame, supplies, builder, "free_start_food_label", RowHeight * 8,
                ExperienceSettings.FreeStartFood, ExperienceSettings.SaveFreeStartFood);
            AddOption(frame, supplies, builder, "free_start_household_label", RowHeight * 9,
                ExperienceSettings.FreeStartHousehold, ExperienceSettings.SaveFreeStartHousehold);
            AddOption(frame, supplies, builder, "free_start_contraband_label", RowHeight * 10,
                ExperienceSettings.FreeStartContraband, ExperienceSettings.SaveFreeStartContraband);
            AddOption(frame, supplies, builder, "free_start_music_label", RowHeight * 11,
                ExperienceSettings.FreeStartMusic, ExperienceSettings.SaveFreeStartMusic);

            RectTransform contacts = panel.NewPage(host, 2, 16);
            builder = new CustomUIBuilder(CustomUIManager.Instance, window, contacts);
            for (int i = 0; i < 5; i++)
                panel.AddReputation(frame, contacts, builder, i);
            panel.AddNumber(frame, contacts, builder, "free_start_reputation_gain_label", RowHeight * 5, 0, 1000,
                () => ExperienceSettings.FreeStartReputationGain, ExperienceSettings.SaveFreeStartReputationGain, false, "%");
            panel.AddNumber(frame, contacts, builder, "free_start_reputation_loss_label", RowHeight * 6, 0, 1000,
                () => ExperienceSettings.FreeStartReputationLoss, ExperienceSettings.SaveFreeStartReputationLoss, false, "%");
            AddOption(frame, contacts, builder, "free_start_landlord_grace_label", RowHeight * 7,
                ExperienceSettings.FreeStartLandlordGrace, ExperienceSettings.SaveFreeStartLandlordGrace);
            AddOption(frame, contacts, builder, "free_start_exchange_label", RowHeight * 8,
                ExperienceSettings.FreeStartExchange, ExperienceSettings.SaveFreeStartExchange);
            AddOption(frame, contacts, builder, "free_start_water_contact_label", RowHeight * 9,
                ExperienceSettings.FreeStartWaterContact, ExperienceSettings.SaveFreeStartWaterContact);
            AddOption(frame, contacts, builder, "free_start_farmer_contact_label", RowHeight * 10,
                ExperienceSettings.FreeStartFarmerContact, ExperienceSettings.SaveFreeStartFarmerContact);
            AddOption(frame, contacts, builder, "free_start_rancher_contact_label", RowHeight * 11,
                ExperienceSettings.FreeStartRancherContact, ExperienceSettings.SaveFreeStartRancherContact);
            AddOption(frame, contacts, builder, "free_start_aquafarmer_contact_label", RowHeight * 12,
                ExperienceSettings.FreeStartAquafarmerContact, ExperienceSettings.SaveFreeStartAquafarmerContact);
            AddOption(frame, contacts, builder, "free_start_chemist_contact_label", RowHeight * 13,
                ExperienceSettings.FreeStartChemistContact, ExperienceSettings.SaveFreeStartChemistContact);
            AddOption(frame, contacts, builder, "free_start_gunsmith_contact_label", RowHeight * 14,
                ExperienceSettings.FreeStartGunsmithContact, ExperienceSettings.SaveFreeStartGunsmithContact);
            AddOption(frame, contacts, builder, "free_start_gun_permit_label", RowHeight * 15,
                ExperienceSettings.FreeStartGunPermit, ExperienceSettings.SaveFreeStartGunPermit);
            var events = panel.NewPage(host, 3, 2);
            builder = new CustomUIBuilder(CustomUIManager.Instance, window, events);
            panel.AddNumber(frame, events, builder, "free_start_bailouts_label", 0, 0, 10,
                () => ExperienceSettings.FreeStartBailouts, ExperienceSettings.SaveFreeStartBailouts, false, FreeStartScenario.Text("free_start_times_unit"));
            panel.AddNumber(frame, events, builder, "free_start_lou_demand_label", RowHeight, 0, 10000,
                () => ExperienceSettings.FreeStartLouDemand, ExperienceSettings.SaveFreeStartLouDemand, true);
            foreach (var text in host.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.fontSize = 22;
            }
            if (panel.input.input != null) panel.input.input.pointSize = 22;
            panel.SelectTab(0);
        }

        private RectTransform NewPage(RectTransform host, int index, int rows)
        {
            var page = NewGameUIElements.NewRect(host, "page_" + index, 0, 48, PanelWidth, ViewHeight);
            pages[index] = page.gameObject;
            var scroll = page.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            var viewport = NewGameUIElements.NewRect(page, "viewport", 0, 0, PanelWidth - 24, ViewHeight);
            // A raycast surface lets the wheel work over empty space between controls.
            var surface = viewport.gameObject.AddComponent<Image>();
            surface.color = NewGameStyle.WithAlpha(NewGameStyle.BULLET_TEXT, 0.05f);
            surface.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewGameUIElements.NewRect(viewport, "content", 0, 0, PanelWidth - 24,
                Mathf.Max(ViewHeight, rows * RowHeight));
            scroll.viewport = viewport;
            scroll.content = content;
            var track = NewGameUIElements.NewRect(page, "scrollbar", PanelWidth - 18, 0, 16, ViewHeight);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = NewGameStyle.WithAlpha(NewGameStyle.BULLET_TEXT, 0.12f);
            var handle = NewGameUIElements.NewRect(track, "handle", 0, 0, 16, ViewHeight);
            // Scrollbar drives the handle anchors to express its proportional size.
            // Fixed sizeDelta from NewRect would be added to that size, overflowing
            // the track even when ScrollRect supplies the correct visible fraction.
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = NewGameStyle.WithAlpha(NewGameStyle.BULLET_TEXT, 0.6f);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle;
            bar.targetGraphic = handleImage;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.size = ViewHeight / Mathf.Max(ViewHeight, rows * RowHeight);
            bar.value = 1;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalNormalizedPosition = 1;
            AddBoundary(page, "top", 0, 0, PanelWidth, 1);
            AddBoundary(page, "bottom", 0, ViewHeight - 1, PanelWidth, 1);
            AddBoundary(page, "left", 0, 0, 1, ViewHeight);
            AddBoundary(page, "right", PanelWidth - 1, 0, 1, ViewHeight);
            return content;
        }

        private static void AddBoundary(RectTransform parent, string name, float x, float y, float width, float height)
        {
            var edge = NewGameUIElements.NewRect(parent, "border_" + name, x, y, width, height);
            var image = edge.gameObject.AddComponent<Image>();
            image.color = NewGameStyle.WithAlpha(NewGameStyle.BULLET_TEXT, 0.45f);
            image.raycastTarget = false;
        }

        private void SelectTab(int index)
        {
            Commit();
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == index);
                if (tabs[i] != null) tabs[i].interactable = i != index;
            }
        }

        private static void AddOption(NewGameSpecializationFrame frame, RectTransform host, CustomUIBuilder builder,
            string key, float y, bool value, System.Action<bool> save)
        {
            var label = NewGameUIElements.NewText(frame, host, key, 16, y, 410, 40,
                FontLoader.FontRole.Noto_Sans_Normal, 22, NewGameStyle.BULLET_TEXT, TextAlignmentOptions.MidlineLeft, false);
            label.text = FreeStartScenario.Text(key);
            builder.AddToggle("", value, save);
            WildNetworkUI.SetRect(builder.Last.Rect, 450, y, 40, 40);
        }
        private void AddReputation(NewGameSpecializationFrame frame, RectTransform host, CustomUIBuilder builder, int index)
        {
            float y = index * RowHeight;
            var label = NewGameUIElements.NewText(frame, host, "reputation_" + index, 16, y, 260, 40,
                FontLoader.FontRole.Noto_Sans_Normal, 22, NewGameStyle.BULLET_TEXT, TextAlignmentOptions.MidlineLeft, false);
            label.text = FreeStartScenario.Text("free_start_reputation_" + index);
            builder.AddSlider(-200, 200, ExperienceSettings.FreeStartReputation(index), amount =>
            {
                int rounded = Mathf.RoundToInt(amount);
                ExperienceSettings.SaveFreeStartReputation(index, rounded);
                if (reputationInputs[index] != null)
                    reputationInputs[index].SetString(rounded.ToString(CultureInfo.InvariantCulture));
            }, true);
            reputationSliders[index] = builder.Last;
            WildNetworkUI.SetRect(builder.Last.Rect, 300, y, 440, 40);
            builder.AddInput("0", ExperienceSettings.FreeStartReputation(index).ToString(CultureInfo.InvariantCulture),
                value => CommitReputation(index), null, value =>
                {
                    if (string.IsNullOrWhiteSpace(value) || value == "-" || value == "+") return;
                    int parsed = FreeStartRules.ParseReputation(value, ExperienceSettings.FreeStartReputation(index));
                    ExperienceSettings.SaveFreeStartReputation(index, parsed);
                    reputationSliders[index].SetFloat(parsed);
                });
            reputationInputs[index] = builder.Last;
            WildNetworkUI.SetRect(builder.Last.Rect, 754, y, 100, 40);
            if (builder.Last.input != null) builder.Last.input.contentType = TMP_InputField.ContentType.IntegerNumber;
        }
        private void CommitReputation(int index)
        {
            var field = reputationInputs[index];
            if (field == null || !field.IsAlive) return;
            int value = FreeStartRules.ParseReputation(field.GetString(), ExperienceSettings.FreeStartReputation(index));
            ExperienceSettings.SaveFreeStartReputation(index, value);
            field.SetString(value.ToString(CultureInfo.InvariantCulture));
            reputationSliders[index].SetFloat(value);
        }
        private void AddNumber(NewGameSpecializationFrame frame, RectTransform host, CustomUIBuilder builder,
            string key, float y, int minimum, int maximum, System.Func<int> read, System.Action<int> save, bool credits, string suffix = null)
        {
            var label = NewGameUIElements.NewText(frame, host, key, 16, y, 260, 40,
                FontLoader.FontRole.Noto_Sans_Normal, 22, NewGameStyle.BULLET_TEXT, TextAlignmentOptions.MidlineLeft, false);
            label.text = FreeStartScenario.Text(key);
            CustomUIElement field = null;
            builder.AddSlider(minimum, maximum, read(), value =>
            {
                int number = Mathf.RoundToInt(value);
                save(number);
                if (field != null) field.SetString(number.ToString(CultureInfo.InvariantCulture));
            }, true);
            var numberSlider = builder.Last;
            WildNetworkUI.SetRect(numberSlider.Rect, 300, y, 440, 40);
            System.Action commit = () =>
            {
                if (field == null || !field.IsAlive) return;
                int value = FreeStartRules.ParseRange(field.GetString(), read(), minimum, maximum);
                save(value);
                field.SetString(value.ToString(CultureInfo.InvariantCulture));
                numberSlider.SetFloat(value);
            };
            builder.AddInput(read().ToString(CultureInfo.InvariantCulture), read().ToString(CultureInfo.InvariantCulture),
                value => commit(), null, value =>
                {
                    if (string.IsNullOrWhiteSpace(value) || value == "-" || value == "+") return;
                    int number = FreeStartRules.ParseRange(value, read(), minimum, maximum);
                    save(number);
                    numberSlider.SetFloat(number);
                });
            field = builder.Last;
            WildNetworkUI.SetRect(field.Rect, 754, y, 100, 40);
            if (field.input != null) field.input.contentType = TMP_InputField.ContentType.IntegerNumber;
            numberCommits.Add(commit);
            if (credits || suffix != null)
            {
                var unit = NewGameUIElements.NewText(frame, host, key + "_unit", 856, y, 22, 40,
                    FontLoader.FontRole.Noto_Sans_Normal, 22, NewGameStyle.BULLET_TEXT, TextAlignmentOptions.MidlineLeft, false);
                unit.spriteAsset = frame.startWithLabel.spriteAsset;
                unit.text = credits ? GeneralHelper.CreditIcon() : suffix;
            }
        }
        private void Slide(float value)
        {
            amount = FreeStartRules.Clamp(Mathf.RoundToInt(value));
            if (input != null && input.IsAlive) input.SetString(amount.ToString(CultureInfo.InvariantCulture));
            dirty = true;
            saveAt = Time.unscaledTime + 0.35f;
        }
        private void Type(string value)
        {
            // Keep incomplete editing (empty text or a sign) intact until commit.
            if (string.IsNullOrWhiteSpace(value) || value == "-" || value == "+") return;
            amount = FreeStartRules.Parse(value, amount);
            if (slider != null && slider.IsAlive) slider.SetFloat(amount);
            dirty = true;
            saveAt = Time.unscaledTime + 0.35f;
        }
        private void Update() { if (dirty && Time.unscaledTime >= saveAt) Save(); }
        private void Save() { ExperienceSettings.SaveFreeStartCash(amount); dirty = false; }
        private void Commit()
        {
            foreach (var commit in numberCommits) commit();
            for (int i = 0; i < reputationInputs.Length; i++) CommitReputation(i);
            if (input != null && input.IsAlive) amount = FreeStartRules.Parse(input.GetString(), amount);
            if (slider != null && slider.IsAlive) slider.SetFloat(amount);
            if (input != null && input.IsAlive) input.SetString(amount.ToString(CultureInfo.InvariantCulture));
            Save();
        }
        private void OnDisable() { Commit(); }
        private void OnDestroy()
        {
            if (dirty) Save();
            if (current == this) current = null;
        }
    }
}

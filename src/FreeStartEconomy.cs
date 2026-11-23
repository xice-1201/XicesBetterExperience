using System;
using HarmonyLib;

namespace XicesBetterExperience
{
    internal static class FreeStartEconomy
    {
        private const string LouKey = "free_start_lou_demand";
        private static int cachedLou;
        private const string GainKey = "free_start_reputation_gain";
        private const string LossKey = "free_start_reputation_loss";
        private static int cachedGain, cachedLoss;
        private const string RentKey = "free_start_rent_growth";
        private const string MarkupKey = "free_start_retail_markup";
        private const string GraceKey = "free_start_landlord_grace";
        private const string PowerKey = "free_start_power_multiplier";
        private const string InsuranceKey = "free_start_insurance_multiplier";
        private const string PermitKey = "free_start_vendor_permit_fee";
        private const string AppraisalKey = "free_start_appraisal_join_multiplier";
        private const string RunnerKey = "free_start_runner_markup";
        private const string HypertechKey = "free_start_hypertech_markup";
        private const string DoctorKey = "free_start_doctor_markup";
        private const string RetiredKey = "free_start_retired_markup";
        private const string PropertyPriceKey = "free_start_property_price";
        private const string PropertyGrowthKey = "free_start_property_growth";
        private const string PropertyCapKey = "free_start_property_cap";
        private static int cachedPropertyPrice, cachedPropertyGrowth, cachedPropertyCap;
        private static DifficultyRules cachedSource, cachedRules;
        private static int cachedRent, cachedMarkup, cachedPower, cachedInsurance;
        private static int cachedPermit, cachedAppraisal, cachedRunner, cachedHypertech, cachedDoctor, cachedRetired;
        private static bool cachedGrace;

        private static DifficultyRules Configure(DifficultyRules source, int rent, int markup, bool grace, int power, int insurance, int permit, int appraisal, int runner, int hypertech, int doctor, int retired, int propertyPrice, int propertyGrowth, int propertyCap, int gain, int loss, int lou)
        {
            lou = Math.Max(0, Math.Min(10000, lou));
            gain = Math.Max(0, Math.Min(1000, gain));
            loss = Math.Max(0, Math.Min(1000, loss));
            propertyPrice = Math.Max(5000, Math.Min(100000, propertyPrice));
            propertyGrowth = Math.Max(0, Math.Min(1000, propertyGrowth));
            propertyCap = Math.Max(0, Math.Min(900000, propertyCap));
            rent = Math.Max(0, Math.Min(100, rent));
            markup = Math.Max(0, Math.Min(100, markup));
            power = Math.Max(0, Math.Min(10, power));
            insurance = Math.Max(0, Math.Min(10, insurance));
            permit = Math.Max(0, Math.Min(10000, permit));
            appraisal = Math.Max(0, Math.Min(10, appraisal));
            runner = Math.Max(0, Math.Min(1000, runner));
            hypertech = Math.Max(0, Math.Min(1000, hypertech));
            doctor = Math.Max(0, Math.Min(1000, doctor));
            retired = Math.Max(0, Math.Min(1000, retired));
            if (cachedLou != lou || cachedGain != gain || cachedLoss != loss || cachedSource != source || cachedRules == null || cachedRent != rent ||
                cachedMarkup != markup || cachedGrace != grace || cachedPower != power || cachedInsurance != insurance ||
                cachedPermit != permit || cachedAppraisal != appraisal || cachedRunner != runner || cachedHypertech != hypertech || cachedDoctor != doctor || cachedRetired != retired ||
                cachedPropertyPrice != propertyPrice || cachedPropertyGrowth != propertyGrowth || cachedPropertyCap != propertyCap)
            {
                cachedSource = source; cachedRent = rent; cachedMarkup = markup; cachedGrace = grace;
                cachedPower = power; cachedInsurance = insurance;
                cachedPermit = permit; cachedAppraisal = appraisal;
                cachedRunner = runner; cachedHypertech = hypertech;
                cachedDoctor = doctor; cachedRetired = retired;
                cachedPropertyPrice = propertyPrice; cachedPropertyGrowth = propertyGrowth; cachedPropertyCap = propertyCap;
                cachedGain = gain; cachedLoss = loss;
                cachedLou = lou;
                cachedRules = source.Clone();
                cachedRules.louDemandValue = lou;
                cachedRules.reputationGainMultiplier = gain / 100f;
                cachedRules.reputationLossMultiplier = loss / 100f;
                cachedRules.propertyBasePrice = propertyPrice;
                cachedRules.propertyPriceIncreasePerDay = propertyGrowth;
                cachedRules.propertyPriceIncreaseCap = propertyCap;
                cachedRules.rentIncreaseMultiplier = 1f + rent / 100f;
                cachedRules.retailMarkup = markup;
                cachedRules.landlordGraceOwnedAtStart = grace;
                cachedRules.powerCostMultiplier = power;
                cachedRules.insuranceCostMultiplier = insurance;
                cachedRules.vendorPermitFee = permit;
                cachedRules.appraisalJoinCostMultiplier = appraisal;
                cachedRules.inventorMarkup = doctor;
                cachedRules.retiredSupplierMarkup = retired;
            }
            return cachedRules;
        }

        internal static DifficultyRules Preview(DifficultyRules source)
        {
            return Configure(source, ExperienceSettings.FreeStartRentGrowth,
                ExperienceSettings.FreeStartRetailMarkup, ExperienceSettings.FreeStartLandlordGrace,
                ExperienceSettings.FreeStartPowerMultiplier, ExperienceSettings.FreeStartInsuranceMultiplier,
                ExperienceSettings.FreeStartVendorPermitFee, ExperienceSettings.FreeStartAppraisalJoinMultiplier,
                ExperienceSettings.FreeStartRunnerMarkup, ExperienceSettings.FreeStartHypertechMarkup,
                ExperienceSettings.FreeStartDoctorMarkup, ExperienceSettings.FreeStartRetiredMarkup,
                ExperienceSettings.FreeStartPropertyPrice, ExperienceSettings.FreeStartPropertyGrowth, ExperienceSettings.FreeStartPropertyCap,
                ExperienceSettings.FreeStartReputationGain, ExperienceSettings.FreeStartReputationLoss, ExperienceSettings.FreeStartLouDemand);
        }

        [HarmonyPatch(typeof(StoreService), "ApplyDifficultyStart")]
        internal static class SaveStartingRules
        {
            static void Prefix()
            {
                if (!ModEntry.IsEnabled || !PlayerStore.IsInstanceExist() ||
                    PlayerStore.Instance.difficultyId != FreeStartDifficulty.Id || !FreeStartEvents.IsCustom()) return;
                ModHelper.SetModDataInt(ModEntry.ModId, LouKey, ExperienceSettings.FreeStartLouDemand);
                ModHelper.SetModDataInt(ModEntry.ModId, GainKey, ExperienceSettings.FreeStartReputationGain);
                ModHelper.SetModDataInt(ModEntry.ModId, LossKey, ExperienceSettings.FreeStartReputationLoss);
                ModHelper.SetModDataInt(ModEntry.ModId, PropertyPriceKey, ExperienceSettings.FreeStartPropertyPrice);
                ModHelper.SetModDataInt(ModEntry.ModId, PropertyGrowthKey, ExperienceSettings.FreeStartPropertyGrowth);
                ModHelper.SetModDataInt(ModEntry.ModId, PropertyCapKey, ExperienceSettings.FreeStartPropertyCap);
                ModHelper.SetModDataInt(ModEntry.ModId, RentKey, ExperienceSettings.FreeStartRentGrowth);
                ModHelper.SetModDataInt(ModEntry.ModId, MarkupKey, ExperienceSettings.FreeStartRetailMarkup);
                ModHelper.SetModDataBool(ModEntry.ModId, GraceKey, ExperienceSettings.FreeStartLandlordGrace);
                ModHelper.SetModDataInt(ModEntry.ModId, PowerKey, ExperienceSettings.FreeStartPowerMultiplier);
                ModHelper.SetModDataInt(ModEntry.ModId, InsuranceKey, ExperienceSettings.FreeStartInsuranceMultiplier);
                ModHelper.SetModDataInt(ModEntry.ModId, PermitKey, ExperienceSettings.FreeStartVendorPermitFee);
                ModHelper.SetModDataInt(ModEntry.ModId, AppraisalKey, ExperienceSettings.FreeStartAppraisalJoinMultiplier);
                ModHelper.SetModDataInt(ModEntry.ModId, RunnerKey, ExperienceSettings.FreeStartRunnerMarkup);
                ModHelper.SetModDataInt(ModEntry.ModId, HypertechKey, ExperienceSettings.FreeStartHypertechMarkup);
                ModHelper.SetModDataInt(ModEntry.ModId, DoctorKey, ExperienceSettings.FreeStartDoctorMarkup);
                ModHelper.SetModDataInt(ModEntry.ModId, RetiredKey, ExperienceSettings.FreeStartRetiredMarkup);
            }
        }

        [HarmonyPatch(typeof(Difficulty), "get_Rules")]
        internal static class ActiveRules
        {
            static void Postfix(ref DifficultyRules __result)
            {
                if (!ModEntry.IsEnabled || __result == null) return;
                if (PlayerStore.IsInstanceExist())
                {
                    if (PlayerStore.Instance.difficultyId != FreeStartDifficulty.Id || !FreeStartEvents.IsCustom()) return;
                    // Defaults preserve the original normal rules for saves created
                    // before these options existed. Never read menu choices on load.
                    __result = Configure(__result,
                        ModHelper.GetModDataInt(ModEntry.ModId, RentKey, 5),
                        ModHelper.GetModDataInt(ModEntry.ModId, MarkupKey, 15),
                        ModHelper.GetModDataBool(ModEntry.ModId, GraceKey, true),
                        ModHelper.GetModDataInt(ModEntry.ModId, PowerKey, 1),
                        ModHelper.GetModDataInt(ModEntry.ModId, InsuranceKey, 1),
                        ModHelper.GetModDataInt(ModEntry.ModId, PermitKey, 1500),
                        ModHelper.GetModDataInt(ModEntry.ModId, AppraisalKey, 1),
                        ModHelper.GetModDataInt(ModEntry.ModId, RunnerKey, 50),
                        ModHelper.GetModDataInt(ModEntry.ModId, HypertechKey, 50),
                        ModHelper.GetModDataInt(ModEntry.ModId, DoctorKey, 15),
                        ModHelper.GetModDataInt(ModEntry.ModId, RetiredKey, 15),
                        ModHelper.GetModDataInt(ModEntry.ModId, PropertyPriceKey, 12000),
                        ModHelper.GetModDataInt(ModEntry.ModId, PropertyGrowthKey, 50),
                        ModHelper.GetModDataInt(ModEntry.ModId, PropertyCapKey, 5000),
                        ModHelper.GetModDataInt(ModEntry.ModId, GainKey, 100),
                        ModHelper.GetModDataInt(ModEntry.ModId, LossKey, 100),
                        ModHelper.GetModDataInt(ModEntry.ModId, LouKey, 750));
                }
                else if (NewGameData.Instance != null && NewGameData.Instance.difficultyId == FreeStartDifficulty.Id &&
                    FreeStartDifficulty.IsFreeStart()) __result = Preview(__result);
            }
        }

        // The native supplier multiplier also affects the hypertech smuggler.
        // Override only the supply runner's feature so that unrelated seller is unchanged.
        [HarmonyPatch(typeof(ItemFeatureList), "PremiumBuy")]
        internal static class RunnerPrice
        {
            static void Postfix(ItemFeature __result)
            {
                if (!ModEntry.IsEnabled || __result == null || !PlayerStore.IsInstanceExist() ||
                    PlayerStore.Instance.difficultyId != FreeStartDifficulty.Id || !FreeStartEvents.IsCustom()) return;
                int percent = ModHelper.GetModDataInt(ModEntry.ModId, RunnerKey, 50);
                __result.SetPreExposedValueMod(Math.Max(0, Math.Min(1000, percent)));
            }
        }

        [HarmonyPatch(typeof(ItemFeatureList), "PremiumBuyHypertech")]
        internal static class HypertechPrice
        {
            static void Postfix(ItemFeature __result)
            {
                if (!ModEntry.IsEnabled || __result == null || !PlayerStore.IsInstanceExist() ||
                    PlayerStore.Instance.difficultyId != FreeStartDifficulty.Id || !FreeStartEvents.IsCustom()) return;
                int percent = ModHelper.GetModDataInt(ModEntry.ModId, HypertechKey, 50);
                __result.SetPreExposedValueMod(Math.Max(0, Math.Min(1000, percent)));
            }
        }
    }
}

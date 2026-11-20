namespace Kriegsspiel.Core.Content;

public sealed class FrictionTable
{
    public HqParameters Hq { get; init; } = new();
    public OrderParameters Orders { get; init; } = new();
    public MovementParameters Movement { get; init; } = new();
    public CombatParameters Combat { get; init; } = new();
    public ReconParameters Recon { get; init; } = new();
    public ReportParameters Reports { get; init; } = new();
    public SupplyParameters Supply { get; init; } = new();
    public CaptureParameters Capture { get; init; } = new();
    public CivilianParameters Civilians { get; init; } = new();
    public LlmParameters Llm { get; init; } = new();
    public List<DifficultyProfile> DifficultyProfiles { get; init; } = new();

    public sealed class HqParameters
    {
        public int Couriers { get; init; } = 2;
        public int CourierReplacementMinutes { get; init; } = 60;
        public int DefaultDeliveryTimeMinutes { get; init; } = 25;
        public int DefaultReturnTimeMinutes { get; init; } = 25;
        public double CourierLossBase { get; init; } = 0.05;
        public double CourierLossPerKm { get; init; } = 0.01;
        public double CourierLossHostileBonus { get; init; } = 0.10;
        public double ReportLossBase { get; init; } = 0.03;
    }

    public sealed class OrderParameters
    {
        public double P0 { get; init; } = 0.03;
        public double PMin { get; init; } = 0.03;
        public double PMax { get; init; } = 0.90;
        public string PMinModifierType { get; init; } = "Additive";
        public AmbiguityWeights Ambiguity { get; init; } = new();
        public TraitCoefficients Traits { get; init; } = new();
        public int MaxBranches { get; init; } = 3;
    }

    public sealed class AmbiguityWeights
    {
        public double MissingTarget { get; init; } = 0.7;
        public double VagueFormulation { get; init; } = 0.25;
        public double MissingDeadline { get; init; } = 0.1;
        public double MissingReportTime { get; init; } = 0.1;
        public double PerBranch { get; init; } = 0.05;
    }

    public sealed class TraitCoefficients
    {
        public double Literalism { get; init; } = 0.15;
        public double Caution { get; init; } = 0.10;
        public double Initiative { get; init; } = 0.10;
    }

    public sealed class MovementParameters
    {
        public MovementMode March { get; init; } = new() { TimeMultiplier = 1.0, Detectability = 0.2, ReadinessDelta = -5, FatigueDelta = 5, Position = "March" };
        public MovementMode Stealth { get; init; } = new() { TimeMultiplier = 1.4, Detectability = -0.1, ReadinessDelta = -3, FatigueDelta = 8, Position = "March" };
        public MovementMode ContactReady { get; init; } = new() { TimeMultiplier = 1.2, Detectability = 0.1, ReadinessDelta = 0, FatigueDelta = 6, Position = "AttackDefense" };
    }

    public sealed class MovementMode
    {
        public double TimeMultiplier { get; init; }
        public double Detectability { get; init; }
        public int ReadinessDelta { get; init; }
        public int FatigueDelta { get; init; }
        public string Position { get; init; } = string.Empty;
    }

    public sealed class CombatParameters
    {
        public double LethalitySelf { get; init; } = 0.35;
        public double LethalityEnemy { get; init; } = 0.35;
        public PositionMultipliers Position { get; init; } = new();
        public CollisionMultipliers Collision { get; init; } = new();
        public double TerrainDefenseFactor { get; init; } = 0.1;
        public double MoraleLossPerTenPercentStrength { get; init; } = 5.0;
        public CombatEffectiveThresholds CombatEffective { get; init; } = new();
        public double CaptureBaseChance { get; init; } = 0.2;
        public double CaptureEncircledBonus { get; init; } = 0.2;
        public double CaptureInitiativePenalty { get; init; } = 0.05;
        public double EntrenchmentRatePerStep { get; init; } = 0.1;
        public double EntrenchmentCap { get; init; } = 1.0;
        public PoorControlModifiers PoorControl { get; init; } = new();
        public double AmmoConsumptionPerTick { get; init; } = 5.0;
        public int FlankMoralePenalty { get; init; } = 10;
        public int RearMoralePenalty { get; init; } = 15;
    }

    public sealed class PositionMultipliers
    {
        public double March { get; init; } = 0.5;
        public double Attack { get; init; } = 1.0;
        public double Defense { get; init; } = 1.3;
    }

    public sealed class CollisionMultipliers
    {
        public double Front { get; init; } = 1.0;
        public double Flank { get; init; } = 1.4;
        public double Rear { get; init; } = 1.8;
    }

    public sealed class CombatEffectiveThresholds
    {
        public double StrengthRatio { get; init; } = 0.4;
        public int Morale { get; init; } = 30;
        public int Readiness { get; init; } = 20;
        public int Ammo { get; init; } = 1;
        public int PanicMorale { get; init; } = 30;
    }

    public sealed class PoorControlModifiers
    {
        public double PositionMultiplier { get; init; } = 0.85;
        public double CollisionMultiplier { get; init; } = 0.85;
        public double MoraleStabilityMultiplier { get; init; } = 0.9;
        public double LossMultiplier { get; init; } = 1.1;
    }

    public sealed class ReconParameters
    {
        public double DetectionBase { get; init; } = 0.2;
        public double DetectionObservabilityWeight { get; init; } = 0.15;
        public double OpDetectionBonus { get; init; } = 0.0;
        public double CamouflageStealthBonus { get; init; } = 0.15;
        public double DetectionMin { get; init; } = 0.05;
        public double DetectionMax { get; init; } = 0.95;
        public double SuccessBase { get; init; } = 0.5;
        public double UnconsciousFailBase { get; init; } = 0.3;
        public double UnconsciousFailCap { get; init; } = 0.8;
        public int FactKeyWindowMinutes { get; init; } = 30;
    }

    public sealed class ReportParameters
    {
        public int BaseDelayMinutes { get; init; } = 20;
        public int JitterMinutes { get; init; } = 20;
        public double StrengthNoise { get; init; } = 0.25;
        public double HonestyDistortionPerLevel { get; init; } = 0.15;
        public double ObserverError { get; init; } = 0.1;
        public int QuerySilenceMinutes { get; init; } = 60;
        public int ImpossibleReactionPenaltyMinutes { get; init; } = 30;
    }

    public sealed class SupplyParameters
    {
        public double CargoToAmmoCoefficient { get; init; } = 1.0;
        public int ConsumersPerResupply { get; init; } = 1;
        public int AmmoCeiling { get; init; } = 100;
    }

    public sealed class CaptureParameters
    {
        public double ConvoySpeedMultiplier { get; init; } = 0.7;
        public double ConvoyOffroadMultiplier { get; init; } = 0.5;
        public double EscapeChanceBase { get; init; } = 0.05;
        public double EscapeChancePerGuardDeficit { get; init; } = 0.05;
        public double NoPrisonersCaptureReduction { get; init; } = 1.0;
        public double NoPrisonersLoyaltyPenalty { get; init; } = 0.2;
        public int InterrogationMinMinutes { get; init; } = 30;
        public int InterrogationMaxMinutes { get; init; } = 60;
    }

    public sealed class CivilianParameters
    {
        public double RumorCredibilityMax { get; init; } = 1.0;
        public double FalseReportBase { get; init; } = 0.2;
        public double PanicRoadMultiplierBase { get; init; } = 1.0;
        public double PanicRoadMultiplierPerLevel { get; init; } = 0.1;
        public double CoercionLoyaltyPenalty { get; init; } = 0.1;
        public double ProtectionLoyaltyBonus { get; init; } = 0.1;
        public double LootingLoyaltyPenalty { get; init; } = 0.2;
        public double PanicIncreaseOnCombat { get; init; } = 0.1;
        public List<double> PanicThresholds { get; init; } = new() { 0.3, 0.6, 0.9 };
        public List<double> LoyaltyThresholds { get; init; } = new() { 0.33, 0.66 };
    }

    public sealed class LlmParameters
    {
        public bool Enabled { get; init; }
        public string SchemaVersion { get; init; } = "1.0";
        public string FallbackMode { get; init; } = "DoctrineTemplate";
        public bool LogPrompts { get; init; } = true;
    }

    public sealed class DifficultyProfile
    {
        public required string Id { get; init; }
        public string? Name { get; init; }
        public Dictionary<string, double> Modifiers { get; init; } = new();
    }
}
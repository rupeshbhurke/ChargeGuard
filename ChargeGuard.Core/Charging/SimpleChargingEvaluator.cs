using ChargeGuard.Core.Battery;
using ChargeGuard.Core.Logging;
using ChargeGuard.Core.Settings;

namespace ChargeGuard.Core.Charging;

/// <summary>
/// Simple charging alert evaluator that checks if battery is at or above target while charging.
/// </summary>
public class SimpleChargingEvaluator
{
    private readonly ChargeGuardSettings _settings;
    private readonly IAppLogger _logger;

    public SimpleChargingEvaluator(ChargeGuardSettings settings, IAppLogger logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Evaluates the battery state and returns an alert decision if needed.
    /// </summary>
    public ChargingAlertDecision? EvaluateState(BatterySnapshot snapshot)
    {
        if (snapshot == null)
            return null;

        // Simple check: if battery is at or above target and charging, show alert
        if (snapshot.IsAcPowerConnected && 
            snapshot.IsCharging && 
            snapshot.BatteryPercentage.HasValue &&
            snapshot.BatteryPercentage.Value >= _settings.NormalTargetPercentage)
        {
            _logger.LogInfo($"Battery at {snapshot.BatteryPercentage}% (target: {_settings.NormalTargetPercentage}%), alert triggered");
            
            return new ChargingAlertDecision(
                ChargingAlertType.Target,
                $"Charging target reached\nBattery is at {snapshot.BatteryPercentage}%. You can disconnect the charger.",
                snapshot.BatteryPercentage.Value,
                _settings.NormalTargetPercentage,
                playSound: _settings.SoundEnabled);
        }

        return null;
    }
}
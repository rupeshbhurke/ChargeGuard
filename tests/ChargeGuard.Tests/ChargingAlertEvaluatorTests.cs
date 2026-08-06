using ChargeGuard.Battery;
using ChargeGuard.Charging;
using ChargeGuard.Settings;
using Xunit;

namespace ChargeGuard.Tests;

public class ChargingAlertEvaluatorTests
{
    private readonly ChargeGuardSettings _defaultSettings;
    private readonly TestClock _testClock;

    public ChargingAlertEvaluatorTests()
    {
        _defaultSettings = new ChargeGuardSettings();
        _testClock = new TestClock();
    }

    [Fact]
    public void ChargingFrom70To75SendsOneAdvanceWarning()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            AdvanceWarningEnabled = true,
            AdvanceWarningPercentage = 75
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Act
        var snapshot75 = CreateSnapshot(75, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot75);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal(ChargingAlertType.AdvanceWarning, decision.AlertType);
        Assert.Contains("75%", decision.Message);
        Assert.Contains("80%", decision.Message);
    }

    [Fact]
    public void ChargingFrom79To80SendsOneTargetAlert()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            SoundEnabled = false
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Act - advance to 79%
        var snapshot79 = CreateSnapshot(79, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot79);

        // Then to 80%
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot80);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal(ChargingAlertType.Target, decision.AlertType);
        Assert.Contains("80%", decision.Message);
    }

    [Fact]
    public void Repeated80EventsDoNotDuplicateTargetAlert()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            AdvanceWarningEnabled = false // Disable to avoid advance warning interfering
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Reach 80%
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        var firstDecision = evaluator.EvaluateState(snapshot80);
        Assert.NotNull(firstDecision);
        Assert.Equal(ChargingAlertType.Target, firstDecision.AlertType);

        // Act - evaluate again at 80%
        var decision = evaluator.EvaluateState(snapshot80);

        // Assert
        Assert.Null(decision); // No duplicate alert
    }

    [Fact]
    public void FluctuationBetween79And80DoesNotCreateAlertSpam()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            AdvanceWarningEnabled = false // Disable to avoid advance warning interfering
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Reach 80%
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        var firstDecision = evaluator.EvaluateState(snapshot80);
        Assert.NotNull(firstDecision);

        // Act - fluctuate between 79 and 80
        var snapshot79 = CreateSnapshot(79, isAcConnected: true, isCharging: true);
        var decision1 = evaluator.EvaluateState(snapshot79);
        var decision2 = evaluator.EvaluateState(snapshot80);
        var decision3 = evaluator.EvaluateState(snapshot79);
        var decision4 = evaluator.EvaluateState(snapshot80);

        // Assert
        Assert.Null(decision1);
        Assert.Null(decision2);
        Assert.Null(decision3);
        Assert.Null(decision4);
    }

    [Fact]
    public void RemainingConnectedSchedulesFirstReminder()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            RepeatedRemindersEnabled = true,
            FirstReminderDelay = TimeSpan.FromMinutes(5),
            AdvanceWarningEnabled = false // Disable to avoid advance warning interfering
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Reach target
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot80);

        // Act
        var session = evaluator.CurrentSession;
        Assert.NotNull(session);
        Assert.True(session.TargetAlertSent);
        Assert.NotNull(session.NextReminderDue);
    }

    [Fact]
    public void ReminderIntervalsRepeatCorrectly()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            RepeatedRemindersEnabled = true,
            FirstReminderDelay = TimeSpan.FromMinutes(5),
            RepeatedReminderInterval = TimeSpan.FromMinutes(10),
            AdvanceWarningEnabled = false // Disable to avoid advance warning interfering
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Reach target
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot80);

        var session = evaluator.CurrentSession;
        Assert.NotNull(session);
        var firstReminderTime = session.NextReminderDue;

        // Act - advance time past first reminder
        _testClock.AdvanceTime(TimeSpan.FromMinutes(6));
        var snapshot81 = CreateSnapshot(81, isAcConnected: true, isCharging: true);
        var firstReminder = evaluator.EvaluateState(snapshot81);
        Assert.NotNull(firstReminder);
        Assert.Equal(ChargingAlertType.Reminder, firstReminder.AlertType);

        // Check next reminder is scheduled
        var secondReminderTime = session.NextReminderDue;
        Assert.NotNull(secondReminderTime);
        var expectedSecondTime = _testClock.UtcNow + settings.RepeatedReminderInterval;
        Assert.Equal(expectedSecondTime, secondReminderTime.Value);
    }

    [Fact]
    public void DisconnectingChargerStopsReminders()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            RepeatedRemindersEnabled = true
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Reach target
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot80);

        // Act - disconnect charger
        var snapshotDisconnected = CreateSnapshot(80, isAcConnected: false, isCharging: false);
        evaluator.EvaluateState(snapshotDisconnected);

        // Assert
        Assert.Null(evaluator.CurrentSession);
    }

    [Fact]
    public void ReconnectingStartsNewChargingSession()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Disconnect
        var snapshotDisconnected = CreateSnapshot(70, isAcConnected: false, isCharging: false);
        evaluator.EvaluateState(snapshotDisconnected);

        // Act - reconnect
        var snapshotConnected = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshotConnected);

        // Assert
        Assert.NotNull(evaluator.CurrentSession);
        Assert.False(evaluator.CurrentSession.TargetAlertSent);
    }

    [Fact]
    public void EscalationOccursOnceAt90()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            EscalationPercentage = 90,
            AdvanceWarningEnabled = false // Disable to avoid advance warning interfering
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Reach target
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot80);

        // Act - reach escalation
        var snapshot90 = CreateSnapshot(90, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot90);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal(ChargingAlertType.Escalation, decision.AlertType);
        Assert.Contains("90%", decision.Message);
    }

    [Fact]
    public void Temporary100ModeSuppresses80Alert()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            AdvanceWarningEnabled = false // Disable advance warning for this test
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Enable temporary 100% mode
        evaluator.EnableTemporaryFullChargeMode();

        // Act - reach 80%
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot80);

        // Assert
        Assert.Null(decision); // No 80% alert
        Assert.Equal(100, evaluator.CurrentSession?.ActiveTargetPercentage);
    }

    [Fact]
    public void Temporary100ModeAlertsAt100()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            AdvanceWarningEnabled = false // Disable for this test
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging and enable 100% mode
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);
        evaluator.EnableTemporaryFullChargeMode();

        // Act - reach 100%
        var snapshot100 = CreateSnapshot(100, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot100);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal(ChargingAlertType.Target, decision.AlertType);
    }

    [Fact]
    public void Temporary100ModeResetsAfterDisconnection()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging and enable 100% mode
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);
        evaluator.EnableTemporaryFullChargeMode();

        // Act - disconnect
        var snapshotDisconnected = CreateSnapshot(70, isAcConnected: false, isCharging: false);
        evaluator.EvaluateState(snapshotDisconnected);

        // Reconnect
        var snapshotReconnected = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshotReconnected);

        // Assert
        Assert.NotNull(evaluator.CurrentSession);
        Assert.Equal(80, evaluator.CurrentSession.ActiveTargetPercentage);
        Assert.False(evaluator.CurrentSession.IsTemporaryFullChargeMode);
    }

    [Fact]
    public void SnoozeSuppressesRemindersUntilExpiration()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            RepeatedRemindersEnabled = true,
            FirstReminderDelay = TimeSpan.FromMinutes(1)
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Reach target
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot80);

        // Snooze for 10 minutes
        evaluator.Snooze(TimeSpan.FromMinutes(10));

        // Act - advance time by 5 minutes
        _testClock.AdvanceTime(TimeSpan.FromMinutes(5));
        var snapshot81 = CreateSnapshot(81, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot81);

        // Assert
        Assert.Null(decision); // Snoozed, no reminder
    }

    [Fact]
    public void PausingAlertsSuppressesAllAlerts()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            AdvanceWarningEnabled = true,
            AdvanceWarningPercentage = 75
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Pause alerts
        evaluator.PauseAlerts();

        // Act - reach advance warning threshold
        var snapshot75 = CreateSnapshot(75, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot75);

        // Assert
        Assert.Null(decision); // Paused, no alert
    }

    [Fact]
    public void ResumingAlertsEvaluatesCurrentStateSafely()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            AdvanceWarningEnabled = false // Disable for this test
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging and pause
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);
        evaluator.PauseAlerts();

        // Advance to 79% while paused (before target)
        var snapshot79 = CreateSnapshot(79, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot79);

        // Act - resume alerts and cross target
        evaluator.ResumeAlerts();
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot80);

        // Assert
        // Should alert when crossing target after resume
        Assert.NotNull(decision);
        Assert.Equal(ChargingAlertType.Target, decision.AlertType);
    }

    [Fact]
    public void StartingAt85WhileConnectedProducesAtMostOneAlert()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Act - start at 85% while connected
        var snapshot85 = CreateSnapshot(85, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot85);

        // Assert
        // Should not alert since already above target at startup
        Assert.Null(decision);
        Assert.True(evaluator.CurrentSession?.TargetAlertSent);
    }

    [Fact]
    public void SleepAndResumeReconciliationDoesNotDuplicateAlerts()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Start charging at 70%
        var snapshot70 = CreateSnapshot(70, isAcConnected: true, isCharging: true);
        evaluator.EvaluateState(snapshot70);

        // Reach target
        var snapshot80 = CreateSnapshot(80, isAcConnected: true, isCharging: true);
        var firstDecision = evaluator.EvaluateState(snapshot80);
        Assert.NotNull(firstDecision);

        // Act - simulate resume by re-evaluating same state
        var decision = evaluator.EvaluateState(snapshot80);

        // Assert
        Assert.Null(decision); // No duplicate alert
    }

    [Fact]
    public void UnknownBatteryPercentageProducesNoAlert()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Act - evaluate with unknown percentage
        var snapshot = CreateSnapshot(null, isAcConnected: true, isCharging: true);
        var decision = evaluator.EvaluateState(snapshot);

        // Assert
        Assert.Null(decision);
    }

    [Fact]
    public void SystemWithNoBatteryDoesNotCrash()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80
        };
        var evaluator = new ChargingAlertEvaluator(settings, _testClock);

        // Act - evaluate with no battery
        var snapshot = new BatterySnapshot
        {
            IsAcPowerConnected = true,
            BatteryPercentage = null,
            IsCharging = false,
            IsBatteryAvailable = false
        };
        var decision = evaluator.EvaluateState(snapshot);

        // Assert
        Assert.Null(decision);
        Assert.Null(evaluator.CurrentSession); // Session should end
    }

    [Fact]
    public void InvalidSettingsAreRejectedOrNormalizedSafely()
    {
        // Arrange
        var settings = new ChargeGuardSettings
        {
            NormalTargetPercentage = 80,
            AdvanceWarningPercentage = 85, // Invalid: higher than target
            EscalationPercentage = 70     // Invalid: lower than target
        };

        // Act
        settings.ValidateAndNormalize();

        // Assert
        Assert.True(settings.AdvanceWarningPercentage < settings.NormalTargetPercentage);
        Assert.True(settings.EscalationPercentage >= settings.NormalTargetPercentage);
    }

    private static BatterySnapshot CreateSnapshot(int? percentage, bool isAcConnected, bool isCharging)
    {
        return new BatterySnapshot
        {
            BatteryPercentage = percentage,
            IsAcPowerConnected = isAcConnected,
            IsCharging = isCharging,
            IsBatteryAvailable = true
        };
    }

    private class TestClock : IClock
    {
        private DateTime _currentTime = DateTime.UtcNow;

        public DateTime UtcNow => _currentTime;

        public void AdvanceTime(TimeSpan duration)
        {
            _currentTime = _currentTime.Add(duration);
        }
    }
}

namespace ChargeGuard.Notifications;

/// <summary>
/// Interface for sound playback implementations.
/// </summary>
public interface ISoundPlayer
{
    /// <summary>
    /// Plays a notification sound.
    /// </summary>
    void PlayNotificationSound();
}

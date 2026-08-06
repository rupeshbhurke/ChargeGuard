using System.Media;

namespace ChargeGuard.Notifications;

/// <summary>
/// Plays system sounds for notifications.
/// </summary>
public class SystemSoundPlayer : ISoundPlayer
{
    public void PlayNotificationSound()
    {
        try
        {
            // Use the system default notification sound
            SystemSounds.Exclamation.Play();
        }
        catch
        {
            // Silently ignore sound playback failures
        }
    }
}

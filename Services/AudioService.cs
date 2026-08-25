using System.IO;
using System.Media;
using System.Windows.Media;

namespace Prayer.Services;

public class AudioService : IAudioService
{
    private MediaPlayer? _mediaPlayer;

    public void PlayAzaan(string? customFilePath, bool playDefaultChimeFallback)
    {
        Stop();

        try
        {
            if (!string.IsNullOrWhiteSpace(customFilePath) && File.Exists(customFilePath))
            {
                PlayMediaFile(customFilePath);
                return;
            }

            if (playDefaultChimeFallback)
            {
                PlayBuiltinChime();
            }
        }
        catch
        {
            // Fallback to system sound if any playback issue occurs
            SystemSounds.Asterisk.Play();
        }
    }

    public void PlayTestSound(string? customFilePath)
    {
        Stop();

        if (!string.IsNullOrWhiteSpace(customFilePath) && File.Exists(customFilePath))
        {
            PlayMediaFile(customFilePath);
        }
        else
        {
            PlayBuiltinChime();
        }
    }

    private void PlayMediaFile(string filePath)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.Open(new Uri(filePath, UriKind.Absolute));
            _mediaPlayer.Volume = 0.85;
            _mediaPlayer.Play();
        });
    }

    private void PlayBuiltinChime()
    {
        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var chimePath = Path.Combine(appDir, "Resources", "Audio", "default_chime.wav");

            if (File.Exists(chimePath))
            {
                using var player = new SoundPlayer(chimePath);
                player.Play();
            }
            else
            {
                // Harmonious synthesized beep sequence (E5 -> G#5 -> B5 -> E6)
                Task.Run(() =>
                {
                    try
                    {
                        Console.Beep(659, 350); // E5
                        Thread.Sleep(100);
                        Console.Beep(830, 350); // G#5
                        Thread.Sleep(100);
                        Console.Beep(987, 450); // B5
                        Thread.Sleep(100);
                        Console.Beep(1318, 700); // E6
                    }
                    catch
                    {
                        SystemSounds.Beep.Play();
                    }
                });
            }
        }
        catch
        {
            SystemSounds.Beep.Play();
        }
    }

    public void Stop()
    {
        try
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Close();
                    _mediaPlayer = null;
                }
            });
        }
        catch { }
    }
}

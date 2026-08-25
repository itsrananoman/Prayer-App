namespace Prayer.Services;

public interface IAudioService
{
    void PlayAzaan(string? customFilePath, bool playDefaultChimeFallback);
    void PlayTestSound(string? customFilePath);
    void Stop();
}

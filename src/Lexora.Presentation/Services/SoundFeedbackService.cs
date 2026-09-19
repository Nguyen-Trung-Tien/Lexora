using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Lexora.Presentation.Services;

public interface ISoundFeedbackService
{
    void PlayCardFlipSound();
    void PlaySoftTapSound();
    void PlayReviewCompleteSound();
    void PlayStreakSound();
}

/// <summary>
/// High-performance, offline sound feedback service for Fluent UI desktop interactions.
/// Synthesizes pleasant harmonic sine chimes in memory and plays via native WinRT MediaPlayer.
/// </summary>
public sealed class SoundFeedbackService : ISoundFeedbackService, IDisposable
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly byte[] _streakSoundBytes;
    private readonly byte[] _reviewCompleteSoundBytes;
    private readonly byte[] _cardFlipSoundBytes;
    private readonly byte[] _softTapSoundBytes;

    public SoundFeedbackService()
    {
        _mediaPlayer = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.SoundEffects
        };

        _streakSoundBytes = GenerateChimeWav(44100,
        [
            (659.25, 0.10, 0.28),  // E5
            (830.61, 0.10, 0.30),  // G#5
            (1046.50, 0.12, 0.32), // C6
            (1318.51, 0.25, 0.35)  // E6
        ]);

        _reviewCompleteSoundBytes = GenerateChimeWav(44100,
        [
            (523.25, 0.12, 0.26),  // C5
            (659.25, 0.12, 0.28),  // E5
            (783.99, 0.12, 0.30),  // G5
            (1046.50, 0.30, 0.32)  // C6
        ]);

        _cardFlipSoundBytes = GenerateChimeWav(44100,
        [
            (587.33, 0.04, 0.14),  // D5
            (880.00, 0.06, 0.16)   // A5
        ]);

        _softTapSoundBytes = GenerateChimeWav(44100,
        [
            (783.99, 0.03, 0.15)   // G5
        ]);
    }

    public void PlayStreakSound() => PlayWavBytes(_streakSoundBytes);

    public void PlayReviewCompleteSound() => PlayWavBytes(_reviewCompleteSoundBytes);

    public void PlayCardFlipSound() => PlayWavBytes(_cardFlipSoundBytes);

    public void PlaySoftTapSound() => PlayWavBytes(_softTapSoundBytes);

    private void PlayWavBytes(byte[] wavBytes)
    {
        try
        {
            var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(wavBytes);
                writer.StoreAsync().AsTask().GetAwaiter().GetResult();
            }
            stream.Seek(0);
            _mediaPlayer.Source = MediaSource.CreateFromStream(stream, "audio/wav");
            _mediaPlayer.Play();
        }
        catch
        {
            // Fail silently if audio device is unavailable
        }
    }

    /// <summary>
    /// Generates an in-memory 16-bit PCM RIFF WAV byte array with smooth attack/decay envelope.
    /// </summary>
    private static byte[] GenerateChimeWav(int sampleRate, (double freq, double duration, double volume)[] notes)
    {
        double totalDuration = 0;
        foreach (var note in notes) totalDuration += note.duration;

        int totalSamples = (int)(sampleRate * totalDuration);
        int byteCount = totalSamples * 2; // 16-bit mono = 2 bytes per sample

        using var ms = new MemoryStream(44 + byteCount);
        using var writer = new BinaryWriter(ms);

        // 1. RIFF Header
        writer.Write("RIFF"u8);
        writer.Write(36 + byteCount);
        writer.Write("WAVE"u8);

        // 2. fmt chunk
        writer.Write("fmt "u8);
        writer.Write(16);                 // Subchunk1Size (16 for PCM)
        writer.Write((short)1);            // AudioFormat (1 = PCM)
        writer.Write((short)1);            // NumChannels (1 = Mono)
        writer.Write(sampleRate);          // SampleRate
        writer.Write(sampleRate * 2);      // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
        writer.Write((short)2);            // BlockAlign (NumChannels * BitsPerSample/8)
        writer.Write((short)16);           // BitsPerSample (16-bit)

        // 3. data chunk
        writer.Write("data"u8);
        writer.Write(byteCount);

        // 4. Synthesize notes with soft exponential attack & release envelope
        int samplesWritten = 0;
        foreach (var (freq, duration, volume) in notes)
        {
            int noteSamples = (int)(sampleRate * duration);
            for (int i = 0; i < noteSamples; i++)
            {
                double t = (double)i / sampleRate;
                // Attack (first 4ms) to eliminate audio clipping, exponential decay for gentle bell tone
                double attack = Math.Min(1.0, i / (sampleRate * 0.004));
                double decay = Math.Exp(-3.8 * (t / duration));
                double envelope = attack * decay;

                short sample = (short)(Math.Sin(2.0 * Math.PI * freq * t) * envelope * volume * short.MaxValue);
                writer.Write(sample);
                samplesWritten++;
            }
        }

        // Fill remaining samples if any rounding difference
        while (samplesWritten < totalSamples)
        {
            writer.Write((short)0);
            samplesWritten++;
        }

        return ms.ToArray();
    }

    public void Dispose()
    {
        _mediaPlayer.Dispose();
    }
}

using System.IO.Compression;
using Engine.Asset;
using Engine.Asset.v1;
using Foster.Audio;
using Foster.Framework;

namespace Engine.MiniAudio;

public class Audio
{
    public void Play(Sound sound)
    {
        sound.Play();
    }


    public static void AudioTest()
    {
        AssetBuilder.Pack(Assets.GetContentAssetsPath(),"pack.zip");
        Asset.v1.AssetsV1.LazyInitializeCache("pack.zip");
        if (!AssetsV1.TryOpenCachedEntry("Audio/shortcuts.ogg", out var stream))
            return;
        if (stream == null)
        {
            Log.Info("Couldn't load shortcuts.ogg");
            return;
        }

        byte[] encodeData;
        using (var ms = new MemoryStream())
        {
            stream.CopyTo(ms);
            encodeData = ms.ToArray();
        }
        stream.Dispose();
        Sound sound;
        SoundInstance instance;
        int channels = 0;
        double instanceLength;
        var format = AudioFormat.F32;
        channels = 0; // Determine automatically
        var sampleRate = Foster.Audio.Audio.SampleRate;

        if (Sound.TryDecode(encodeData, ref format, ref channels, ref sampleRate, out var frameCount, out var data))
        {
            sound = new Sound(data!, format, channels, sampleRate, frameCount);
            instance = sound.CreateInstance();
            instance.Protected = true;
            instance.Looping = true;
            instance.Volume = 0.1f;
            //instanceLength = instance.Length.TotalSeconds;
            instance.Play();
        }
        else
        {
            throw new Exception("Couldn't parse encoded data");
        }
    }
}
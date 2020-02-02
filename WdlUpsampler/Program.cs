using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WdlUpsampler {
    class Program {

        static void Main(string[] args) {

            if (args.Length < 2) {
                Console.WriteLine("Usage: wdlresampler <input> <output>");
                using (var enumerator = new MMDeviceEnumerator()) {

                    var inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    Console.WriteLine("Available input devices:");
                    foreach (var device in inputDevices) {
                        Console.WriteLine(device.FriendlyName);
                    }
                    Console.WriteLine("Available output devices:");
                    foreach (var device in outputDevices) {
                        Console.WriteLine(device.FriendlyName);
                    }

                }
                return;
            }

            StartApp(args[0], args[1]);
        }

        static void StartApp(string inputName, string outputName) {
            try {

                MMDevice input = FindDevice(inputName, false),
                    output = FindDevice(outputName, true);

                if (input == null || output == null) {
                    Console.WriteLine("Waiting for devices...");

                    while (input == null || output == null) {
                        Thread.Sleep(500);
                        input = FindDevice(inputName, false);
                        output = FindDevice(outputName, true);
                    }
                }

                var inputFormat = input.AudioClient.MixFormat;
                var outputFormat = output.AudioClient.MixFormat;
                Console.WriteLine("Recording from \"" + input.FriendlyName + "\" (" + FormatToString(inputFormat) +
                    ") to \"" + output.FriendlyName + "\" (" + FormatToString(outputFormat) + "). Press Enter to exit.");

                using (var recorder = new WasapiCapture(input))
                using (var player = new WasapiOut(output, AudioClientShareMode.Shared, true, 200)) {
                    var waveInProvider = new WaveInProvider(recorder);
                    var sampleProvider = ConvertWaveProviderIntoSampleProvider(waveInProvider);
                    var resampler = new InputDrivenWdlResamplingSampleProvider(sampleProvider, outputFormat.SampleRate);
                    var waveProvider = new SampleToWaveProvider(resampler);
                    player.Init(waveInProvider);
                    var exit = false;
                    player.PlaybackStopped += delegate (object sender, StoppedEventArgs e) {
                        if (exit)
                            return;
                        Thread.Sleep(1000);
                        player.Init(waveInProvider);
                        player.Play();
                    };
                    player.Play();
                    recorder.StartRecording();
                    Console.ReadLine();
                    exit = true;

                }

            } catch (Exception) {

                Thread.Sleep(1000);
                StartApp(inputName, outputName);

            }
        }

        static MMDevice FindDevice(string name, bool forPlayback) {
            using (var enumerator = new MMDeviceEnumerator()) {
                MMDeviceCollection devices;
                if (forPlayback)
                    devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                else
                    devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                foreach (var device in devices) {
                    if (device.FriendlyName.Contains(name)) {
                        return device;
                    }
                }
                return null;
            }
        }

        static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider waveProvider) {
            ISampleProvider sampleProvider;
            if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm) {
                // go to float
                if (waveProvider.WaveFormat.BitsPerSample == 8) {
                    sampleProvider = new Pcm8BitToSampleProvider(waveProvider);
                } else if (waveProvider.WaveFormat.BitsPerSample == 16) {
                    sampleProvider = new Pcm16BitToSampleProvider(waveProvider);
                } else if (waveProvider.WaveFormat.BitsPerSample == 24) {
                    sampleProvider = new Pcm24BitToSampleProvider(waveProvider);
                } else if (waveProvider.WaveFormat.BitsPerSample == 32) {
                    sampleProvider = new Pcm32BitToSampleProvider(waveProvider);
                } else {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            } else if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat) {
                if (waveProvider.WaveFormat.BitsPerSample == 64)
                    sampleProvider = new WaveToSampleProvider64(waveProvider);
                else
                    sampleProvider = new WaveToSampleProvider(waveProvider);
            } else {
                throw new ArgumentException("Unsupported source encoding");
            }
            return sampleProvider;
        }

        static String FormatToString(WaveFormat format) {
            return format.SampleRate + "/" + format.BitsPerSample;
        }
    }
}
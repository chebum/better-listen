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
            using (var enumerator = new MMDeviceEnumerator()) {
                
                var inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                MMDevice input = null, output = null;
                if (args.Length > 0) {
                    foreach (var device in inputDevices) {
                        if (device.FriendlyName.Contains(args[0])) {
                            input = device;
                            break;
                        }
                    }
                }
                if (args.Length > 1) {
                    foreach (var device in outputDevices) {
                        if (device.FriendlyName.Contains(args[1])) {
                            output = device;
                            break;
                        }
                    }
                }
                if (input == null || output == null) {
                    Console.WriteLine("Usage: wdlresampler <input> <output>");
                    Console.WriteLine("Available input devices:");
                    foreach (var device in inputDevices) {
                        Console.WriteLine(device.FriendlyName);
                    }
                    Console.WriteLine("Available output devices:");
                    foreach (var device in outputDevices) {
                        Console.WriteLine(device.FriendlyName);
                    }
                    return;
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
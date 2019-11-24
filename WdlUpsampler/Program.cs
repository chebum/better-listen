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
			var enumerator = new MMDeviceEnumerator();
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
				Console.ReadLine();
				return;
			}

			Console.WriteLine("Recording from " + input.FriendlyName + " to " + output.FriendlyName + ". Press Enter to exit.");
			var outputFormat = output.AudioClient.MixFormat;
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

		public static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider waveProvider) {
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
	}
}

	//class RecordingSampleProvider : SampleProviderConverterBase {
	//	private readonly IWaveIn input;
	//	private readonly float[] buffer;

	//	public WaveFormat WaveFormat => input.WaveFormat;

	//	public RecordingSampleProvider(IWaveIn input) {
	//		this.input = input;
	//		this.buffer = new float[4096];
	//	}

	//	public void StartRecording() {
	//		this.input.DataAvailable += Input_DataAvailable;
	//		this.input.StartRecording();
	//	}

	//	public void StopRecording() {
	//		this.input.StopRecording();
	//		this.input.DataAvailable -= Input_DataAvailable;
	//	}

	//	private void Input_DataAvailable(object sender, WaveInEventArgs e) {
	//		read = e.BytesRecorded;
	//		int framesAvailable = read / reader.WaveFormat.Channels;
	//		float[] inBuffer;
	//		int inBufferOffset;
	//		int inNeeded = resampler.ResamplePrepare(framesAvailable, writer.OutputWaveFormat.Channels,
	//			out inBuffer, out inBufferOffset);
	//	}

	//	public int Read(float[] buffer, int offset, int count) {
	//		// TODO: change buffer if not fit
	//	}
	//}

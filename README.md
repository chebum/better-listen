# Better "Listen" implementation for Windows

Windows allows you to listen to an audio device and output recorded sound into another output device. Unfortunately, if sample rates do not match, the sound quality gets noticeable worse. Windows 10 as of build 1909 use very quick and dirty algorithm to change audio sample rate.

This console app does the same - it listens to an input device of your choice and outputs recorded audio into another audio output device. Instead of quick and dirty resampling, it uses one of the best algorithms available. We use resampler from WDL library powering Reaper DAW. Here you can find test results for it: [http://src.infinitewave.ca/](http://src.infinitewave.ca/)

## Usage

    wdlresampler input output
	
Running app without arguments will list all available audio devices. You can specify a portion of audio device name. For example, if your input device is called "Internal Microphone (Cirrus Logic CS4208)" and your output device is "Digital Audio (S/PDIF)", you can start recording using the following command:

    wdlresampler Mic Digital
	
	
## Restarting App In Case Of Crash

It's recommended to use this app with a guardian app that will restart the resampler in case of unexpected crash. You can do this either using a Windows command line, or use another app to guard the process.

Create a bat file:

    @echo off
    :start
    start /w "wdlresampler input output"
    goto start
	
Now, when you start the batch file, it will monitor the app for potential crashes and restart it when necessary.

Another option is to use free `supervisor` tool [https://github.com/chebum/Supervisor](https://github.com/chebum/Supervisor). It allows to restart the crashed app, plus it allows to monitor two or more apps at once and it will automatically close these apps when supervisor's window is closed.

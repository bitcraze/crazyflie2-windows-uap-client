# crazyflie2-windows-uap-client
Windows 10 UWP (formerly UAP) application for controlling a Crazyflie 2.0 Quadcopter from a bluetooth 4.0 enabled windows phone, PC, or HoloLens.

## Building the project

The project requires VisualStudio 2015. It is working with the free
[Visual Studio 2015 Community](https://www.visualstudio.com/products/visual-studio-community-vs).

If VS2015 is not already installed, install it with default options and open the project.
VS2015 will launch the installer to install the required modules.

## Running

You should set your Windows device in developer mode in order to be able to launch the app.
The option can be found in settings->Update & Security->For developers. Choose "Developer mode" under "Use developer features".

On Windows the Crazyflie should be paired to Windows to be able to connect to it.
For that you need to build and flash the latest development version of the [Crazyflie 2 NRF firmware](https://github.com/bitcraze/crazyflie2-nrf-firmware) master branch.

## HoloLens Support

This app has the ability to support controlling the Crazyflie with hand gestures. Currently, a code change and a recompile is required.
See MainPage.xaml.cs line 23-30: Comment line 28 which creates a FlightController object and uncomment line 30 which creates a GestureController object.

To fly with hand gestures, use the manipulation gesture (tap and hold followed by movement, such as when moving a hologram around in space). Hand movements up and down along the vertical axis control thrust. Movements side to side control roll. Movements forward and backward control pitch.

## Contribute
Go to the [contribute page](https://www.bitcraze.io/contribute/) on our website to learn more.


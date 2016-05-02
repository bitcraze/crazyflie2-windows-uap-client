# crazyflie2-windows-uap-client
Windows 10 UAP application for controlling a Crazyflie 2.0 Quadcopter from a bluetooth 4.0 enabled phone or PC

## Building the project

The project requires VisualStudio 2015. It is working with the free
[Visual Studio 2015 Community](https://www.visualstudio.com/products/visual-studio-community-vs).

If VS2015 is not already installed, install it with default options and open the project.
VS2015 will launch the installer to install the required modules.

## Running

You should set your Windows device in developper mode in order to be able to launch the app.
The option can be found in the settings.

On Windows the Crazyflie should be paired to Windows to be able to connect to it.
For that you need to build and flash the latest development version of the [Crazyflie 2 NRF firmware](https://github.com/bitcraze/crazyflie2-nrf-firmware) master branch.

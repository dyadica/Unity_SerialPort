# Unity_SerialPort Example Project (2022)

![The UnitySerialPort](https://dyadica.github.io/wp-content/uploads/2022/02/21/SP_Full.png)

This repository contains an example Unity3D project that shows the use of serial communication between Unity and a configurable COM port. This in turn allows for the use of custom hardware developed on platforms such as Arduino and Netduino to be used within Unity applications. In addition to the example Unity3D files also included is an Arduino sample which can be used to test the serialports functionality.

## Updates (Threading and Coroutine)
* 22.02.22: Added data read options (ReadLine and ReadTo)
* 03.02.22: An updated version of the Unity_SerialPort so that it now works with both Unity 2021 and the ESP32 Microcontrollers (Standard Port). 

## Overview
The Unity_SerialPort.cs script allows for a port to be opened using either a seperate thread, or a coroutine to listen for incoming data. Using an external thread to call the Unity API can be a little tricky; however there are a few workarounds! 

Full documentation coming soon (website currently down) but for now provided in script as comments.


## Unity_SerialPort (Coroutine Only) 2013-2016

For more information on the use and install of the files following download check out my blog: dyadica.github.io/blog.

Direct links:

* Unity - https://dyadica.github.io/blog/adding-events-to-the-serialport-script/
* Unity - https://dyadica.github.io/blog/unity3d-serialport-script/
* Arduino -https://dyadica.github.io/blog/simple-serial-string-parsing/

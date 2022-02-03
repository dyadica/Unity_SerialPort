# Unity Serial Port (Threading and Coroutine) 2022

A "Finally" updated version of my [old](https://github.com/dyadica/Unity_SerialPort/commit/31359117115245526634b6f4beea60ea96674421) Unity_SerialPort implementation so that it works with both Unity 2021 and the ESP32 Microcontrollers (Standard Port). 

The Unity_SerialPort.cs script allows for comms to be opened using either a seperate thread or coroutine to listen for incoming data. Using a thread for this can be tricky to call the Unity API from another thread; however there are a few workarounds! 



## Unity_SerialPort (Coroutine Only) 2013-2016

This [repository](https://github.com/dyadica/Unity_SerialPort/commit/31359117115245526634b6f4beea60ea96674421) presents a script and or prefab developed to allow drag and drop capability for serial communication within the Unity3D game development ecosystem. This in turn allows for the use of  custom gaming controllers and hardware developed on platforms such as Arduino and Netduino within Unity applications.

In addition to the example Unity3D files also included is an Arduino sample which can be used to test the serialports functionality.

For more information on the use and install of the files following download check out my blog: www.dyadica.co.uk

Direct links:

Unity - http://www.dyadica.co.uk/journal/adding-events-to-the-serialport-script/

Unity - http://www.dyadica.co.uk/journal/unity3d-serialport-script/

Arduino - http://www.dyadica.co.uk/journal/simple-serial-string-parsing/

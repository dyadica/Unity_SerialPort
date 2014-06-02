// <copyright file="UnitySerialPort.cs" company="dyadica.co.uk">
// Copyright (c) 2010, 2014 All Right Reserved, http://www.dyadica.co.uk

// This source is subject to the dyadica.co.uk Permissive License.
// Please see the http://www.dyadica.co.uk/permissive-license file for more information.
// All other rights reserved.

// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>

// <author>SJB</author>
// <email>SJB@dyadica.co.uk</email>
// <date>04.09.2013</date>
// <summary>A MonoBehaviour type class containing several functions which can be utilised 
// to perform serial communication within Unity3D</summary>

// This code was updated 04.03.2014 to include Notification Events. Please see:
// http://www.dyadica.co.uk/journal/adding-events-to-the-serialport-script for
// more information.

using UnityEngine;
using System.Collections;

using System.IO;
using System.IO.Ports;
using System;

using System.Threading;

// needed for invoke
using System.ComponentModel;

public class UnitySerialPort : MonoBehaviour 
{
    // Init a static reference if script is to be accessed by others when used in a 
    // none static nature eg. its dropped onto a gameObject. The use of "Instance"
    // allows access to public vars as such as those available to the unity editor.

    public static UnitySerialPort Instance;

    #region Properties

    // The serial port

    public SerialPort SerialPort;

    // The script update can now only run as a standard 
    // coroutine. All reference to threading has been 
    // removed!

    // List of all baudrates available to the arduino platform

    private ArrayList baudRates =
        new ArrayList() { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200 };

    // List of all com ports available on the system

    private ArrayList comPorts =
        new ArrayList();

    // If set to true then open the port when the start
    // event is called.

    public bool OpenPortOnStart = false;

    // Holder for status report information

    private string portStatus = "";
    public string PortStatus
    {
        get { return portStatus; }
        set { portStatus = value; }
    }

    // Current com port and set of default

    public string ComPort = "COM5";

    // Current baud rate and set of default

    public int BaudRate = 38400;

    // Read and write timeouts

    public int ReadTimeout = 10;
    public int WriteTimeout = 10;

    // Property used to run/keep alive the serial thread loop

    private bool isRunning = false;
    public bool IsRunning
    {
        get { return isRunning; }
        set { isRunning = value; }
    }

    // Set the gui to show ready

    private string rawData = "Ready";
    public string RawData
    {
        get { return rawData; }
        set { rawData = value; }
    }
    
    // Storage for parsed incoming data

    private string[] chunkData;
    public string[] ChunkData
    {
        get { return chunkData; }
        set { chunkData = value; }
    }

    // Refs populated by the editor inspector for default gui
    // functionality if script is to be used in a non-static
    // context.

    public GameObject ComStatusText;
    public GameObject RawDataText;
    
    // Define a delegate for our event to use. Delegates 
    // encapsulate both an object instance and a method 
    // and are similar to c++ pointers.

    public delegate void SerialDataParseEventHandler(string[] data, string rawData);

    // Define the event that utilizes the delegate to
    // fire off a notification to all registered objs 

    public static event SerialDataParseEventHandler SerialDataParseEvent;

    // Delegate and event for serialport open notification

    public delegate void SerialPortOpenEventHandler();
    public static event SerialPortOpenEventHandler SerialPortOpenEvent;

    // Delegate and event for serialport close notification

    public delegate void SerialPortCloseEventHandler();
    public static event SerialPortCloseEventHandler SerialPortCloseEvent;

    // Delegate and event for serialport sentData notification

    public delegate void SerialPortSentDataEventHandler(string data);
    public static event SerialPortSentDataEventHandler SerialPortSentDataEvent;

    // Delegate and event for serialport sentLineData notification

    public delegate void SerialPortSentLineDataEventHandler(string data);
    public static event SerialPortSentLineDataEventHandler SerialPortSentLineDataEvent;

    public bool ShowDebugs = true;

    public bool AutoDetectArduino = false;
    public string AutoDetectMessage = "Arduino";
    public string HandshakeMessage = "Unity3D";

    #endregion Properties

    #region Unity Frame Events

    /// <summary>
    /// The awake call is used to populate refs to the gui elements used in this 
    /// example. These can be removed or replaced if needed with bespoke elements.
    /// This will not affect the functionality of the system. If we are using awake
    /// then the script is being run non staticaly ie. its initiated and run by 
    /// being dropped onto a gameObject, thus enabling the game loop events to be 
    /// called e.g. start, update etc.
    /// </summary>
    void Awake()
    {
        // Define the script Instance

        Instance = this;

        // If we have used the editor inspector to populate any included gui
        // elements then lets initiate them and set some default values.

        // Details if the port is open or closed

        if (ComStatusText != null)
        { ComStatusText.guiText.text = "ComStatus: Closed"; }
    }

    /// <summary>
    /// The start call is used to populate a list of available com ports on the
    /// system. The correct port can then be selected via the respective guitext
    /// or a call to UpdateComPort();
    /// </summary>
    void Start()
    {
        // Register for a notification of the SerialDataParseEvent

        SerialDataParseEvent += 
            new SerialDataParseEventHandler(UnitySerialPort_SerialDataParseEvent);

        // Register for a notification of the open port event

        SerialPortOpenEvent += 
            new SerialPortOpenEventHandler(UnitySerialPort_SerialPortOpenEvent);

        // Register for a notification of the close port event

        SerialPortCloseEvent +=
            new SerialPortCloseEventHandler(UnitySerialPort_SerialPortCloseEvent);

        // Population of comport list via system.io.ports

        PopulateComPorts();

        // If set to true then we will try to autodetect
        // the arduino and open the port.

        if (AutoDetectArduino)
        {
            AutoDetectArduinoComPort();
        }
        else
        {
            // If set to true then open the port. You must 
            // ensure that the port is valid etc. for this! 

            if (OpenPortOnStart) { OpenSerialPort(); } 
        }        
    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed.
    /// OnDestroy will only be called on game objects that have previously
    /// been active.
    /// </summary>
    void OnDestroy()
    {
        // If we are registered for a notification of the 
        // SerialPort events then remove the registration

        if (SerialDataParseEvent != null)
            SerialDataParseEvent -= UnitySerialPort_SerialDataParseEvent;

        if (SerialPortOpenEvent != null)
            SerialPortOpenEvent -= UnitySerialPort_SerialPortOpenEvent;

        if (SerialPortCloseEvent != null)
            SerialPortCloseEvent -= UnitySerialPort_SerialPortCloseEvent;
    }

    /// <summary>
    /// The update frame call is used to provide caps for sending data to the arduino
    /// triggered via keypress. This can be replaced via use of the static functions
    /// SendSerialData() & SendSerialDataAsLine(). Additionaly this update uses the
    /// RawData property to update the gui. Again this can be removed etc.
    /// </summary>
    void Update()
    {
        // Check if the serial port exists and is open
        if (SerialPort == null || SerialPort.IsOpen == false) { return; }

        // Example calls from system to the arduino. For more detail on the
        // structure of the calls see: http://www.dyadica.co.uk/journal/simple-serial-string-parsing/
        try
        {
            // Example of sending space press event to arduino
            if (Input.GetKeyDown("space"))
            { SerialPort.WriteLine(""); }

            // Example of sending key 1 press event to arduino.
            // The "A,1" string will call functionA and pass a
            // char value of 1
            if (Input.GetKeyDown(KeyCode.Alpha1))
            { SerialPort.WriteLine("A,1"); }

            // Example of sending key 1 press event to arduino.
            // The "A,2" string will call functionA and pass a
            // char value of 2
            if (Input.GetKeyDown(KeyCode.Alpha2))
            { SerialPort.WriteLine("A,2"); }
        }
        catch (Exception ex)
        {
            // Failed to send serial data
            Debug.Log("Error 6: " + ex.Message.ToString());
        }

        try
        {
            // If we have set a GUI Text object then update it. This can only be
            // run on the thread that initialised the object thus cnnot be run
            // in the ParseSerialData() call below... Unless run as a coroutine!

            // I have also included a raw data example which is called from a
            // seperate script... see RawDataExample.cs

            if (RawDataText != null)
                RawDataText.guiText.text = RawData;
        }
        catch (Exception ex)
        {
            // Failed to update serial data
            Debug.Log("Error 7: " + ex.Message.ToString());
        }
    }

    /// <summary>
    /// Clean up the thread and close the port on application close event.
    /// </summary>
    void OnApplicationQuit()
    {
        // Call to cloase the serial port
        CloseSerialPort();

        Thread.Sleep(500);

        StopSerialCoroutine();
      
        Thread.Sleep(500);
    }

    #endregion Unity Frame Events

    #region Notification Events

    /// <summary>
    /// Data parsed serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    /// <param name="RawData">string[]</param>
    void UnitySerialPort_SerialDataParseEvent(string[] Data, string RawData)
    {
        if (ShowDebugs) 
            print("Data Recieved via port: " + RawData);
    }

    /// <summary>
    /// Open serialport notification event
    /// </summary>
    void UnitySerialPort_SerialPortOpenEvent()
    {
        if (ShowDebugs) 
            print("The serialport is now open!");
    }

    /// <summary>
    /// Close serialport notification event
    /// </summary>
    void UnitySerialPort_SerialPortCloseEvent()
    {
        if (ShowDebugs) 
            print("The serialport is now closed!");
    }

    /// <summary>
    /// Send data serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    void UnitySerialPort_SerialPortSentDataEvent(string Data)
    {
        if (ShowDebugs)
            print("Sent data: " + Data);
    }

    /// <summary>
    /// Send data with "\n" serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    void UnitySerialPort_SerialPortSentLineDataEvent(string Data)
    {
        if (ShowDebugs)
            print("Sent data as line: " + Data);
    }

    #endregion Notification Events

    #region Object Serial Port

    /// <summary>
    /// Opens the defined serial port and starts the serial thread used
    /// to catch and deal with serial events.
    /// </summary>
    public void OpenSerialPort()
    {
        try
        {
            // Initialise the serial port
            SerialPort = new SerialPort(ComPort, BaudRate);

            SerialPort.ReadTimeout = ReadTimeout;

            SerialPort.WriteTimeout = WriteTimeout;

            // Open the serial port
            SerialPort.Open();

            // Initialise the serial listening loop
            InitialiseTheSerialLoop();

        }
        catch (Exception ex)
        {
            // Failed to open com port or start serial thread
            Debug.Log("Error 1: " + ex.Message.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void InitialiseTheSerialLoop()
    {
        // Update the gui if applicable
        if (Instance != null && Instance.ComStatusText != null)
        { Instance.ComStatusText.guiText.text = "ComStatus: Open"; }

        if (isRunning == false)
        {
            StartSerialCoroutine();
        }
        else
        {
            isRunning = false;

            // Give it chance to timeout
            Thread.Sleep(100);

            try
            {
                // Kill it just in case
                StopCoroutine("SerialCoroutineLoop");
            }
            catch (Exception ex)
            {
                if (ShowDebugs)
                    print("Error N: " + ex.Message.ToString());
            }

            // Restart it once more
            StartSerialCoroutine();
        }

        if (ShowDebugs)
            print("SerialPort successfully opened!");

        // Trigger a port open notification

        if (SerialPortOpenEvent != null)
            SerialPortOpenEvent();
    }

    /// <summary>
    /// Cloases the serial port so that changes can be made or communication
    /// ended.
    /// </summary>
    public void CloseSerialPort()
    {
        try
        {
            // Close the serial port
            SerialPort.Close();

            // Update the gui if applicable
            if (Instance.ComStatusText != null)
            { Instance.ComStatusText.guiText.text = "ComStatus: Closed"; }
        }
        catch (Exception ex)
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
            {
                // Failed to close the serial port. Uncomment if
                // you wish but this is triggered as the port is
                // already closed and or null.

                // Debug.Log("Error 2A: " + "Port already closed!");
            }
            else
            {
                // Failed to close the serial port
                Debug.Log("Error 2B: " + ex.Message.ToString());
            }
        }

        if (ShowDebugs)
            print("Serial port closed!");

        // Trigger a port closed notification

        if (SerialPortCloseEvent != null)
            SerialPortCloseEvent();
    }

    #endregion Object Serial Port

    #region Serial Coroutine

    /// <summary>
    /// Function used to start coroutine for reading serial 
    /// data.
    /// </summary>
    public void StartSerialCoroutine()
    {
        isRunning = true;

        StartCoroutine("SerialCoroutineLoop");
    }

    private void AutoDetectArduinoComPort()
    {
        SerialPort = new SerialPort();

        foreach (string cPort in System.IO.Ports.SerialPort.GetPortNames())
        {
            SerialPort.Close();

            bool portfound = false;
            SerialPort.PortName = cPort;
            SerialPort.BaudRate = BaudRate;

            try
            {
                SerialPort.Open();
                print("Trying port: " + cPort);
            }
            catch
            {
                print("Invalid Port!");
                continue;
            }

            if (!portfound)
            {
                if (SerialPort.IsOpen) // Port has been opened properly...
                {
                    // We have a long timeout here to account for
                    // the microcontroller rebooting upon serial
                    // connection attempts.

                    SerialPort.ReadTimeout = 2000;

                    // Really this should be run in a Thread as not to
                    // hang up the playback of the Unity3D application.

                    SerialPort.WriteTimeout = WriteTimeout;

                    print("Attempting to open port " + SerialPort.PortName);

                    try
                    {
                        print("Waiting for a response from controller: " + SerialPort.PortName);

                        string comms = SerialPort.ReadLine();

                        // We have found the arduino!

                        if (comms == AutoDetectMessage)
                        {
                            print(SerialPort.PortName + " Opened Successfully!");

                            // Reset the timeout to that defined in 
                            // the unity editor

                            SerialPort.ReadTimeout = ReadTimeout;

                            print("Initialising the listen loop");                            

                            InitialiseTheSerialLoop();
                        }
                        else
                        {
                            print("Port Not Found! Please cycle controller power and try again");

                            SerialPort.Close();
                        }
                    }
                    catch (Exception)
                    {
                        print("Incorrect Port! Trying again...");

                        SerialPort.Close();
                    }
                }
            }
        }
    }

    /// <summary>
    /// A Coroutine used to recieve serial data thus not 
    /// affecting generic unity playback etc.
    /// </summary>
    public IEnumerator SerialCoroutineLoop()
    {
        while (isRunning)
        {
            GenericSerialLoop();
            yield return null;
        }

        if (ShowDebugs)
            print("Ending Coroutine!");
    }

    /// <summary>
    /// Function used to stop the coroutine and kill
    /// off any instance
    /// </summary>
    public void StopSerialCoroutine()
    {
        isRunning = false;

        Thread.Sleep(100);

        try
        {
            StopCoroutine("SerialCoroutineLoop");
        }
        catch (Exception ex)
        {
            if (ShowDebugs)
                print("Error 2A: " + ex.Message.ToString());
        }

        // Reset the serial port to null
        if (SerialPort != null)
        { SerialPort = null; }

        // Update the port status... just in case :)
        portStatus = "Ended Serial Loop Coroutine!";

        if (ShowDebugs)
            print("Ended Serial Loop Coroutine!");
    }

    #endregion Serial Coroutine

    #region Static Functions

    /// <summary>
    /// Function used to send string data over serial with
    /// an included line return
    /// </summary>
    /// <param name="data">string</param>
    public void SendSerialDataAsLine(string data)
    {
        if (SerialPort != null)
        { SerialPort.WriteLine(data); }

        if (ShowDebugs)
            print("Sent data: " + data);

        // throw a sent data notification

        if (SerialPortSentDataEvent != null)
            SerialPortSentDataEvent(data);
    }

    /// <summary>
    /// Function used to send string data over serial without
    /// a line return included.
    /// </summary>
    /// <param name="data"></param>
    public void SendSerialData(string data)
    {
        if (SerialPort != null)
        { SerialPort.Write(data); }

        if (ShowDebugs)
            print("Sent data: " + data);

        // throw a sent data notification

        if (SerialPortSentLineDataEvent != null)
            SerialPortSentLineDataEvent(data);
    }

    #endregion Static Functions

    /// <summary>
    /// The serial thread loop & the coroutine loop both utilise
    /// the same code with the exception of the null return on 
    /// the coroutine, so we share it here.
    /// </summary>
    private void GenericSerialLoop()
    {
        try
        {
            // Check that the port is open. If not skip and do nothing
            if (SerialPort.IsOpen)
            {
                // Read serial data until a '\n' character is recieved
                string rData = SerialPort.ReadLine();

                // If the data is valid then do something with it
                if (rData != null && rData != "")
                {
                    // Store the raw data
                    RawData = rData;

                    if (RawData == AutoDetectMessage)
                    { SendSerialDataAsLine(HandshakeMessage); }

                    // split the raw data into chunks via ',' and store it
                    // into a string array
                    ChunkData = RawData.Split(',');

                    // Or you could call a function to do something with
                    // data e.g.
                    ParseSerialData(ChunkData, RawData);
                }
            }
        }
        catch (TimeoutException)
        {
            // This will be triggered lots with the coroutine method
        }
        catch (Exception ex)
        {
            // This could be thrown if we close the port whilst the thread 
            // is reading data. So check if this is the case!
            if (SerialPort.IsOpen)
            {
                // Something has gone wrong!
                Debug.Log("Error 4: " + ex.Message.ToString());
            }
            else
            {
                // Error caused by closing the port whilst in use! This is 
                // not really an error but uncomment if you wish.

                // Debug.Log("Error 5: Port Closed Exception!");
            }
        }
    }

    /// <summary>
    /// Function used to filter and act upon the data recieved. You can add
    /// bespoke functionality here.
    /// </summary>
    /// <param name="data">string[] of raw data seperated into chunks via ','</param>
    /// <param name="rawData">string of raw data</param>
    private void ParseSerialData(string[] data, string rawData)
    {

        // Fire a notification to all registered objects. Before we do
        // this however, first double check that we have some valid
        // data here so this only has to be performed once and not on
        // each object notified.

        if (data != null && rawData != string.Empty)
        {
            if (SerialDataParseEvent != null)
                SerialDataParseEvent(data, rawData);
        }
    }

    /// <summary>
    /// Function that utilises system.io.ports.getportnames() to populate
    /// a list of com ports available on the system.
    /// </summary>
    public void PopulateComPorts()
    {
        // Loop through all available ports and add them to the list
        foreach (string cPort in System.IO.Ports.SerialPort.GetPortNames())
        {
            comPorts.Add(cPort); // Debug.Log(cPort.ToString());
        }

        // Update the port status just in case :)
        portStatus = "ComPort list population complete";
    }

    //public void AutoDetectArduinoComPort()
    //{
    //    StartCoroutine(DetectionCoroutineLoop());
    //}

    /// <summary>
    /// Function used to update the current selected com port
    /// </summary>
    public string UpdateComPort()
    {
        // If open close the existing port
        if (SerialPort != null && SerialPort.IsOpen)
        { CloseSerialPort(); }

        // Find the current id of the existing port within the
        // list of available ports
        int currentComPort = comPorts.IndexOf(ComPort);

        // check against the list of ports and get the next one.
        // If we have reached the end of the list then reset to zero.
        if (currentComPort + 1 <= comPorts.Count - 1)
        {
            // Inc the port by 1 to get the next port
            ComPort = (string)comPorts[currentComPort + 1];
        }
        else
        {
            // We have reached the end of the list reset to the
            // first available port.
            ComPort = (string)comPorts[0];
        }

        // Update the port status just in case :)
        portStatus = "ComPort set to: " + ComPort.ToString();

        // Return the new ComPort just in case
        return ComPort;
    }

    /// <summary>
    /// Function used to update the current baudrate
    /// </summary>
    public int UpdateBaudRate()
    {
        // If open close the existing port
        if (SerialPort != null && SerialPort.IsOpen)
        { CloseSerialPort(); }

        // Find the current id of the existing rate within the
        // list of defined baudrates
        int currentBaudRate = baudRates.IndexOf(BaudRate);

        // check against the list of rates and get the next one.
        // If we have reached the end of the list then reset to zero.
        if (currentBaudRate + 1 <= baudRates.Count - 1)
        {
            // Inc the rate by 1 to get the next rate
            BaudRate = (int)baudRates[currentBaudRate + 1];
        }
        else
        {
            // We have reached the end of the list reset to the
            // first available rate.
            BaudRate = (int)baudRates[0];
        }

        // Update the port status just in case :)
        portStatus = "BaudRate set to: " + BaudRate.ToString();

        // Return the new BaudRate just in case
        return BaudRate;
    }
}

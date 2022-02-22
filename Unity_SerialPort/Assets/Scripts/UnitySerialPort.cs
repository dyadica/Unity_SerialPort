// <copyright file="UnitySerialPort.cs" company="dyadica.co.uk">
// Copyright (c) 2010, 2014 All Right Reserved, http://www.dyadica.co.uk

// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>

// <author>SJB</author>
// <email>github@dyadica.co.uk</email>
// <date>04.09.2013</date>
// <summary>A MonoBehaviour type class containing several functions which can be utilised 
// to perform serial communication within Unity3D</summary>

// This code was updated 04.03.2014 to include Notification Events. Please see:
// http://www.dyadica.co.uk/journal/adding-events-to-the-serialport-script for
// more information.

// This code was updated 03.02.2022 to include Notification Events. My website is
// down so please see the readme.md for more information!

using UnityEngine;
using System.Collections;

using System.IO.Ports;
using System;

using System.Threading;

using System.Collections.Generic;

// new Text Mesh Pro text
using TMPro;

public class UnitySerialPort : MonoBehaviour
{
    // Init a static reference if script is to be accessed by others when used in a 
    // none static nature eg. its dropped onto a gameObject. The use of "Instance"
    // allows access to public vars as such as those available to the unity editor.

    public static UnitySerialPort Instance;

    #region Properties

    // The serial port

    public SerialPort SerialPort;

    // Thread for thread version of port
    Thread SerialLoopThread;

    [Header("SerialPort")]

    // Current com port and set of default
    public string ComPort = "COM5";

    // Current baud rate and set of default
    // 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200
    public int BaudRate = 115200;

    // The parity-checking protocol.
    public Parity Parity = Parity.None;

    // The standard number of stopbits per byte.
    public StopBits StopBits = StopBits.One;

    // The standard length of data bits per byte.
    public int DataBits = 8;

    // The state of the Data Terminal Ready(DTR) signal during serial communication.
    public bool DtrEnable;
    
    // Whether or not the Request to Send(RTS) signal is enabled during serial communication.
    public bool RtsEnable;    

    // Holder for status report information

    private string portStatus = "";
    public string PortStatus
    {
        get { return portStatus; }
        set { portStatus = value; }
    }

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

    [Header("GUI Fields")]

    // Refs populated by the editor inspector for default gui
    // functionality if script is to be used in a non-static
    // context.

    public TMP_Text ComStatusText;
    public TMP_Text RawDataText;
    public TMP_Text StatusMsgBox;

    // public TMP_InputField OutputString;

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
   
    public enum LoopMethods
    { Threading, Coroutine }

    [Header("Options")]
    [SerializeField]
    public LoopMethods LoopMethod =
        LoopMethods.Coroutine;

    // If set to true then open the port when the start
    // event is called.

    public bool OpenPortOnStart = false;
    public bool ShowDebugs = true;
    
    // List of all com ports available on the system

    private ArrayList comPorts =
        new ArrayList();

    [Header("Misc")]
    public List<string> ComPorts =
        new List<string>();

    [Header("Data Read")]    

    public ReadMethod ReadDataMethod = 
        ReadMethod.ReadLine;
    public enum ReadMethod
    {
        ReadLine,
        ReadToChar
    }

    public string Delimiter;
    public char Separator;

   


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
        { ComStatusText.text = "ComStatus: Closed"; }
    }

    /// <summary>
    /// The start call is used to populate a list of available com ports on the
    /// system. The correct port can then be selected via the respective guitext
    /// or a call to UpdateComPort();
    /// </summary>
    void Start()
    {
        // Register for a notification of the open port event

        SerialPortOpenEvent +=
            new SerialPortOpenEventHandler(UnitySerialPort_SerialPortOpenEvent);

        // Register for a notification of the close port event

        SerialPortCloseEvent +=
            new SerialPortCloseEventHandler(UnitySerialPort_SerialPortCloseEvent);

        // Register for a notification of data sent

        SerialPortSentDataEvent +=
            new SerialPortSentDataEventHandler(UnitySerialPort_SerialPortSentDataEvent);

        // Register for a notification of data sent

        SerialPortSentLineDataEvent +=
            new SerialPortSentLineDataEventHandler(UnitySerialPort_SerialPortSentLineDataEvent);

        // Register for a notification of the SerialDataParseEvent

        SerialDataParseEvent +=
            new SerialDataParseEventHandler(UnitySerialPort_SerialDataParseEvent);

        // Population of comport list via system.io.ports

        PopulateComPorts();

        // If set to true then open the port. You must 
        // ensure that the port is valid etc. for this! 

        if (OpenPortOnStart) { OpenSerialPort(); }
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

        if (SerialPortSentDataEvent != null)
            SerialPortSentDataEvent -= UnitySerialPort_SerialPortSentDataEvent;

        if (SerialPortSentLineDataEvent != null)
            SerialPortSentLineDataEvent -= UnitySerialPort_SerialPortSentLineDataEvent;
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
        // structure of the calls see:
        // http://www.dyadica.co.uk/journal/simple-serial-string-parsing/

        //try
        //{
        //    // Here we have some sample usage scenarios that
        //    // demo the operation of the UnitySerialPort. In
        //    // order to use these you must first ensure that
        //    // the custom inputs are defined via:

        //    // Edit > Project Settings > Input

        //    if (Input.GetButtonDown("SendData"))
        //    { SendSerialDataAsLine(OutputString.text); }

        //    // Example of sending key 1 press event to arduino.
        //    // The "A,1" string will call functionA and pass a
        //    // char value of 1
        //    if (Input.GetButtonDown("Key1"))
        //    { SendSerialDataAsLine("A,1"); }

        //    // Example of sending key 1 press event to arduino.
        //    // The "A,2" string will call functionA and pass a
        //    // char value of 2
        //    if (Input.GetButtonDown("Key2"))
        //    { SendSerialDataAsLine("A,2"); }

        //    // Example of sending space press event to arduino
        //    if (Input.GetButtonDown("Key3"))
        //    { SendSerialDataAsLine(""); }
        //}
        //catch (Exception ex)
        //{
        //    // Failed to send serial data
        //    Debug.Log("Error 6: " + ex.Message.ToString());
        //}

        try
        {
            // If we have set a GUI Text object then update it. This can only be
            // run on the thread that initialised the object thus cnnot be run
            // in the ParseSerialData() call below... Unless run as a coroutine!

            // I have also included other raw data examples in GUIManager.cs         

            // RawDataText is null/none by default for examples (see GUIManager.cs)
            if (RawDataText != null)
                RawDataText.text = RawData; 
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

        Thread.Sleep(100);

        if (LoopMethod == LoopMethods.Coroutine)
            StopSerialCoroutine();

        if (LoopMethod == LoopMethods.Threading)
            StopSerialThreading();

        Thread.Sleep(100);
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
        // Not fired via portStatus to avoid hiding other messages from the GUI
        if (ShowDebugs)
            print("Data Recieved via port: " + RawData);
    }

    /// <summary>
    /// Open serialport notification event
    /// </summary>
    void UnitySerialPort_SerialPortOpenEvent()
    {
        portStatus = "The serialport:" + ComPort + " is now open!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    /// <summary>
    /// Close serialport notification event
    /// </summary>
    void UnitySerialPort_SerialPortCloseEvent()
    {
        portStatus = "The serialport:" + ComPort + " is now closed!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    /// <summary>
    /// Send data serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    void UnitySerialPort_SerialPortSentDataEvent(string Data)
    {
        portStatus = "Sent data: " + Data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    /// <summary>
    /// Send data with "\n" serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    void UnitySerialPort_SerialPortSentLineDataEvent(string Data)
    {
        portStatus = "Sent data as line: " + Data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
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
            SerialPort = new SerialPort(ComPort, BaudRate, Parity, DataBits, StopBits);

            SerialPort.ReadTimeout = ReadTimeout;
            SerialPort.WriteTimeout = WriteTimeout;

            SerialPort.DtrEnable = DtrEnable;
            SerialPort.RtsEnable = RtsEnable;

            // Open the serial port
            SerialPort.Open();

            // Update the gui if applicable
            if (Instance != null && Instance.ComStatusText != null)
            { Instance.ComStatusText.text = "ComStatus: Open"; }

            if (LoopMethod == LoopMethods.Coroutine)
            {
                if (isRunning)
                {
                    // TCoroutine is already running so kill it!?
                    StopSerialCoroutine();
                }

                // Restart it once more
                StartSerialCoroutine();
            }

            if (LoopMethod == LoopMethods.Threading)
            {
                if (isRunning)
                {
                    // Thread is already running so kill it!?
                    StopSerialThreading();
                }

                // Restart it once more
                StartSerialThread();
            }

            portStatus = "The serialport is now open!";

            if (ShowDebugs)
                ShowDebugMessages(portStatus);
        }
        catch (Exception ex)
        {
            // Failed to open com port or start serial thread
            Debug.Log("Error 1: " + ex.Message.ToString());
        }

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
            { Instance.ComStatusText.text = "ComStatus: Closed"; }
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

        if (LoopMethod == LoopMethods.Coroutine)
            StopSerialCoroutine();

        if (LoopMethod == LoopMethods.Threading)
            StopSerialThreading();

        portStatus ="Serial port closed!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);

        // Trigger a port closed notification

        if (SerialPortCloseEvent != null)
            SerialPortCloseEvent();
    }

    #endregion Object Serial Port

    #region Serial Threading

    void StartSerialThread()
    {
        isRunning = true;

        SerialLoopThread = new Thread(SerialThreadLoop);
        SerialLoopThread.Start();
    }

    void SerialThreadLoop()
    {
        while (isRunning)
        {
            if (isRunning == false)
                break;

            // Run the generic loop
            GenericSerialLoop();
        }

      portStatus = "Ending Serial Thread!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    /// <summary>
    /// Function used to stop the thread and "over" kill
    /// off any instance
    /// </summary>
    public void StopSerialThreading()
    {
        isRunning = false;

        // this should timeout the thread

        Thread.Sleep(100);

        // otherwise...

        if (SerialLoopThread != null && SerialLoopThread.IsAlive)
            SerialLoopThread.Abort();

        Thread.Sleep(100);

        if (SerialLoopThread != null)
            SerialLoopThread = null;

        // Reset the serial port to null

        if (SerialPort != null)
        { SerialPort = null; }

        // Update the port status... just in case :)

        portStatus = "Ended Serial Loop Thread!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    #endregion Serial Threading

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

       portStatus = "Ending Coroutine!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
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
            portStatus = "Error 2A: " + ex.Message.ToString();

            if (ShowDebugs)
                ShowDebugMessages(portStatus);
        }

        // Reset the serial port to null
        if (SerialPort != null)
        { SerialPort = null; }

        // Update the port status... just in case :)
        portStatus = "Ended Serial Loop Coroutine!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    #endregion Serial Coroutine

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
                // Read serial data until...

                string rData = string.Empty;

                // swap between the ReadLine or ReadTo
                switch (ReadDataMethod)
                {
                    case ReadMethod.ReadLine:
                        rData = SerialPort.ReadLine();
                        break;
                    case ReadMethod.ReadToChar:
                        rData = SerialPort.ReadTo(Delimiter);
                        break;
                }

                // If the data is valid then do something with it
                if (rData != null && rData != "")
                {
                    // Store the raw data
                    RawData = rData;
                    // split the raw data into chunks via ',' and store it
                    // into a string array
                    ChunkData = RawData.Split(Separator);

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

                Debug.Log("Error 5: Port Closed Exception!");
            }
        }
    }

    #region Methods

    /// <summary>
    /// Function used to send string data over serial with
    /// an included line return
    /// </summary>
    /// <param name="data">string</param>
    public void SendSerialDataAsLine(string data)
    {
        if (SerialPort != null)
        { SerialPort.WriteLine(data); }

        portStatus = "Sent data: " + data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);

        // throw a sent data notification

        if (SerialPortSentLineDataEvent != null)
            SerialPortSentLineDataEvent(data);        
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

        portStatus = "Sent data: " + data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);

        // throw a sent data notification

        if (SerialPortSentDataEvent != null)
            SerialPortSentDataEvent(data);
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
        foreach (string cPort in SerialPort.GetPortNames())
        {
            ComPorts.Add(cPort);

            comPorts.Add(cPort);

            // Debug.Log(cPort.ToString());
        }

        // Update the port status just in case :)
        portStatus = "ComPort list population complete";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

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

        if (ShowDebugs)
            ShowDebugMessages(portStatus);

        // Return the new ComPort just in case
        return ComPort;
    }

    /// <summary>
    /// What it says on the tin!
    /// </summary>
    /// <param name="portStatus">string</param>
    public void ShowDebugMessages(string portStatus)
    {
        if (StatusMsgBox != null)
            StatusMsgBox.text = portStatus;

        print(portStatus);
    }

    #endregion Methods
}

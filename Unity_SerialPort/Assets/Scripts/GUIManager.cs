// <copyright file="GUIManager.cs" company="dyadica.co.uk">
// Copyright (c) 2010, 2014, 2022 All Right Reserved, http://www.dyadica.co.uk

// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>

// <author>SJB</author>
// <email>github@dyadica.co.uk</email>
// <date>04.09.2013</date>
// <summary>A MonoBehaviour type class containing an example GUI that can be used to 
// communicate with the UnitySerialPort.cs script</summary>

// This code was updated 03.02.2022 to include Notification Events. My website is
// down so please see the readme.md for more information!

using UnityEngine;

// new Text Mesh Pro text
using TMPro;
using System.Collections.Generic;

public class GUIManager : MonoBehaviour
{
    // Init a static reference if script is to be accessed by others when used in a 
    // none static nature eg. its dropped onto a gameObject. The use of "Instance"
    // allows access to public vars as such as those available to the unity editor.

    public static GUIManager Instance;

    // A reference to the UnitySerialPort.cs Instance

    private UnitySerialPort unitySerialPort;

    // GUI Text fields, raw data, event data

    public TMP_Text RawDataGUI;
    public TMP_Text EvtDataGUI;

    // GUI Button, open/close serial port

    public TMP_Text ComButton;

    // GUI Text field, status

    public TMP_Text StatusMsgBox;

    // GUI Inputfield, output text

    public TMP_InputField OutputString;

    // Property to sore the incomming data so it is
    // accessible via the unity main loop/thread

    private string EvtDataString;

    // An example of using the parsed data
    public int[] ParsedEvtData;

    /// <summary>
    /// Use this for before initialization
    /// </summary>
    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Use this for initialization
    /// </summary>
	void Start()
    {
        // Register reference to the UnitySerialPort. This
        // was defined in the scripts Awake function so we
        // know it is instantiated before this call.

        unitySerialPort = UnitySerialPort.Instance;

        // Register an event for data reception!

        UnitySerialPort.SerialDataParseEvent += 
            UnitySerialPort_SerialDataParseEvent;
    }

    /// <summary>
    /// Data parsed serialport notification event
    /// </summary>
    /// <param name="data">string</param>
    /// <param name="rawData">string[]</param>
    private void UnitySerialPort_SerialDataParseEvent(string[] data, string rawData)
    {
        // If we are using the threading method we can set local variable
        // via an event and this can then be used for GUI etc.

        // EvtDataString = "Evt: " + rawData; // e.g.1

        // If we try to access the data directly it will cause an error as
        // it originates from a separate thread e.g:
        // get_isActiveAndEnabled can only be called from the main thread.
        // un-comment to try (p.s. also comment out the equivalent update call)
        // None of this is a problem with the coroutine method as it is run on
        // the same thread!

        //if (EvtDataGUI != null)
        //    EvtDataGUI.text = rawData;

        // Here is another example showing how to obtain the data and convert it to
        // an array of ints. It also outputs each value to the GUI on a separate line
        // so that they can be easily viewed etc!

        // Create the array

        ParsedEvtData = new int[data.Length];

        // Create a string for GUI display

        string values = string.Empty;

        // Populate both the array and string using the event data

        for(int i=0; i<data.Length; i++)
        {
            // Convert the data to ints. These can be vieved
            // via the unity editor!

            int.TryParse(data[i], out ParsedEvtData[i]);

            // add to the string
            values += i + ": " + data[i];

            // check if we are at the last value and if not add a new line
            if (i != data.Length - 1)
                values += "\n";
        }

        // Update the variable so the gui can call it up the update method
        EvtDataString = values; // e.g.2
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        // If we are using the threading method we can obtain the data
        // using the properties set in the UnitySerialPort.cs. Events
        // wont work for the GUI due to the loop being a seperate thread.

        if (RawDataGUI != null)
            RawDataGUI.text = "Raw: " + unitySerialPort.RawData;

        // If we are using the threading method we can set local variable
        // via an event and this can then be used for GUI etc.

        if (EvtDataGUI != null)
            EvtDataGUI.text = EvtDataString;

        if (StatusMsgBox != null)
            StatusMsgBox.text = unitySerialPort.PortStatus;

        // Here we have some sample usage scenarios that
        // demo the operation of the UnitySerialPort. In
        // order to use these you must first ensure that
        // the custom inputs are defined via:

        // Edit > Project Settings > Input

        if (Input.GetButtonDown("SendData"))
        { unitySerialPort.SendSerialDataAsLine(OutputString.text); }

        // Example of sending key 1 press event to arduino.
        // The "A,1" string will call functionA and pass a
        // char value of 1
        if (Input.GetButtonDown("Key1"))
        { unitySerialPort.SendSerialDataAsLine("A,1"); }

        // Example of sending key 1 press event to arduino.
        // The "A,2" string will call functionA and pass a
        // char value of 2
        if (Input.GetButtonDown("Key2"))
        { unitySerialPort.SendSerialDataAsLine("B,1"); }

        // Example of sending space press event to arduino
        if (Input.GetButtonDown("Key3"))
        { unitySerialPort.SendSerialDataAsLine(""); }
    }

    /// <summary>
    /// Method that can be used to both open and close the serial port.
    /// </summary>
    public void OpenClosePort()
    {
        // Check to see if there is a port set
        if (unitySerialPort.SerialPort == null)
        {   
            // nope so best open one
            unitySerialPort.OpenSerialPort();

            // Update the gui to reflect
            if (ComButton != null)
                ComButton.text = "Close-Port";

            // exit
            return;
        }
        else
        {
            // yup so lets close it!
            unitySerialPort.CloseSerialPort();

            // Update the gui to reflect
            if (ComButton != null)
                ComButton.text = "Open-Port";

            //exit
            return;
        }
    }

    /// <summary>
    ///  Method that can be used to send serial data from the unity environment.
    /// </summary>
    public void SendSerialData()
    {
        // Send data as is
        //if (unitySerialPort.SerialPort != null && unitySerialPort.SerialPort.IsOpen)
        //    unitySerialPort.SendSerialData(OutputString.text);

        // Send data as line
        if (unitySerialPort.SerialPort != null && unitySerialPort.SerialPort.IsOpen)
            unitySerialPort.SendSerialDataAsLine(OutputString.text);
    }
}

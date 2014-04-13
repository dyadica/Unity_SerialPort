// <copyright file="GUIManager.cs" company="dyadica.co.uk">
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
// <summary>A MonoBehaviour type class containing an example GUI that can be used to 
// communicate with the UnitySerialPort.cs script</summary>

using UnityEngine;
using System.Collections;

public class GUIManager : MonoBehaviour 
{
    public static GUIManager Instance;

    private UnitySerialPort unitySerialPort;

    public GUISkin GUISkin;

    public string OutputString;

    private string PortOpenStatus;

    public GUIText RawDataGUI;
    public GUIText DataValueGUI;

    public bool ShowDebugs = true;

    /// <summary>
    /// Use this for before initialization
    /// </summary>
    void Awake ()
    {
        Instance = this;
    }

    /// <summary>
    /// Use this for initialization
    /// </summary>
	void Start () 
    {
        // Register reference to the UnitySerialPort. This
        // was defined in the scripts Awake function so we
        // know it is instantiated before this call.

        unitySerialPort = UnitySerialPort.Instance;

        // Register the script for all of the available event
        // notifications.

        UnitySerialPort.SerialDataParseEvent += 
            new UnitySerialPort.SerialDataParseEventHandler(UnitySerialPort_SerialDataParseEvent);

        // Port status events

        UnitySerialPort.SerialPortOpenEvent +=
            new UnitySerialPort.SerialPortOpenEventHandler(UnitySerialPort_SerialPortOpenEvent);

        UnitySerialPort.SerialPortCloseEvent +=
            new UnitySerialPort.SerialPortCloseEventHandler(UnitySerialPort_SerialPortCloseEvent);

        // Sent data events

        UnitySerialPort.SerialPortSentDataEvent +=
            new UnitySerialPort.SerialPortSentDataEventHandler(UnitySerialPort_SerialPortSentDataEvent);

        UnitySerialPort.SerialPortSentLineDataEvent +=
            new UnitySerialPort.SerialPortSentLineDataEventHandler(UnitySerialPort_SerialPortSentLineDataEvent);
	}
	
    /// <summary>
    /// Update is called once per frame
    /// </summary>
	void Update () 
    {
        // Check to see if we have a serial port defined
        // and if not then return.

        if (unitySerialPort.SerialPort == null)
        {
            PortOpenStatus = "Open Port"; return;
        }

        // Check to see if the serial port is open or not
        // and then set the button text "PortOpenStatus" 
        // to reflect.

        switch (unitySerialPort.SerialPort.IsOpen)
        {
            case true: PortOpenStatus = "Close Port"; break;
            case false: PortOpenStatus = "Open Port"; break;
        }

	    // Here we have some sample usage scenarios that
        // demo the operation of the UnitySerialPort. In
        // order to use these you must first ensure that
        // the custom inputs are defined via:

        // Edit > Project Settings > Input

        if (Input.GetButtonDown("SendData"))
        { unitySerialPort.SendSerialDataAsLine(OutputString); }
	}

    /// <summary>
    /// OnGUI is called once per draw
    /// </summary>
    void OnGUI ()
    {
        // If we have defined a GUI Skin via the unity 
        // editor then apply it.

        if (GUISkin != null) { GUI.skin = GUISkin; }

        // Draw an area to hold the GUI content.

        GUILayout.BeginArea(new Rect(10, 10, 200, 200), "", GUI.skin.box);

        // Draw a button that can be used to open or
        // close the serial port.

        if (GUILayout.Button(PortOpenStatus, GUILayout.Height(30)))
        {
            if (unitySerialPort.SerialPort == null)
            { unitySerialPort.OpenSerialPort(); return; }

            switch (unitySerialPort.SerialPort.IsOpen)
            {
                case true: unitySerialPort.CloseSerialPort(); break;
                case false: unitySerialPort.OpenSerialPort(); break;
            }
        }

        // Draw a title for the input textfield

        GUILayout.Label("Input string");

        // Draw a textfield that can be used to show 
        // data sent via the serial port to unity.

        GUILayout.TextField(unitySerialPort.RawData,GUILayout.Height(20));

        // Provide some padding to seperate

        GUILayout.Space(20);

        // Draw a title for the output textfield

        GUILayout.Label("Output string");

        // Draw a textfield that can be used to define 
        // data to be sent via the serial port

        OutputString = GUILayout.TextField(OutputString, GUILayout.Height(20));

        // Draw a button that can be used to send serial
        // data from the unity environment.

        if (GUILayout.Button("Send Data", GUILayout.Height(30)))
        {
            if (unitySerialPort.SerialPort.IsOpen)
                unitySerialPort.SendSerialDataAsLine(OutputString);
        }

        // Thats it we are finished so lets close the area

        GUILayout.EndArea();
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

        UnitySerialPort.SerialDataParseEvent -= 
            UnitySerialPort_SerialDataParseEvent;

        // Port status events

        UnitySerialPort.SerialPortOpenEvent -= 
            UnitySerialPort_SerialPortOpenEvent;

         UnitySerialPort.SerialPortCloseEvent -= 
             UnitySerialPort_SerialPortCloseEvent;

        // Sent data events

         UnitySerialPort.SerialPortSentDataEvent -=
             UnitySerialPort_SerialPortSentDataEvent;

         UnitySerialPort.SerialPortSentLineDataEvent -=
             UnitySerialPort_SerialPortSentLineDataEvent;
    }

    #region Notification Events

    /// <summary>
    /// Data parsed serialport notification event
    /// </summary>
    /// <param name="Data">string[]</param>
    /// <param name="RawData">string</param>
    void UnitySerialPort_SerialDataParseEvent(string[] Data, string RawData)
    {
        // print debug if ShowDebugs set to true

        if (ShowDebugs)
            print("Data Recieved via GUIManager: " + RawData);

        // If we have assigned a GUIText object to RawDataGUI then use 
        // this object to show the raw data

        if (RawDataGUI != null && Data.Length >= 2)
            RawDataGUI.text = "RawData: " + RawData;

        // If we have 2 or more values in the Data string[] then we know
        // that index[1] is a value.

        if (DataValueGUI != null && Data.Length >= 2)
            DataValueGUI.text = "ValData: " + Data[1]; 
    }

    /// <summary>
    /// Open serialport notification event
    /// </summary>
    void UnitySerialPort_SerialPortOpenEvent()
    {
        if (ShowDebugs)
            print("The serialport is now open! via GUIManager");
    }

    /// <summary>
    /// Close serialport notification event
    /// </summary>
    void UnitySerialPort_SerialPortCloseEvent()
    {
        if (ShowDebugs)
            print("The serialport is now closed! via GUIManager");
    }

    /// <summary>
    /// Send data serialport notification event
    /// </summary>
    /// <param name="Data"></param>
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
}

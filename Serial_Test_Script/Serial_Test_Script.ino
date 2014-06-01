/*

 Simple Serial String Parsing
 
 The circuit:
 * LED attached from digital pin 9 to ground.
 
 Created 11 March 2011
 By SJB
 
 http://www.dyadica.co.uk/journal/simple-serial-string-parsing
 
 Based upon code found by Tom Igoe at:
 
 http://www.arduino.cc/en/Tutorial/SerialEvent
 
 and
 
 http://arduino.cc/en/Tutorial/Fading
 
  
 This example code is in the public domain.
 
 */

// Buffer for the incoming data
char inData[100];
// Buffer for the parsed data chunks
char *inParse[100];

// Storage for data as string
String inString = "";

// Incoming data id
int index = 0;
// Read state of incoming data
boolean stringComplete = false;

// if true then show data stream
boolean showDataStream = false;

// dummy prop value to send to Unity
int dummy = 0;

// if true then run autodetection
boolean autoDetect = true;

// message sent for auto detection
String autoDetectionMessage = "Arduino";

// message used to detect handShake
String handShake = "Unity3D";

void setup() 
{
  // Delay to facilitate start up of Xbee usually about 
  // 5 seconds. Comment out if using wired serial etc.
  // If this is used then we need to account for this
  // additional delay each time the port is tested and
  // thus the microcontroller reboots.
  
  // delay(5000);
  
  // Initialise the serial port
  
  Serial.begin(9600);
}

void detectionLoop()
{
  if (stringComplete) 
  {
    // Check to see if we have been 
    // returned the handShake string
    
    if(inString = handShake)
    { 
      // If we have the handShake then set 
      // detection to false and begin play.
      
      autoDetect = false; 
      
      // Exit the loop
      return;
    } 
    
    // Reset inString to empty
    inString = "";
    
    // Reset the system for further 
    // input of data.
    
    stringComplete = false; 
  }
  
  // Send the AutoDetection string.
  
  Serial.println(autoDetectionMessage);
  
  // Pause a little to allow for
  // mesages to be sent etc.
  
  delay(100);
}

void playbackLoop()
{
  if (stringComplete) 
  {    
    // Parse the recieved data.
    
    ParseSerialData();
    
    // Reset inString to empty.
    
    inString = "";    
    
    // Reset the system for further 
    // input of data.
    
    stringComplete = false; 
  }  
  
  // Send dummy data if set to true
  // to toggle send B,1
  
  if(showDataStream)
  {
    // Send the dummy value
    
    Serial.print("D,");
    Serial.print(dummy);
    Serial.println(); 
 
    // change the dummy value 
    
    dummy++;
    
    // slow things down a little
    
    delay(50);
  }
}

void loop() 
{
  // If we are in auto connection mode then
  // run the detection loop. Otherwise run
  // the playback loop.
  
  if(autoDetect == true) { detectionLoop(); }
  else { playbackLoop(); }
}

void ParseSerialData()
{
  // The data to be parsed
  char *p = inData;
  // Temp store for each data chunk
  char *str;   
  // Id ref for each chunk 
  int count = 0;
    
  // Loop through the data and seperate it into
  // chunks at each "," delimeter
  while ((str = strtok_r(p, ",", &p)) != NULL)
  { 
    // Add chunk to array  
    inParse[count] = str;
    // Increment data count
    count++;      
  }
  
  // If the data has two values then..  
  if(count == 2)
  {
    // Define value 1 as a function identifier
    char *func = inParse[0];
    // Define value 2 as a property value
    char *prop = inParse[1];
    
    // Call the relevant identified function  
    switch(*func)
    {
      case 'A': FunctionA(prop); break;
      case 'B': FunctionB(prop); break;
    }    
  }
}

void serialEvent() 
{
  // Read while we have data
  while (Serial.available() && stringComplete == false) 
  {
    // Read a character
    char inChar = Serial.read(); 
    // Store it in char array
    inData[index] = inChar; 
    // Increment where to write next
    index++;     
    // Also add it to string storage just
    // in case, not used yet :)
    inString += inChar;
    
    // Check for termination character
    if (inChar == '\n') 
    {
      // Reset the index
      index = 0;
      // Set completion of read to true
      stringComplete = true;
    }
  }
}

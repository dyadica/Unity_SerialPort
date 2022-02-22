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

// #include "Arduino.h"

// Buffer for the incoming data
char inData[100];
// Buffer for the parsed data chunks
char *inParse[100];

// Storage for data as string
String inString = "";

// Incoming data id
int indx = 0;
// Read state of incoming data
boolean stringComplete = false;

// if true then show data stream
boolean showDataStream = false;

// dummy prop value to send to Unity
int dummy = 0;

void setup() 
{
  // Delay to facilitate start up of Xbee usually about 
  // 5 seconds. Comment out if using wired serial etc.
  
  delay(5000);
  
  // Initialise the serial port
  
  Serial.begin(115200);
  
  // Ready to go!  
  
  Serial.println("Robot Ready");
}

void loop() 
{

  // For ESP32 as it doesn't register serialEvent
  if (Serial.available())
        serialEvent();
  
  if (stringComplete) 
  {
    // Parse the recieved data
    ParseSerialData();
    // Reset inString to empty
    inString = "";    
    // Reset the system for further 
    // input of data   
    stringComplete = false; 
  }  
  
  // Send dummy data if set to true
  // to toggle send B,1
  
  if(showDataStream)
  {

    // use seperator "," to split data
    Serial.print("D,");
    // Send the dummy value 
    Serial.print(dummy);

    // Newline character for .Readline()
    Serial.println(); 
        
    // Delimiter character for .ReadTo(Delimeter)
    // Serial.print("|"); 
 
    // change the dummy value 
    
    dummy++;
    
    // slow things down a little
    
    delay(50);
  }
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
  // Clear inData, so there are no leftovers for next cycle
  memset(&inData[0], 0, sizeof(inData));
}

void serialEvent() 
{
  // Read while we have data
  while (Serial.available() && stringComplete == false) 
  {
    // Read a character
    char inChar = Serial.read(); 
    // Store it in char array
    inData[indx] = inChar; 
    // Increment where to write next
    indx++;     
    // Also add it to string storage just
    // in case, not used yet :)
    inString += inChar;
    
    // Check for termination character
    if (inChar == '\n') 
    {
      // Reset the index
      indx = 0;
      // Set completion of read to true
      stringComplete = true;
    }
  }
}

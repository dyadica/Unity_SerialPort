// Example function A - this just simply
// bounces the data back to Unity3D
void FunctionA(char *prop)
{
  // Output the data
  Serial.print("A,");
  Serial.print(prop); 
  Serial.println();
  // Data already contains "\n"
}

// Example function B - this function
// sets the main loop to output a stream
// of example data.
void FunctionB(char *prop)
{
  // Toggle the bool
  showDataStream = !showDataStream;
  
  // Reset the dummy data value to zero
  if(showDataStream){ dummy = 0; }
}

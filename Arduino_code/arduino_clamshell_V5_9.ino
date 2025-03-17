#include <SoftwareSerial.h> // If using SoftwareSerial

// Define pins
const int relayPin_west_1 = 2;
const int relayPin_west_2 = 3;
const int relayPin_east_1 = 4;
const int relayPin_east_2 = 5;
const int limitSwitch_west_openPin = 6;
const int limitSwitch_west_closedPin = 7;
const int limitSwitch_east_openPin = 8;
const int limitSwitch_east_closedPin = 9;
const int parkedSensorPin = 10;

// Define serial communication pins (if using SoftwareSerial)
// const int rxPin = 12;
// const int txPin = 13;

// Actuator States
enum ActuatorState { OPEN, CLOSED, OPENING, CLOSING, STOPPED };

// Actuator State Variables
ActuatorState actuator_west_State = STOPPED;
ActuatorState actuator_east_State = STOPPED;

// Flags
bool closingInProgress = false;
bool openingInProgress = false;

// Debounce delay (adjust as needed)
const int debounceDelay = 50;

// Limit switch timeout
const unsigned long limitSwitchTimeout = 90000; // 90 seconds

// SoftwareSerial object (if using SoftwareSerial)
// SoftwareSerial mySerial(rxPin, txPin);

// Functions to control actuators
void openActuator(int relayPin1, int relayPin2) {
  digitalWrite(relayPin1, HIGH); // Energize the first relay
  digitalWrite(relayPin2, LOW);  // De-energize the second relay
}


void closeActuator(int relayPin1, int relayPin2) {
  digitalWrite(relayPin1, LOW);  // De-energize the first relay
  digitalWrite(relayPin2, HIGH); // Energize the second relay
}

void stopActuator(int relayPin1, int relayPin2) {
  digitalWrite(relayPin1, LOW);
  digitalWrite(relayPin2, LOW);
}

// Debounce Function
bool readDebouncedPin(int pin) {
  static unsigned long lastDebounceTime[4] = {0, 0, 0, 0}; // Array for multiple pins
  int pinIndex = pin - 6; // Index for limit switches (assuming they start from pin 6)

  int reading = digitalRead(pin);

  if (reading != digitalRead(pin)) {
    lastDebounceTime[pinIndex] = millis();
  }

  if ((millis() - lastDebounceTime[pinIndex]) > debounceDelay) {
    return reading;
  }
  return digitalRead(pin);
}

// Sensor Reading Functions (with debouncing for parked sensor)
bool isTelescopeParked() {
  bool parked = !readDebouncedPin(parkedSensorPin);
  return parked;
}

bool isWestOpen() {
  bool pressed = readDebouncedPin(limitSwitch_west_openPin);
  return pressed;
}

bool isWestClosed() {
  bool pressed = readDebouncedPin(limitSwitch_west_closedPin);
  return pressed;
}

bool isEastOpen() {
  bool pressed = readDebouncedPin(limitSwitch_east_openPin);
  return pressed;
}

bool isEastClosed() {
  bool pressed = readDebouncedPin(limitSwitch_east_closedPin);
  return pressed;
}

// Send Status to Driver (Modified - More Detailed Messages)
void sendStatusToDriver() {
  String statusMessage = "STATUS:";

  // Shutter Status
  if (openingInProgress) {
    statusMessage += "Opening";
  } else if (closingInProgress) {
    statusMessage += "Closing";
  } else if (isWestOpen() && isEastOpen()) {
    statusMessage += "Open";
  } else if (isWestClosed() && isEastClosed()) {
    statusMessage += "Closed";
  } else {
    statusMessage += "Stopped";
  }

  statusMessage += ":"; // Separator

  // Parked Status
  statusMessage += (isTelescopeParked() ? "Parked" : "NotParked");

  Serial.println(statusMessage);
  //delay(50); //Removed delay, as it can cause issues.
}


void setup() {
  // Pin Modes
  pinMode(relayPin_west_1, OUTPUT);
  pinMode(relayPin_west_2, OUTPUT);
  pinMode(relayPin_east_1, OUTPUT);
  pinMode(relayPin_east_2, OUTPUT);
  pinMode(limitSwitch_west_openPin, INPUT_PULLUP);
  pinMode(limitSwitch_west_closedPin, INPUT_PULLUP);
  pinMode(limitSwitch_east_openPin, INPUT_PULLUP);
  pinMode(limitSwitch_east_closedPin, INPUT_PULLUP);
  pinMode(parkedSensorPin, INPUT_PULLUP);

  // Serial Begin
  Serial.begin(9600);
  Serial.println("Arduino Dome Controller Started");
}

unsigned long lastStatusTime = 0;
const unsigned long statusInterval = 3000; // 3 seconds

void loop() {
  // Command Processing
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();

    Serial.print("Received Command: ");
    Serial.println(command);

    if (command == "OPENSHUTTER") {
      openingInProgress = true;
      closingInProgress = false;
      actuator_west_State = OPENING;
      actuator_east_State = STOPPED; 
      Serial.println("OK");
    } else if (command == "CLOSESHUTTER") {
      if (isTelescopeParked()) {
        closingInProgress = true;
        openingInProgress = false;
        actuator_east_State = CLOSING;
        actuator_west_State = CLOSING; 
        Serial.println("OK");
      } else {
        Serial.println("ERROR:MountNotParked");
      }
    } else if (command == "STOP") {  // *** Added STOP command handling ***
      openingInProgress = false;
      closingInProgress = false;
      stopActuator(relayPin_west_1, relayPin_west_2);
      stopActuator(relayPin_east_1, relayPin_east_2);
      actuator_west_State = STOPPED;
      actuator_east_State = STOPPED;
      Serial.println("OK"); 
    } else {
      Serial.println("ERROR:UnknownCommand");
    }
  }

 // Actuator Control Logic
  if (openingInProgress) {
    if (actuator_west_State == OPENING) {
    openActuator(relayPin_west_1, relayPin_west_2); // Open west actuator
      unsigned long startTime = millis();
      while (!isWestOpen() && (millis() - startTime) < limitSwitchTimeout) {
        Serial.print("West Opening Time Elapsed: ");
        Serial.println(millis() - startTime);
        delay(500);
        //sendStatusToDriver(); // removed, now sending every 3 seconds.
      }

      if (isWestOpen()) {
        stopActuator(relayPin_west_1, relayPin_west_2);
        actuator_west_State = OPEN;
        //sendStatusToDriver(); // removed, now sending every 3 seconds.
        actuator_east_State = OPENING;
      } else {
        Serial.print("ERROR:WestLimitSwitchTimeout. Time: ");
        Serial.println(millis() - startTime);
        stopActuator(relayPin_west_1, relayPin_west_2);
        actuator_west_State = STOPPED;
        openingInProgress = false;
      }
    } else if (actuator_east_State == OPENING) {
    openActuator(relayPin_east_1, relayPin_east_2); // Open east actuator
      unsigned long startTime = millis();
      while (!isEastOpen() && (millis() - startTime) < limitSwitchTimeout) {
        Serial.print("East Opening Time Elapsed: ");
        Serial.println(millis() - startTime);
        delay(100);
        //sendStatusToDriver();// removed, now sending every 3 seconds.
      }
      if (isEastOpen()) {
        stopActuator(relayPin_east_1, relayPin_east_2);
        actuator_east_State = OPEN;
        //sendStatusToDriver();// removed, now sending every 3 seconds.
        openingInProgress = false;
      } else {
        Serial.print("ERROR:EastLimitSwitchTimeout. Time: ");
        Serial.println(millis() - startTime);
        stopActuator(relayPin_east_1, relayPin_east_2);
        actuator_east_State = STOPPED;
        openingInProgress = false;
      }
    }
  } else if (closingInProgress) {
  if (actuator_east_State == CLOSING) {
    closeActuator(relayPin_east_1, relayPin_east_2); // Close east actuator
      unsigned long startTime = millis();
      while (!isEastClosed() && (millis() - startTime) < limitSwitchTimeout) {
        Serial.print("East Closing Time Elapsed: ");
        Serial.println(millis() - startTime);
        delay(100);
        //sendStatusToDriver();
      }

      if (isEastClosed()) {
        stopActuator(relayPin_east_1, relayPin_east_2);
        actuator_east_State = CLOSED;
        //sendStatusToDriver();// removed, now sending every 3 seconds.
        actuator_west_State = CLOSING;
      } else {
        Serial.print("ERROR:EastLimitSwitchTimeout. Time: ");
        Serial.println(millis() - startTime);
        stopActuator(relayPin_east_1, relayPin_east_2);
        actuator_east_State = STOPPED;
        closingInProgress = false;
      }
    } else if (actuator_west_State == CLOSING) {
    closeActuator(relayPin_west_1, relayPin_west_2); // Close west actuator
      unsigned long startTime = millis();
      while (!isWestClosed() && (millis() - startTime) < limitSwitchTimeout) {
        Serial.print("West Closing Time Elapsed: ");
        Serial.println(millis() - startTime);
        delay(100);
        //sendStatusToDriver();// removed, now sending every 3 seconds.
      }
      if (isWestClosed()) {
        stopActuator(relayPin_west_1, relayPin_west_2);
        actuator_west_State = CLOSED;
        //sendStatusToDriver();// removed, now sending every 3 seconds.
        closingInProgress = false;
      } else {
        Serial.print("ERROR:WestLimitSwitchTimeout. Time: ");
        Serial.println(millis() - startTime);
        stopActuator(relayPin_west_1, relayPin_west_2);
        actuator_west_State = STOPPED;
        closingInProgress = false;
      }
    }
  }

  // Send status message at regular intervals
  if (millis() - lastStatusTime >= statusInterval) {
    sendStatusToDriver();
    lastStatusTime = millis();
  }

  delay(100); // Keep this for responsiveness
}
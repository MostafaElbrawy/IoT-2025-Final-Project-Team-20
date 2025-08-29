#include <WiFi.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <ESP32Servo.h>

// Servo
Servo myservo;
int servoPin = 5;

// Pins
int ledPin = 18;       // LED أخضر (للشخص المسموح)
int buzzerPin = 19;    // Buzzer
int redLedPin = 4;     // LED أحمر (للشخص unknown)
int irPin = 15;        // IR Sensor

// LCD
LiquidCrystal_I2C lcd(0x27, 16, 2);

// WiFi
const char* ssid = "outside";      
const char* password = "##outside25";  
WiFiServer server(8080);

void setup() {
  Serial.begin(115200);

  // LCD
  Wire.begin(21, 22);
  lcd.begin();
  lcd.backlight();
  lcd.print("Connecting WiFi");

  // Servo
  myservo.attach(servoPin);
  myservo.write(90); // Neutral

  // Pins
  pinMode(ledPin, OUTPUT);
  pinMode(buzzerPin, OUTPUT);
  pinMode(redLedPin, OUTPUT);

  // ✅ IR Sensor Input (Pullup for stability)
  pinMode(irPin, INPUT_PULLUP);

  // WiFi Connect
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("\nConnected!");
  Serial.print("IP: ");
  Serial.println(WiFi.localIP());

  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("IP:");
  lcd.setCursor(0, 1);
  lcd.print(WiFi.localIP().toString());

  delay(3000);

  server.begin();
  lcd.clear();
  lcd.print("Waiting...");
}

void loop() {
  WiFiClient client = server.available();
  if (client) {
    String data = client.readStringUntil('\n');
    data.trim();
    Serial.println("Received: " + data);

    // ✅ تعديل هنا: بعض الـ IR LOW = Detect , HIGH = No Detect
    bool irDetected = (digitalRead(irPin) == LOW);  

    // الشرط: لازم الشخص معروف + IR detect مع بعض
    if (data != "UNKNOWN" && irDetected) {
      handleAccepted(data);
    } 
    else if (data == "UNKNOWN" && irDetected) {
      handleUnknown();
    } 
    else {
      lcd.clear();
      lcd.print("No IR Detect!");
      delay(2000);
      lcd.clear();
      lcd.print("Waiting...");
    }

    client.println("OK");
    client.stop();
  }
}

void handleAccepted(String name) {
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Welcome:");
  lcd.setCursor(0, 1);
  lcd.print(name);

  digitalWrite(ledPin, HIGH);

  // فتح الباب مباشرة (لأننا تأكدنا من الشرطين قبل كدا)
  openDoor();

  digitalWrite(ledPin, LOW);
  lcd.clear();
  lcd.print("Waiting...");
}

void handleUnknown() {
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("Unknown Person");

  digitalWrite(redLedPin, HIGH); 
  digitalWrite(buzzerPin, HIGH);
  delay(3000);
  digitalWrite(buzzerPin, LOW);
  digitalWrite(redLedPin, LOW);

  delay(7000);
  lcd.clear();
  lcd.print("Waiting...");
}

void openDoor() {
  myservo.write(180);
  delay(177);
  myservo.write(90);
  delay(5000);
  myservo.write(0);
  delay(177);
  myservo.write(90);
}

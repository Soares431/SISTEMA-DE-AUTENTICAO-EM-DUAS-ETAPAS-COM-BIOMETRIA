// Sketch de diagnóstico — descobre o endereço I2C do LCD.
// Upload, abrir Serial Monitor em 9600 baud. Procure linha "I2C device found at 0x..".
// Endereços tipicos do LCD16x2 com backpack PCF8574: 0x27 ou 0x3F.
// Use o endereço encontrado em LiquidCrystal_I2C lcd(0xXX, 16, 2) no Arduino.ino.

#include <Wire.h>

void setup() {
  Wire.begin();
  Serial.begin(9600);
  while (!Serial) delay(10);
  Serial.println("I2C Scanner — procurando dispositivos...");
}

void loop() {
  byte count = 0;
  for (byte addr = 1; addr < 127; addr++) {
    Wire.beginTransmission(addr);
    if (Wire.endTransmission() == 0) {
      Serial.print("  I2C device found at 0x");
      if (addr < 16) Serial.print("0");
      Serial.println(addr, HEX);
      count++;
    }
  }
  if (count == 0) Serial.println("Nenhum dispositivo I2C — verifique fiacao SDA=A4 SCL=A5 e VCC=5V.");
  Serial.println("---");
  delay(5000);
}

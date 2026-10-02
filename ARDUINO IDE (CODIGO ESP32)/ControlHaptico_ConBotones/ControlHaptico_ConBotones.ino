#include <Wire.h>
#include <Adafruit_MPU6050.h>
#include <Adafruit_Sensor.h>
#include "BluetoothSerial.h"

// ---------------------------------------------------------------
// Sketch completo del control háptico, con botones WASD + Agarre
// Envía por Bluetooth: "pitch,roll,w,a,s,d,e"
// Sigue escuchando '1'/'2' entrantes para activar la vibración.
// ---------------------------------------------------------------

Adafruit_MPU6050 mpu;
BluetoothSerial SerialBT;

const int PIN_MOTOR = 4;

// Pines de los botones (INPUT_PULLUP: presionado = LOW)
const int PIN_W = 13;
const int PIN_A = 14;
const int PIN_S = 27;
const int PIN_D = 26;
const int PIN_E = 25;

float accXoffset = 0, accYoffset = 0, accZoffset = 0;
float gyroXoffset = 0, gyroYoffset = 0, gyroZoffset = 0;

float pitchFiltrado = 0;
float rollFiltrado = 0;
unsigned long tiempoAnterior = 0;

const float ALPHA = 0.98;

void calibrarIMU() {
  Serial.println("Calibrando... no muevas el sensor.");
  const int muestras = 200;
  float sumAX = 0, sumAY = 0, sumAZ = 0;
  float sumGX = 0, sumGY = 0, sumGZ = 0;

  for (int i = 0; i < muestras; i++) {
    sensors_event_t a, g, temp;
    mpu.getEvent(&a, &g, &temp);
    sumAX += a.acceleration.x;
    sumAY += a.acceleration.y;
    sumAZ += a.acceleration.z;
    sumGX += g.gyro.x;
    sumGY += g.gyro.y;
    sumGZ += g.gyro.z;
    delay(5);
  }

  accXoffset = sumAX / muestras;
  accYoffset = sumAY / muestras;
  accZoffset = (sumAZ / muestras) - 9.8;
  gyroXoffset = sumGX / muestras;
  gyroYoffset = sumGY / muestras;
  gyroZoffset = sumGZ / muestras;

  Serial.println("Calibración terminada.");
}

void patronObjetoManipulable() {
  analogWrite(PIN_MOTOR, 150);
  delay(150);
  analogWrite(PIN_MOTOR, 0);
}

void patronParedLimite() {
  for (int i = 0; i < 3; i++) {
    analogWrite(PIN_MOTOR, 255);
    delay(100);
    analogWrite(PIN_MOTOR, 0);
    delay(80);
  }
}

void setup() {
  Serial.begin(115200);
  while (!Serial) delay(10);

  pinMode(PIN_MOTOR, OUTPUT);

  // Botones con resistencia interna: presionado = LOW, suelto = HIGH
  pinMode(PIN_W, INPUT_PULLUP);
  pinMode(PIN_A, INPUT_PULLUP);
  pinMode(PIN_S, INPUT_PULLUP);
  pinMode(PIN_D, INPUT_PULLUP);
  pinMode(PIN_E, INPUT_PULLUP);

  SerialBT.begin("ESP32_ControlHaptico");
  Serial.println("Bluetooth iniciado. Empareja desde Windows como 'ESP32_ControlHaptico'.");

  if (!mpu.begin()) {
    Serial.println("No se detectó el MPU6050. Revisa las conexiones (VCC, GND, SCL, SDA).");
    while (1) delay(10);
  }

  mpu.setAccelerometerRange(MPU6050_RANGE_8_G);
  mpu.setGyroRange(MPU6050_RANGE_500_DEG);
  mpu.setFilterBandwidth(MPU6050_BAND_21_HZ);

  calibrarIMU();
  tiempoAnterior = millis();
}

void loop() {
  // --- 1) Comandos entrantes de vibración ---
  if (SerialBT.available() > 0) {
    char comando = SerialBT.read();
    if (comando == '1') {
      patronObjetoManipulable();
    } else if (comando == '2') {
      patronParedLimite();
    }
  }

  // --- 2) Leer botones (invertidos: LOW=presionado -> 1, HIGH=suelto -> 0) ---
  int w = (digitalRead(PIN_W) == LOW) ? 1 : 0;
  int a = (digitalRead(PIN_A) == LOW) ? 1 : 0;
  int s = (digitalRead(PIN_S) == LOW) ? 1 : 0;
  int d = (digitalRead(PIN_D) == LOW) ? 1 : 0;
  int e = (digitalRead(PIN_E) == LOW) ? 1 : 0;

  // --- 3) Leer y filtrar la IMU ---
  sensors_event_t acc, gyr, temp;
  mpu.getEvent(&acc, &gyr, &temp);

  float accX = acc.acceleration.x - accXoffset;
  float accY = acc.acceleration.y - accYoffset;
  float accZ = acc.acceleration.z - accZoffset;
  float gyroX = gyr.gyro.x - gyroXoffset;
  float gyroY = gyr.gyro.y - gyroYoffset;

  float pitchAcc = atan2(-accX, sqrt(accY * accY + accZ * accZ)) * 180.0 / PI;
  float rollAcc  = atan2(accY, accZ) * 180.0 / PI;

  unsigned long tiempoActual = millis();
  float dt = (tiempoActual - tiempoAnterior) / 1000.0;
  tiempoAnterior = tiempoActual;

  float gyroYgrados = gyroY * 180.0 / PI;
  float gyroXgrados = gyroX * 180.0 / PI;

  pitchFiltrado = ALPHA * (pitchFiltrado + gyroYgrados * dt) + (1 - ALPHA) * pitchAcc;
  rollFiltrado  = ALPHA * (rollFiltrado  + gyroXgrados * dt) + (1 - ALPHA) * rollAcc;

  // --- 4) Enviar todo junto por Bluetooth: pitch,roll,w,a,s,d,e ---
  SerialBT.print(pitchFiltrado);
  SerialBT.print(",");
  SerialBT.print(rollFiltrado);
  SerialBT.print(",");
  SerialBT.print(w);
  SerialBT.print(",");
  SerialBT.print(a);
  SerialBT.print(",");
  SerialBT.print(s);
  SerialBT.print(",");
  SerialBT.print(d);
  SerialBT.print(",");
  SerialBT.println(e);

  delay(50);
}

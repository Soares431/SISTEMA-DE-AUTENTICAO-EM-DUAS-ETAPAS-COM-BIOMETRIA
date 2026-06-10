// ═══════════════════════════════════════════════════════════════
// Arduino — Terminal de acesso (cliente Serial do Worker .NET)
// ═══════════════════════════════════════════════════════════════
// Sem dados locais. O Arduino:
//   1. Coleta ID e senha pelo keypad
//   2. Envia EVT| pelo Serial USB
//   3. Reage aos CMD| do Worker (LCD, ASK_PASSWORD, START_VERIFY,
//      START_ENROLL|<slot>, BUZZER, ACCESS|DENIED)
//   4. Quando recebe START_VERIFY/START_ENROLL, controla o AS608
//      e devolve OK/FAIL ou ENROLLED
//
// Toda regra de negócio (existe? ativa? tem permissão? senha bate?)
// fica no Worker (EventProcessorArduino.cs) consultando o banco.
//
// ═══════════════════════════════════════════════════════════════
// Pinagem (ver doc DOC_INTEGRACAO_ARDUINO.md):
//   LCD I2C    : SDA=A4, SCL=A5     (endereço 0x27)
//   Keypad 4x4 : linhas 2-5, cols 6-9
//   AS608      : RX_sensor → pino 10, TX_sensor → pino 11, TCH → pino 12
//                VCC do AS608 vem do AMS1117-3.3 (5V → 3.3V regulado)
//   Buzzer     : pino A0  (opcional — se não tiver, ignora)
//
// Libs (Library Manager):
//   - LiquidCrystal_I2C (Frank de Brabander)
//   - Keypad (Mark Stanley)
//   - Adafruit Fingerprint Sensor Library
// ═══════════════════════════════════════════════════════════════

#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <Keypad.h>
#include <SoftwareSerial.h>
#include <Adafruit_Fingerprint.h>

// ── LCD ──────────────────────────────────────────────────────────
LiquidCrystal_I2C lcd(0x27, 16, 2);

// ── KEYPAD ───────────────────────────────────────────────────────
const byte ROWS = 4;
const byte COLS = 4;

char keys[ROWS][COLS] = {
  {'1','2','3','A'},
  {'4','5','6','B'},
  {'7','8','9','C'},
  {'*','0','#','D'}
};

byte rowPins[ROWS] = {2, 3, 4, 5};
byte colPins[COLS] = {6, 7, 8, 9};

Keypad keypad = Keypad(makeKeymap(keys), rowPins, colPins, ROWS, COLS);

// ── AS608 ────────────────────────────────────────────────────────
const byte FP_RX_PIN = 10;   // recebe do sensor (sensor TX → arduino RX)
const byte FP_TX_PIN = 11;   // envia ao sensor (sensor RX ← arduino TX)
const byte FP_TCH_PIN = 12;  // touch detection (HIGH quando o dedo toca)

SoftwareSerial fingerSerial(FP_RX_PIN, FP_TX_PIN);
Adafruit_Fingerprint finger = Adafruit_Fingerprint(&fingerSerial);

// ── BUZZER ───────────────────────────────────────────────────────
const byte BUZZER_PIN = A0;

// ── RELÉ (fechadura/solenoide) ───────────────────────────────────
// HIGH = acionado (fechadura abre se o relé for normalmente-aberto).
// Worker envia CMD|RELAY|OPEN|<segundos>; o Arduino aciona por X seg.
const byte RELAY_PIN = A1;

// ── TIMEOUTS ─────────────────────────────────────────────────────
const unsigned long TIMEOUT_DEDO_MS = 10000;     // tempo pra colocar o dedo
const unsigned long DURACAO_RESULTADO_MS = 2500;

// ── ESTADO ───────────────────────────────────────────────────────
enum Estado {
  DIGITANDO_ID,
  AGUARDANDO_SERVIDOR_ID,
  DIGITANDO_SENHA,
  AGUARDANDO_SERVIDOR_SENHA,
  VERIFY_AGUARDANDO_DEDO,     // CMD|FINGER|START_VERIFY recebido — espera dedo + faz fingerSearch
  ENROLL_AGUARDANDO_1A_VEZ,   // CMD|FINGER|START_ENROLL|<slot> — 1ª captura
  ENROLL_AGUARDANDO_RETIRAR,  // pede pra tirar o dedo
  ENROLL_AGUARDANDO_2A_VEZ,   // 2ª captura
  MOSTRANDO_RESULTADO         // tela final por DURACAO_RESULTADO_MS
};

Estado estado = DIGITANDO_ID;
String idDigitado = "";
String senhaDigitada = "";
String bufferSerial = "";
unsigned long tempoResultado = 0;
unsigned long tempoInicioEspera = 0;
uint16_t slotEnroll = 0;          // slot do AS608 vindo do CMD|FINGER|START_ENROLL|<slot>
bool dedoEstavaPresente = false;  // pra emitir PLACED/REMOVED por borda
bool releAtivo = false;           // estado atual do relé
unsigned long releDesligaAt = 0;  // millis() em que o relé deve desligar

// ═══════════════════════════════════════════════════════════════
// SETUP
// ═══════════════════════════════════════════════════════════════
void setup() {
  Serial.begin(9600);
  delay(500);
  Wire.begin();
  lcd.init();
  lcd.backlight();

  pinMode(BUZZER_PIN, OUTPUT);
  digitalWrite(BUZZER_PIN, LOW);
  pinMode(RELAY_PIN, OUTPUT);
  digitalWrite(RELAY_PIN, LOW);
  pinMode(FP_TCH_PIN, INPUT);

  // AS608 fala em 57600 (padrão Adafruit Fingerprint)
  finger.begin(57600);
  delay(200);
  if (finger.verifyPassword()) {
    Serial.println("EVT|FINGER|SENSOR|OK");
  } else {
    Serial.println("EVT|FINGER|SENSOR|FAIL");
  }

  telaInicial();
  Serial.println("EVT|READY");
}

// ═══════════════════════════════════════════════════════════════
// LOOP
// ═══════════════════════════════════════════════════════════════
void loop() {
  receberSerial();
  voltarSeResultadoExpirou();
  gerenciarRele();
  detectarTouch();
  processarEstadoBiometrico();
  lerTeclado();
}

// Desliga o relé quando o tempo solicitado pelo Worker expira (não-bloqueante).
void gerenciarRele() {
  if (releAtivo && (long)(millis() - releDesligaAt) >= 0) {
    digitalWrite(RELAY_PIN, LOW);
    releAtivo = false;
  }
}

// ═══════════════════════════════════════════════════════════════
// LCD HELPERS
// ═══════════════════════════════════════════════════════════════
void telaInicial() {
  estado = DIGITANDO_ID;
  idDigitado = "";
  senhaDigitada = "";
  slotEnroll = 0;
  mostrarDuasLinhas("Sistema Pronto", "Digite o ID:");
}

void mostrarDuasLinhas(const String& l1, const String& l2) {
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print(l1.substring(0, 16));
  lcd.setCursor(0, 1);
  lcd.print(l2.substring(0, 16));
}

void mostrarLinha(int linha, const String& texto) {
  lcd.setCursor(0, linha);
  lcd.print("                ");
  lcd.setCursor(0, linha);
  lcd.print(texto.substring(0, 16));
}

void mostrarResultado(const String& l1, const String& l2) {
  mostrarDuasLinhas(l1, l2);
  estado = MOSTRANDO_RESULTADO;
  tempoResultado = millis();
}

void voltarSeResultadoExpirou() {
  if (estado != MOSTRANDO_RESULTADO) return;
  if (millis() - tempoResultado >= DURACAO_RESULTADO_MS) telaInicial();
}

// ═══════════════════════════════════════════════════════════════
// BUZZER
// ═══════════════════════════════════════════════════════════════
void beepOk() {
  digitalWrite(BUZZER_PIN, HIGH);
  delay(120);
  digitalWrite(BUZZER_PIN, LOW);
}

void beepFalha() {
  for (int i = 0; i < 2; i++) {
    digitalWrite(BUZZER_PIN, HIGH);
    delay(80);
    digitalWrite(BUZZER_PIN, LOW);
    delay(80);
  }
}

// ═══════════════════════════════════════════════════════════════
// TOUCH DETECT — emite EVT|FINGER|PLACED / REMOVED por borda
// ═══════════════════════════════════════════════════════════════
void detectarTouch() {
  bool dedoAgora = digitalRead(FP_TCH_PIN) == HIGH;
  if (dedoAgora && !dedoEstavaPresente) {
    Serial.println("EVT|FINGER|PLACED");
  } else if (!dedoAgora && dedoEstavaPresente) {
    Serial.println("EVT|FINGER|REMOVED");
  }
  dedoEstavaPresente = dedoAgora;
}

// ═══════════════════════════════════════════════════════════════
// SERIAL — RECEBE COMANDOS DO WORKER
// ═══════════════════════════════════════════════════════════════
void receberSerial() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '\n') {
      processarLinhaSerial(bufferSerial);
      bufferSerial = "";
    } else if (c != '\r') {
      bufferSerial += c;
    }
  }
}

int quebrarMensagem(const String& linha, String campos[5]) {
  int n = 0;
  int inicio = 0;
  for (int i = 0; i <= (int)linha.length() && n < 5; i++) {
    if (i == (int)linha.length() || linha.charAt(i) == '|') {
      campos[n++] = linha.substring(inicio, i);
      inicio = i + 1;
    }
  }
  return n;
}

void processarLinhaSerial(String linha) {
  linha.trim();
  if (linha.length() == 0) return;

  String c[5];
  int n = quebrarMensagem(linha, c);
  if (n < 2 || c[0] != "CMD") return;

  if (c[1] == "LCD" && c[2] == "LINE1") { mostrarLinha(0, n >= 4 ? c[3] : ""); return; }
  if (c[1] == "LCD" && c[2] == "LINE2") { mostrarLinha(1, n >= 4 ? c[3] : ""); return; }
  if (c[1] == "LCD" && c[2] == "CLEAR") { lcd.clear(); return; }

  if (c[1] == "ASK" && c[2] == "PASSWORD") {
    estado = DIGITANDO_SENHA;
    senhaDigitada = "";
    mostrarDuasLinhas("1o Acesso", "Senha:");
    return;
  }

  if (c[1] == "FINGER" && c[2] == "START_VERIFY") {
    estado = VERIFY_AGUARDANDO_DEDO;
    tempoInicioEspera = millis();
    mostrarDuasLinhas("Coloque o dedo", "no sensor");
    return;
  }

  if (c[1] == "FINGER" && c[2] == "START_ENROLL") {
    // CMD|FINGER|START_ENROLL|<slot>
    slotEnroll = (n >= 4) ? (uint16_t)c[3].toInt() : 1;
    if (slotEnroll == 0 || slotEnroll > 127) slotEnroll = 1;
    estado = ENROLL_AGUARDANDO_1A_VEZ;
    tempoInicioEspera = millis();
    mostrarDuasLinhas("Cadastrar dig.", "Coloque o dedo");
    return;
  }

  if (c[1] == "FINGER" && c[2] == "CANCEL") {
    Serial.println("EVT|FINGER|CANCEL");
    mostrarResultado("Cancelado", "");
    return;
  }

  if (c[1] == "BUZZER" && c[2] == "OK") {
    beepOk();
    mostrarResultado("Acesso Liberado!", "Bem vindo!");
    return;
  }
  if (c[1] == "BUZZER" && c[2] == "FAIL") {
    beepFalha();
    return;
  }

  // CMD|RELAY|OPEN|<segundos> — aciona relé por N segundos (não-bloqueante)
  if (c[1] == "RELAY" && c[2] == "OPEN") {
    int dur = (n >= 4) ? c[3].toInt() : 5;
    if (dur <= 0 || dur > 60) dur = 5;  // sanidade: limita 60s
    digitalWrite(RELAY_PIN, HIGH);
    releAtivo = true;
    releDesligaAt = millis() + (unsigned long)dur * 1000UL;
    return;
  }

  if (c[1] == "ACCESS" && c[2] == "DENIED") {
    beepFalha();
    String motivo = n >= 4 ? c[3] : "negado";
    mostrarResultado("Acesso Negado!", motivo);
    return;
  }
}

// ═══════════════════════════════════════════════════════════════
// MAQUINA DE ESTADOS BIOMETRICA
// ═══════════════════════════════════════════════════════════════
void processarEstadoBiometrico() {
  // ── VERIFY 1:N ──────────────────────────────────────────────
  if (estado == VERIFY_AGUARDANDO_DEDO) {
    if (millis() - tempoInicioEspera > TIMEOUT_DEDO_MS) {
      Serial.println("EVT|FINGER|FAIL|TIMEOUT_IMAGEM");
      mostrarResultado("Tempo esgotado", "");
      return;
    }
    uint8_t p = finger.getImage();
    if (p == FINGERPRINT_NOFINGER) return;  // ainda sem dedo, segue esperando
    Serial.print("DBG|GETIMG|"); Serial.println(p);
    if (p != FINGERPRINT_OK) {
      Serial.println("EVT|FINGER|FAIL|IMG");
      mostrarResultado("Falha leitura", "Tente de novo");
      return;
    }
    uint8_t t = finger.image2Tz(1);
    Serial.print("DBG|IMG2TZ|"); Serial.println(t);
    if (t != FINGERPRINT_OK) {
      Serial.println("EVT|FINGER|FAIL|TZ");
      mostrarResultado("Falha leitura", "Tente de novo");
      return;
    }
    uint8_t s = finger.fingerSearch();
    Serial.print("DBG|SEARCH|"); Serial.print(s);
    Serial.print("|fid="); Serial.println(finger.fingerID);
    if (s == FINGERPRINT_OK) {
      // Devolve o ID que o usuário digitou (CodigoUsuario) — o Worker
      // já validou pessoa+permissão antes de chamar START_VERIFY, então
      // o match aqui só confirma que tem digital cadastrada.
      Serial.print("EVT|FINGER|OK|"); Serial.println(idDigitado);
    } else {
      Serial.println("EVT|FINGER|FAIL|NAO_RECONHECIDO");
      mostrarResultado("Nao reconhecido", "");
    }
    return;
  }

  // ── ENROLL — 1ª captura ────────────────────────────────────
  if (estado == ENROLL_AGUARDANDO_1A_VEZ) {
    if (millis() - tempoInicioEspera > TIMEOUT_DEDO_MS) {
      Serial.println("EVT|FINGER|FAIL|TIMEOUT_IMAGEM");
      mostrarResultado("Tempo esgotado", "");
      return;
    }
    uint8_t p = finger.getImage();
    if (p == FINGERPRINT_NOFINGER) return;
    Serial.print("DBG|GETIMG1|"); Serial.println(p);
    if (p != FINGERPRINT_OK) {
      Serial.println("EVT|FINGER|FAIL|IMG");
      mostrarResultado("Falha leitura", "Tente de novo");
      return;
    }
    uint8_t t = finger.image2Tz(1);
    Serial.print("DBG|IMG2TZ1|"); Serial.println(t);
    if (t != FINGERPRINT_OK) {
      Serial.println("EVT|FINGER|FAIL|TZ");
      mostrarResultado("Falha leitura", "Tente de novo");
      return;
    }
    estado = ENROLL_AGUARDANDO_RETIRAR;
    mostrarDuasLinhas("Tire o dedo", "");
    return;
  }

  if (estado == ENROLL_AGUARDANDO_RETIRAR) {
    if (finger.getImage() == FINGERPRINT_NOFINGER) {
      estado = ENROLL_AGUARDANDO_2A_VEZ;
      tempoInicioEspera = millis();
      mostrarDuasLinhas("Coloque dedo", "novamente");
    }
    return;
  }

  if (estado == ENROLL_AGUARDANDO_2A_VEZ) {
    if (millis() - tempoInicioEspera > TIMEOUT_DEDO_MS) {
      Serial.println("EVT|FINGER|FAIL|TIMEOUT_IMAGEM");
      mostrarResultado("Tempo esgotado", "");
      return;
    }
    uint8_t p = finger.getImage();
    if (p == FINGERPRINT_NOFINGER) return;
    Serial.print("DBG|GETIMG2|"); Serial.println(p);
    if (p != FINGERPRINT_OK) {
      Serial.println("EVT|FINGER|FAIL|IMG");
      mostrarResultado("Falha leitura", "Tente de novo");
      return;
    }
    uint8_t t = finger.image2Tz(2);
    Serial.print("DBG|IMG2TZ2|"); Serial.println(t);
    if (t != FINGERPRINT_OK) {
      Serial.println("EVT|FINGER|FAIL|TZ");
      mostrarResultado("Falha leitura", "Tente de novo");
      return;
    }
    uint8_t m = finger.createModel();
    Serial.print("DBG|CREATE|"); Serial.println(m);
    if (m != FINGERPRINT_OK) {
      Serial.println("EVT|FINGER|FAIL|MISMATCH");
      mostrarResultado("Digitais", "diferentes");
      return;
    }
    uint8_t st = finger.storeModel(slotEnroll);
    Serial.print("DBG|STORE|"); Serial.print(st);
    Serial.print("|slot="); Serial.println(slotEnroll);
    if (st != FINGERPRINT_OK) {
      Serial.println("EVT|FINGER|FAIL|STORE");
      mostrarResultado("Erro ao salvar", "");
      return;
    }
    Serial.print("EVT|FINGER|ENROLLED|"); Serial.println(idDigitado);
    beepOk();
    mostrarResultado("Digital", "cadastrada!");
    return;
  }
}

// ═══════════════════════════════════════════════════════════════
// KEYPAD
// ═══════════════════════════════════════════════════════════════
void lerTeclado() {
  if (estado == MOSTRANDO_RESULTADO) return;
  if (estado == AGUARDANDO_SERVIDOR_ID || estado == AGUARDANDO_SERVIDOR_SENHA) return;
  if (estado == VERIFY_AGUARDANDO_DEDO
      || estado == ENROLL_AGUARDANDO_1A_VEZ
      || estado == ENROLL_AGUARDANDO_RETIRAR
      || estado == ENROLL_AGUARDANDO_2A_VEZ) {
    // Permite cancelar durante leitura biométrica com #
    char t = keypad.getKey();
    if (t == '#') {
      Serial.println("EVT|FINGER|CANCEL");
      mostrarResultado("Cancelado", "");
    }
    return;
  }

  char tecla = keypad.getKey();
  if (tecla == NO_KEY) return;

  if (estado == DIGITANDO_ID) {
    if (tecla == '#') { telaInicial(); return; }
    if (tecla == '*') {
      if (idDigitado.length() != 6) {
        mostrarLinha(0, "ID: 6 digitos!");
        delay(1200);
        mostrarDuasLinhas("Sistema Pronto", "Digite o ID:");
        return;
      }
      Serial.println("EVT|ID|" + idDigitado);
      estado = AGUARDANDO_SERVIDOR_ID;
      mostrarDuasLinhas("ID enviado...", "Aguardando...");
      return;
    }
    if (idDigitado.length() < 6 && tecla >= '0' && tecla <= '9') {
      idDigitado += tecla;
      String mask = "";
      for (unsigned int i = 0; i < idDigitado.length(); i++) mask += "*";
      mostrarLinha(1, "ID: " + mask);
    }
    return;
  }

  if (estado == DIGITANDO_SENHA) {
    if (tecla == '#') { telaInicial(); return; }
    if (tecla == '*') {
      if (senhaDigitada.length() != 6) {
        mostrarLinha(0, "Senha: 6 dig!");
        delay(1200);
        mostrarDuasLinhas("1o Acesso", "Senha:");
        return;
      }
      Serial.println("EVT|SENHA|" + idDigitado + "|" + senhaDigitada);
      estado = AGUARDANDO_SERVIDOR_SENHA;
      mostrarDuasLinhas("Senha enviada...", "Aguardando...");
      return;
    }
    if (senhaDigitada.length() < 6 && tecla >= '0' && tecla <= '9') {
      senhaDigitada += tecla;
      String mask = "";
      for (unsigned int i = 0; i < senhaDigitada.length(); i++) mask += "*";
      mostrarLinha(1, "Senha: " + mask);
    }
    return;
  }
}

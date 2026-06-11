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
//   LCD I2C    : SDA=A4, SCL=A5     (endereço 0x27 ou 0x3F — testar com I2C_Scanner)
//   Keypad 4x4 : linhas 2-5, cols 6-9
//   AS608      : Sensor TX → pino 10 (Arduino RX), Sensor RX → pino 11 (Arduino TX),
//                TCH → pino 12, VCC=3.3V (NÃO 5V), GND=GND
//                ATENÇÃO: TX e RX SEMPRE CRUZAM entre Arduino ↔ Sensor.
//   Buzzer     : pino A0  (opcional — se não tiver, ignora)
//
// Libs (Library Manager):
//   - LiquidCrystal_I2C (Frank de Brabander)
//   - Keypad (Mark Stanley)
//   - Adafruit Fingerprint Sensor Library
//
// MEMÓRIA: Uno tem só 2KB de RAM. Tudo que é literal de string vai
// dentro de F("...") pra ficar na flash (32KB) e não estourar a SRAM.
// Sem isso o boot entra em loop e o LCD fica piscando preto.
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

// Pinos 3 e 4 trocados em software: a fiação física tinha L2↔L3 invertida.
// L1 e L4 testaram OK (1,2,3 e *,0,# nas posições certas) então não precisei mexer.
byte rowPins[ROWS] = {2, 4, 3, 5};
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
const unsigned long TIMEOUT_DEDO_MS = 20000;     // tempo pra colocar o dedo (20s — antes 10s era curto)
const unsigned long DURACAO_RESULTADO_MS = 2500;
const unsigned long TIMEOUT_INATIVIDADE_MS = 60000;  // 60s sem tecla no DIGITANDO_* → volta
const unsigned long TIMEOUT_SERVIDOR_MS   = 15000;   // 15s sem resposta do Worker → "offline"
const unsigned long INTERVALO_HEARTBEAT_MS = 30000;  // a cada 30s verifica sensor AS608

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
unsigned long ultimaAtividade = 0;     // última tecla ou transição de estado
unsigned long pedidoEnviadoEm = 0;     // millis() do EVT|ID ou EVT|SENHA enviado
unsigned long ultimoHeartbeat = 0;     // último heartbeat do AS608

// Modo simulador: setado em setup() conforme finger.verifyPassword().
// Se false (AS608 não responde ou está mal fiado), os estados biométricos
// aceitam keypad como entrada simulada — A=sucesso, B=falha.
// Permite testar o fluxo completo do sistema mesmo sem sensor funcionando.
bool as608Online = false;

// Retentativas automáticas do fingerSearch antes de cancelar o VERIFY.
// Captura do AS608 caseiro é inconsistente: às vezes Conf=4, próxima vez OK.
// Sem isso, o usuário teria que redigitar ID a cada falha de match.
const uint8_t MAX_TENTATIVAS_VERIFY = 3;
uint8_t tentativasVerify = 0;

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
    as608Online = true;
    // Security level 1-5: maior = mais estrito (default 3). Captura imperfeita do AS608
    // em ambiente caseiro funciona melhor em 2 — match com tolerancia, mas seguro o bastante.
    finger.setSecurityLevel(2);
    Serial.println(F("EVT|FINGER|SENSOR|OK"));
  } else {
    as608Online = false;
    Serial.println(F("EVT|FINGER|SENSOR|FAIL"));
    // Mostra aviso no LCD por 2s pra deixar claro que entrou em modo simulado
    mostrarDuasLinhasF(F("AS608 offline"), F("Modo simulado"));
    delay(2000);
  }

  telaInicial();
  Serial.println(F("EVT|READY"));
}

// ═══════════════════════════════════════════════════════════════
// LOOP
// ═══════════════════════════════════════════════════════════════
void loop() {
  receberSerial();
  voltarSeResultadoExpirou();
  gerenciarRele();
  gerenciarTimeouts();
  heartbeatSensor();
  detectarTouch();
  processarEstadoBiometrico();
  lerTeclado();
}

// Timeout de inatividade — se o usuário abandonar o terminal no meio do fluxo
// (digitando ID/senha) ou se o Worker não responder, volta pra tela inicial
// pra liberar o terminal pro próximo usuário.
void gerenciarTimeouts() {
  // DIGITANDO_ID: terminal fica esperando o próximo usuário indefinidamente,
  // não há timeout — a tela "Digite o ID:" é o estado idle do terminal.
  if (estado == DIGITANDO_SENHA) {
    if (millis() - ultimaAtividade > TIMEOUT_INATIVIDADE_MS) {
      mostrarResultadoF(F("Tempo esgotado"), F("(sem senha 60s)"));
    }
    return;
  }
  if (estado == AGUARDANDO_SERVIDOR_ID || estado == AGUARDANDO_SERVIDOR_SENHA) {
    if (millis() - pedidoEnviadoEm > TIMEOUT_SERVIDOR_MS) {
      Serial.println(F("EVT|SERVIDOR|TIMEOUT"));
      mostrarResultadoF(F("Servidor offline"), F("Tente de novo"));
    }
  }
}

// Heartbeat — periodicamente verifica se o AS608 ainda responde.
// Worker monitora EVT|FINGER|SENSOR|OK/FAIL pra detectar desconexão do sensor.
void heartbeatSensor() {
  if (millis() - ultimoHeartbeat < INTERVALO_HEARTBEAT_MS) return;
  ultimoHeartbeat = millis();
  // só roda heartbeat se não estiver no meio de uma operação biométrica
  if (estado == VERIFY_AGUARDANDO_DEDO
      || estado == ENROLL_AGUARDANDO_1A_VEZ
      || estado == ENROLL_AGUARDANDO_RETIRAR
      || estado == ENROLL_AGUARDANDO_2A_VEZ) return;
  if (finger.verifyPassword()) {
    as608Online = true;
    Serial.println(F("EVT|FINGER|SENSOR|OK"));
  } else {
    as608Online = false;
    Serial.println(F("EVT|FINGER|SENSOR|FAIL"));
  }
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
  ultimaAtividade = millis();
  mostrarDuasLinhasF(F("Digite o ID:"), F(""));
}

// Versão que aceita String (usada quando há variáveis dinâmicas)
void mostrarDuasLinhas(const String& l1, const String& l2) {
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print(l1.substring(0, 16));
  lcd.setCursor(0, 1);
  lcd.print(l2.substring(0, 16));
}

// Versão F() — string em flash, não consome RAM
void mostrarDuasLinhasF(const __FlashStringHelper* l1, const __FlashStringHelper* l2) {
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print(l1);
  lcd.setCursor(0, 1);
  lcd.print(l2);
}

void mostrarLinha(int linha, const String& texto) {
  lcd.setCursor(0, linha);
  lcd.print(F("                "));
  lcd.setCursor(0, linha);
  lcd.print(texto.substring(0, 16));
}

void mostrarLinhaF(int linha, const __FlashStringHelper* texto) {
  lcd.setCursor(0, linha);
  lcd.print(F("                "));
  lcd.setCursor(0, linha);
  lcd.print(texto);
}

void mostrarResultado(const String& l1, const String& l2) {
  mostrarDuasLinhas(l1, l2);
  estado = MOSTRANDO_RESULTADO;
  tempoResultado = millis();
}

void mostrarResultadoF(const __FlashStringHelper* l1, const __FlashStringHelper* l2) {
  mostrarDuasLinhasF(l1, l2);
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
    Serial.println(F("EVT|FINGER|PLACED"));
  } else if (!dedoAgora && dedoEstavaPresente) {
    Serial.println(F("EVT|FINGER|REMOVED"));
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

  if (c[1] == "LCD" && c[2] == "LINE1") { mostrarLinha(0, n >= 4 ? c[3] : String("")); return; }
  if (c[1] == "LCD" && c[2] == "LINE2") { mostrarLinha(1, n >= 4 ? c[3] : String("")); return; }
  if (c[1] == "LCD" && c[2] == "CLEAR") { lcd.clear(); return; }

  if (c[1] == "ASK" && c[2] == "PASSWORD") {
    estado = DIGITANDO_SENHA;
    senhaDigitada = "";
    // CMD|ASK|PASSWORD|1 = primeiro acesso (digital_e_senha sem biometria, vai enrollar).
    // CMD|ASK|PASSWORD|0 = pessoa somente_senha (acesso por senha de cada vez).
    bool primeiroAcesso = (n >= 4 && c[3] == "1");
    if (primeiroAcesso) {
      mostrarDuasLinhasF(F("1o Acesso"), F("Senha:"));
    } else {
      mostrarDuasLinhasF(F("Acesso por senha"), F("Senha:"));
    }
    return;
  }

  if (c[1] == "FINGER" && c[2] == "START_VERIFY") {
    estado = VERIFY_AGUARDANDO_DEDO;
    tempoInicioEspera = millis();
    tentativasVerify = 0;  // novo ciclo de tentativas
    if (as608Online) {
      mostrarDuasLinhasF(F("Coloque o dedo"), F("no sensor"));
    } else {
      mostrarDuasLinhasF(F("Simul: A=OK"), F("B=FAIL  #=cancel"));
    }
    return;
  }

  if (c[1] == "FINGER" && c[2] == "START_ENROLL") {
    // CMD|FINGER|START_ENROLL|<slot>
    slotEnroll = (n >= 4) ? (uint16_t)c[3].toInt() : 1;
    if (slotEnroll == 0 || slotEnroll > 127) slotEnroll = 1;
    estado = ENROLL_AGUARDANDO_1A_VEZ;
    tempoInicioEspera = millis();
    if (as608Online) {
      mostrarDuasLinhasF(F("Cadastrar dig."), F("Coloque o dedo"));
    } else {
      mostrarDuasLinhasF(F("Simul: A=salvar"), F("B=FAIL  #=cancel"));
    }
    return;
  }

  if (c[1] == "FINGER" && c[2] == "CANCEL") {
    Serial.println(F("EVT|FINGER|CANCEL"));
    mostrarResultadoF(F("Cancelado"), F(""));
    return;
  }

  if (c[1] == "BUZZER" && c[2] == "OK") {
    beepOk();
    mostrarResultadoF(F("Acesso Liberado!"), F("Bem vindo!"));
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

  // CMD|FINGER|DELETE|<slot> — apaga template do AS608 e devolve EVT|FINGER|DELETED|<slot>
  // Worker chama isso após admin resetar/inativar pessoa pra liberar o slot no sensor.
  if (c[1] == "FINGER" && c[2] == "DELETE") {
    if (n < 4) return;
    int slot = c[3].toInt();
    if (slot <= 0 || slot > 127) return;
    uint8_t r = finger.deleteModel((uint16_t)slot);
    if (r == FINGERPRINT_OK) {
      Serial.print(F("EVT|FINGER|DELETED|")); Serial.println(slot);
    } else {
      Serial.print(F("EVT|FINGER|FAIL|DELETE|")); Serial.println(slot);
    }
    return;
  }

  if (c[1] == "ACCESS" && c[2] == "DENIED") {
    beepFalha();
    String motivo = n >= 4 ? c[3] : String(F("negado"));
    mostrarResultado(String(F("Acesso Negado!")), motivo);
    return;
  }
}

// ═══════════════════════════════════════════════════════════════
// MAQUINA DE ESTADOS BIOMETRICA
// ═══════════════════════════════════════════════════════════════
void processarEstadoBiometrico() {
  // Sem sensor: estados biométricos esperam keypad (tratado em lerTeclado).
  // Não chamamos finger.* aqui pra evitar timeouts repetidos no AS608 desconectado.
  if (!as608Online) return;

  // ── VERIFY 1:N ──────────────────────────────────────────────
  if (estado == VERIFY_AGUARDANDO_DEDO) {
    if (millis() - tempoInicioEspera > TIMEOUT_DEDO_MS) {
      Serial.println(F("EVT|FINGER|FAIL|TIMEOUT_VERIFY"));
      mostrarResultadoF(F("Tempo esgotado"), F("(sem dedo 20s)"));
      return;
    }
    uint8_t p = finger.getImage();
    if (p == FINGERPRINT_NOFINGER) return;  // ainda sem dedo, segue esperando
    Serial.print(F("DBG|GETIMG|")); Serial.println(p);
    if (p != FINGERPRINT_OK) {
      Serial.println(F("EVT|FINGER|FAIL|IMG"));
      mostrarResultadoF(F("Falha leitura"), F("Tente de novo"));
      return;
    }
    uint8_t t = finger.image2Tz(1);
    Serial.print(F("DBG|IMG2TZ|")); Serial.println(t);
    if (t != FINGERPRINT_OK) {
      Serial.println(F("EVT|FINGER|FAIL|TZ"));
      mostrarResultadoF(F("Falha leitura"), F("Tente de novo"));
      return;
    }
    uint8_t s = finger.fingerSearch();
    Serial.print(F("DBG|SEARCH|")); Serial.print(s);
    Serial.print(F("|fid=")); Serial.println(finger.fingerID);
    if (s == FINGERPRINT_OK) {
      tentativasVerify = 0;
      Serial.print(F("EVT|FINGER|OK|")); Serial.println(idDigitado);
    } else {
      // confidence pode vir com lixo quando s != OK (16384 etc).
      // Clampea pra ficar legível no LCD.
      uint16_t conf = finger.confidence > 255 ? 0 : finger.confidence;
      tentativasVerify++;
      Serial.print(F("EVT|FINGER|RETRY|conf="));
      Serial.print(conf);
      Serial.print(F("|tentativa="));
      Serial.println(tentativasVerify);
      if (tentativasVerify < MAX_TENTATIVAS_VERIFY) {
        // Volta pra VERIFY e deixa tentar de novo (sem novo digitar ID).
        lcd.clear();
        lcd.setCursor(0, 0); lcd.print(F("Tente de novo "));
        lcd.print(tentativasVerify); lcd.print('/'); lcd.print(MAX_TENTATIVAS_VERIFY);
        lcd.setCursor(0, 1); lcd.print(F("Conf: ")); lcd.print(conf);
        delay(1500);
        mostrarDuasLinhasF(F("Coloque o dedo"), F("no sensor"));
        tempoInicioEspera = millis();
        // estado continua VERIFY_AGUARDANDO_DEDO
      } else {
        // 3 falhas: desiste, manda FAIL pro Worker (sera registrada como tentativa negada).
        Serial.print(F("EVT|FINGER|FAIL|NAO_RECONHECIDO|conf="));
        Serial.println(conf);
        lcd.clear();
        lcd.setCursor(0, 0); lcd.print(F("Nao reconhecido"));
        lcd.setCursor(0, 1); lcd.print(F("Conf: ")); lcd.print(conf);
        estado = MOSTRANDO_RESULTADO;
        tempoResultado = millis();
        tentativasVerify = 0;
      }
    }
    return;
  }

  // ── ENROLL — 1ª captura ────────────────────────────────────
  if (estado == ENROLL_AGUARDANDO_1A_VEZ) {
    if (millis() - tempoInicioEspera > TIMEOUT_DEDO_MS) {
      Serial.println(F("EVT|FINGER|FAIL|TIMEOUT_ENROLL1"));
      mostrarResultadoF(F("Tempo esgotado"), F("(cadastro 1a)"));
      return;
    }
    uint8_t p = finger.getImage();
    if (p == FINGERPRINT_NOFINGER) return;
    Serial.print(F("DBG|GETIMG1|")); Serial.println(p);
    if (p != FINGERPRINT_OK) {
      Serial.println(F("EVT|FINGER|FAIL|IMG"));
      mostrarResultadoF(F("Falha leitura"), F("Tente de novo"));
      return;
    }
    uint8_t t = finger.image2Tz(1);
    Serial.print(F("DBG|IMG2TZ1|")); Serial.println(t);
    if (t != FINGERPRINT_OK) {
      Serial.println(F("EVT|FINGER|FAIL|TZ"));
      mostrarResultadoF(F("Falha leitura"), F("Tente de novo"));
      return;
    }
    estado = ENROLL_AGUARDANDO_RETIRAR;
    mostrarDuasLinhasF(F("Tire o dedo"), F(""));
    return;
  }

  if (estado == ENROLL_AGUARDANDO_RETIRAR) {
    if (finger.getImage() == FINGERPRINT_NOFINGER) {
      estado = ENROLL_AGUARDANDO_2A_VEZ;
      tempoInicioEspera = millis();
      mostrarDuasLinhasF(F("Coloque dedo"), F("novamente"));
    }
    return;
  }

  if (estado == ENROLL_AGUARDANDO_2A_VEZ) {
    if (millis() - tempoInicioEspera > TIMEOUT_DEDO_MS) {
      Serial.println(F("EVT|FINGER|FAIL|TIMEOUT_ENROLL2"));
      mostrarResultadoF(F("Tempo esgotado"), F("(cadastro 2a)"));
      return;
    }
    uint8_t p = finger.getImage();
    if (p == FINGERPRINT_NOFINGER) return;
    Serial.print(F("DBG|GETIMG2|")); Serial.println(p);
    if (p != FINGERPRINT_OK) {
      Serial.println(F("EVT|FINGER|FAIL|IMG"));
      mostrarResultadoF(F("Falha leitura"), F("Tente de novo"));
      return;
    }
    uint8_t t = finger.image2Tz(2);
    Serial.print(F("DBG|IMG2TZ2|")); Serial.println(t);
    if (t != FINGERPRINT_OK) {
      Serial.println(F("EVT|FINGER|FAIL|TZ"));
      mostrarResultadoF(F("Falha leitura"), F("Tente de novo"));
      return;
    }
    uint8_t m = finger.createModel();
    Serial.print(F("DBG|CREATE|")); Serial.println(m);
    if (m != FINGERPRINT_OK) {
      Serial.println(F("EVT|FINGER|FAIL|MISMATCH"));
      mostrarResultadoF(F("Digitais"), F("diferentes"));
      return;
    }
    uint8_t st = finger.storeModel(slotEnroll);
    Serial.print(F("DBG|STORE|")); Serial.print(st);
    Serial.print(F("|slot=")); Serial.println(slotEnroll);
    if (st != FINGERPRINT_OK) {
      Serial.println(F("EVT|FINGER|FAIL|STORE"));
      mostrarResultadoF(F("Erro ao salvar"), F(""));
      return;
    }
    Serial.print(F("EVT|FINGER|ENROLLED|")); Serial.println(idDigitado);
    beepOk();
    mostrarResultadoF(F("Digital"), F("cadastrada!"));
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
    char t = keypad.getKey();
    if (t == '#') {
      Serial.println(F("EVT|FINGER|CANCEL"));
      mostrarResultadoF(F("Cancelado"), F(""));
      return;
    }
    // Modo simulado: keypad substitui o AS608. Permite testar o sistema sem sensor.
    if (!as608Online) {
      if (t == 'A') {
        if (estado == VERIFY_AGUARDANDO_DEDO) {
          Serial.print(F("EVT|FINGER|OK|")); Serial.println(idDigitado);
        } else {
          // Qualquer estado de ENROLL → simula cadastro concluído
          Serial.print(F("EVT|FINGER|ENROLLED|")); Serial.println(idDigitado);
          beepOk();
          mostrarResultadoF(F("Digital sim."), F("cadastrada!"));
        }
      } else if (t == 'B') {
        Serial.println(F("EVT|FINGER|FAIL|SIMULADO"));
        beepFalha();
        mostrarResultadoF(F("Falha sim."), F(""));
      }
    }
    return;
  }

  char tecla = keypad.getKey();
  if (tecla == NO_KEY) return;

  if (estado == DIGITANDO_ID) {
    if (tecla == '#') { telaInicial(); return; }
    if (tecla == '*') {
      if (idDigitado.length() != 6) {
        mostrarLinhaF(0, F("ID: 6 digitos!"));
        delay(1200);
        mostrarDuasLinhasF(F("Digite o ID:"), F(""));
        return;
      }
      Serial.print(F("EVT|ID|")); Serial.println(idDigitado);
      estado = AGUARDANDO_SERVIDOR_ID;
      pedidoEnviadoEm = millis();
      mostrarDuasLinhasF(F("ID enviado..."), F("Aguardando..."));
      return;
    }
    if (idDigitado.length() < 6 && tecla >= '0' && tecla <= '9') {
      idDigitado += tecla;
      ultimaAtividade = millis();
      // ID aparece em claro pra ajudar a diagnosticar fiação do keypad (linha/coluna invertida).
      lcd.setCursor(0, 1);
      lcd.print(F("                "));
      lcd.setCursor(0, 1);
      lcd.print(F("ID: "));
      lcd.print(idDigitado);
    }
    return;
  }

  if (estado == DIGITANDO_SENHA) {
    if (tecla == '#') { telaInicial(); return; }
    if (tecla == '*') {
      if (senhaDigitada.length() != 6) {
        mostrarLinhaF(0, F("Senha: 6 dig!"));
        delay(1200);
        mostrarDuasLinhasF(F("1o Acesso"), F("Senha:"));
        return;
      }
      Serial.print(F("EVT|SENHA|")); Serial.print(idDigitado);
      Serial.print('|'); Serial.println(senhaDigitada);
      estado = AGUARDANDO_SERVIDOR_SENHA;
      pedidoEnviadoEm = millis();
      mostrarDuasLinhasF(F("Senha enviada..."), F("Aguardando..."));
      return;
    }
    if (senhaDigitada.length() < 6 && tecla >= '0' && tecla <= '9') {
      senhaDigitada += tecla;
      ultimaAtividade = millis();
      // senha mascarada — imprime asteriscos direto sem alocar String temporária
      lcd.setCursor(0, 1);
      lcd.print(F("                "));
      lcd.setCursor(0, 1);
      lcd.print(F("Senha: "));
      for (unsigned int i = 0; i < senhaDigitada.length(); i++) lcd.print('*');
    }
    return;
  }
}

#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <Keypad.h>
#include <SoftwareSerial.h>

// ═══════════════════════════════════════
// LCD
// ═══════════════════════════════════════
LiquidCrystal_I2C lcd(0x27, 16, 2);

// ═══════════════════════════════════════
// KEYPAD
// ═══════════════════════════════════════
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

// ═══════════════════════════════════════
// AS608 — protocolo direto sem biblioteca
// ═══════════════════════════════════════
SoftwareSerial sensor(10, 11); // RX=10, TX=11
const int PIN_TCH = 12;

// Pacote base AS608
void as608Send(uint8_t* data, uint8_t len) {
  uint8_t pkg[len + 11];
  pkg[0] = 0xEF; pkg[1] = 0x01;           // cabeçalho
  pkg[2] = 0xFF; pkg[3] = 0xFF;           // endereço (broadcast)
  pkg[4] = 0xFF; pkg[5] = 0xFF;
  pkg[6] = 0x01;                           // tipo: comando
  pkg[7] = (len + 2) >> 8;
  pkg[8] = (len + 2) & 0xFF;              // tamanho
  uint16_t sum = 0x01 + pkg[7] + pkg[8];
  for (int i = 0; i < len; i++) {
    pkg[9 + i] = data[i];
    sum += data[i];
  }
  pkg[9 + len]     = sum >> 8;
  pkg[9 + len + 1] = sum & 0xFF;
  sensor.write(pkg, len + 11);
}

// Aguarda confirmação do AS608 — retorna código de confirmação
uint8_t as608Ack(int timeout_ms = 3000) {
  unsigned long t = millis();
  while (millis() - t < (unsigned long)timeout_ms) {
    if (sensor.available() >= 12) {
      uint8_t buf[12];
      sensor.readBytes(buf, 12);
      if (buf[0] == 0xEF && buf[1] == 0x01)
        return buf[9]; // código de confirmação
    }
  }
  return 0xFF; // timeout
}

uint8_t as608GetImage() {
  uint8_t cmd[] = {0x01};
  as608Send(cmd, 1);
  return as608Ack();
}

uint8_t as608Image2Tz(uint8_t slot) {
  uint8_t cmd[] = {0x02, slot};
  as608Send(cmd, 2);
  return as608Ack();
}

uint8_t as608Search(uint16_t& fid) {
  uint8_t cmd[] = {0x04, 0x01, 0x00, 0x00, 0x00, 0xA3};
  as608Send(cmd, 6);
  unsigned long t = millis();
  while (millis() - t < 3000) {
    if (sensor.available() >= 16) {
      uint8_t buf[16];
      sensor.readBytes(buf, 16);
      if (buf[0] == 0xEF && buf[1] == 0x01) {
        fid = (buf[10] << 8) | buf[11];
        return buf[9];
      }
    }
  }
  return 0xFF;
}

uint8_t as608CreateModel() {
  uint8_t cmd[] = {0x05};
  as608Send(cmd, 1);
  return as608Ack();
}

uint8_t as608StoreModel(uint16_t id) {
  uint8_t cmd[] = {0x06, 0x01, (uint8_t)(id >> 8), (uint8_t)(id & 0xFF)};
  as608Send(cmd, 4);
  return as608Ack();
}

uint8_t as608VerifyPassword() {
  uint8_t cmd[] = {0x13, 0x00, 0x00, 0x00, 0x00};
  as608Send(cmd, 5);
  return as608Ack();
}

// ═══════════════════════════════════════
// ESTADOS
// ═══════════════════════════════════════
enum Estado {
  AGUARDANDO_ID,
  AGUARDANDO_SENHA,
  AGUARDANDO_RESPOSTA_CS,
  AGUARDANDO_DIGITAL,
  AGUARDANDO_ENROLL_1,
  AGUARDANDO_ENROLL_2
};

Estado estadoAtual   = AGUARDANDO_ID;
String idDigitado    = "";
String senhaDigitada = "";
int    enrollId      = 0;

// ═══════════════════════════════════════
// SETUP
// ═══════════════════════════════════════
void setup() {
  Serial.begin(9600);
  sensor.begin(57600);
  delay(200);
  Wire.begin();
  lcd.init();
  lcd.init();
  lcd.backlight();
  pinMode(PIN_TCH, INPUT);

  if (as608VerifyPassword() == 0x00) {
    Serial.println("EVT|FINGER|SENSOR|OK");
  } else {
    Serial.println("EVT|FINGER|SENSOR|FAIL");
    exibirMensagem("Sensor erro", "Verifique fiacao");
    delay(2000);
  }

  exibirMensagem("Sistema Pronto", "Digite o ID:");
  Serial.println("EVT|READY");
}

// ═══════════════════════════════════════
// LCD
// ═══════════════════════════════════════
void exibirMensagem(String l1, String l2) {
  lcd.clear();
  lcd.setCursor(0, 0); lcd.print(l1);
  lcd.setCursor(0, 1); lcd.print(l2);
}

void exibirLinha(int l, String txt) {
  lcd.setCursor(0, l);
  lcd.print("                ");
  lcd.setCursor(0, l);
  lcd.print(txt);
}

// ═══════════════════════════════════════
// LOOP
// ═══════════════════════════════════════
void loop() {
  lerTeclado();
  lerComandoSerial();
  lerToque();
}

// ═══════════════════════════════════════
// TOQUE
// ═══════════════════════════════════════
void lerToque() {
  static bool posto = false;
  bool t = digitalRead(PIN_TCH) == HIGH;
  if (t && !posto)  { posto = true;  Serial.println("EVT|FINGER|PLACED");  }
  if (!t && posto)  { posto = false; Serial.println("EVT|FINGER|REMOVED"); }
}

// ═══════════════════════════════════════
// TECLADO
// ═══════════════════════════════════════
void lerTeclado() {
  char tecla = keypad.getKey();
  if (tecla == NO_KEY) return;

  if (estadoAtual == AGUARDANDO_ID) {
    if (tecla == '#') { idDigitado = ""; exibirMensagem("Cancelado", "Digite o ID:"); return; }
    if (tecla == '*') {
      if (idDigitado.length() != 6) { exibirLinha(0, "ID: 6 digitos!"); delay(1500); exibirMensagem("Digite o ID:", ""); return; }
      estadoAtual = AGUARDANDO_RESPOSTA_CS;
      exibirMensagem("Verificando...", "Aguarde");
      Serial.println("EVT|ID|" + idDigitado);
      return;
    }
    if (idDigitado.length() < 6) {
      idDigitado += tecla;
      String ast = ""; for (int i = 0; i < idDigitado.length(); i++) ast += "*";
      exibirLinha(1, "ID: " + ast);
    }
    return;
  }

  if (estadoAtual == AGUARDANDO_SENHA) {
    if (tecla == '#') { idDigitado = ""; senhaDigitada = ""; estadoAtual = AGUARDANDO_ID; exibirMensagem("Cancelado", "Digite o ID:"); return; }
    if (tecla == '*') {
      if (senhaDigitada.length() != 6) { exibirLinha(0, "Senha: 6 dig!"); delay(1500); exibirMensagem("1o Acesso", "Senha:"); return; }
      estadoAtual = AGUARDANDO_RESPOSTA_CS;
      exibirMensagem("Verificando...", "Aguarde");
      Serial.println("EVT|SENHA|" + idDigitado + "|" + senhaDigitada);
      return;
    }
    if (senhaDigitada.length() < 6) {
      senhaDigitada += tecla;
      String ast = ""; for (int i = 0; i < senhaDigitada.length(); i++) ast += "*";
      exibirLinha(1, "Senha: " + ast);
    }
    return;
  }

  if (tecla == '#') { Serial.println("EVT|FINGER|CANCEL"); resetar(); }
}

// ═══════════════════════════════════════
// COMANDOS DO C#
// ═══════════════════════════════════════
void lerComandoSerial() {
  if (!Serial.available()) return;
  String cmd = Serial.readStringUntil('\n');
  cmd.trim();

  if (cmd.startsWith("CMD|LCD|LINE1|"))       { exibirLinha(0, cmd.substring(14)); }
  else if (cmd.startsWith("CMD|LCD|LINE2|"))  { exibirLinha(1, cmd.substring(14)); }
  else if (cmd.startsWith("CMD|LCD|CLEAR"))   { lcd.clear(); }
  else if (cmd.startsWith("CMD|ASK|PASSWORD")) { estadoAtual = AGUARDANDO_SENHA; senhaDigitada = ""; exibirMensagem("1o Acesso", "Senha:"); }
  else if (cmd.startsWith("CMD|FINGER|START_VERIFY"))  { estadoAtual = AGUARDANDO_DIGITAL;  exibirMensagem("ID: "+idDigitado, "Coloque o dedo"); verificarDigital(); }
  else if (cmd.startsWith("CMD|FINGER|START_ENROLL|")) { enrollId = cmd.substring(24).toInt(); estadoAtual = AGUARDANDO_ENROLL_1; exibirMensagem("Cadastrar", "Coloque o dedo"); cadastrarDigital(); }
  else if (cmd.startsWith("CMD|BUZZER|OK"))   { exibirLinha(0, "Acesso Liberado!"); delay(1500); resetar(); }
  else if (cmd.startsWith("CMD|BUZZER|FAIL")) { exibirLinha(0, "Acesso Negado!"); delay(1500); resetar(); }
  else if (cmd.startsWith("CMD|ACCESS|DENIED|")) { exibirMensagem("Acesso Negado!", cmd.substring(18)); delay(2000); resetar(); }
  else if (cmd.startsWith("CMD|FINGER|CANCEL")) { resetar(); }
}

// ═══════════════════════════════════════
// VERIFICAR DIGITAL
// ═══════════════════════════════════════
void verificarDigital() {
  // Espera o dedo (timeout 10s)
  unsigned long t = millis();
  uint8_t r = 0xFF;
  while (millis() - t < 10000) {
    r = as608GetImage();
    Serial.println("DBG|GETIMAGE|" + String(r, HEX));
    if (r == 0x00) break;
    delay(300);
  }
  if (r != 0x00) {
    Serial.println("EVT|FINGER|FAIL|TIMEOUT_IMAGEM");
    exibirLinha(0, "Tempo esgotado");
    delay(1500); resetar(); return;
  }

  uint8_t tz = as608Image2Tz(1);
  Serial.println("DBG|IMAGE2TZ|" + String(tz, HEX));
  if (tz != 0x00) { Serial.println("EVT|FINGER|FAIL|TZ"); resetar(); return; }

  uint16_t fid = 0;
  uint8_t s = as608Search(fid);
  Serial.println("DBG|SEARCH|" + String(s, HEX) + "|fid=" + String(fid));
  if (s == 0x00) {
    Serial.println("EVT|FINGER|OK|" + idDigitado);
  } else {
    Serial.println("EVT|FINGER|FAIL|NAO_RECONHECIDO");
    exibirLinha(0, "Nao reconhecido");
    delay(1500);
    resetar();
  }
}

// ═══════════════════════════════════════
// CADASTRAR DIGITAL
// ═══════════════════════════════════════
void cadastrarDigital() {
  // ── Leitura 1 ──
  exibirMensagem("Coloque o dedo", "1a leitura");
  unsigned long t = millis();
  uint8_t r = 0xFF;
  while (millis() - t < 10000) {
    r = as608GetImage();
    Serial.println("DBG|GETIMAGE1|" + String(r, HEX));
    if (r == 0x00) break;
    delay(300);
  }
  if (r != 0x00) { Serial.println("EVT|FINGER|FAIL|TIMEOUT1"); exibirLinha(0,"Tempo esgotado"); delay(1500); resetar(); return; }

  uint8_t tz1 = as608Image2Tz(1);
  Serial.println("DBG|IMAGE2TZ1|" + String(tz1, HEX));
  if (tz1 != 0x00) { exibirLinha(0, "Erro leitura 1"); delay(1500); resetar(); return; }

  exibirMensagem("Retire o dedo", "");
  delay(2000);

  // ── Leitura 2 ──
  exibirMensagem("Coloque o dedo", "2a leitura");
  t = millis();
  r = 0xFF;
  while (millis() - t < 10000) {
    r = as608GetImage();
    Serial.println("DBG|GETIMAGE2|" + String(r, HEX));
    if (r == 0x00) break;
    delay(300);
  }
  if (r != 0x00) { Serial.println("EVT|FINGER|FAIL|TIMEOUT2"); exibirLinha(0,"Tempo esgotado"); delay(1500); resetar(); return; }

  uint8_t tz2 = as608Image2Tz(2);
  Serial.println("DBG|IMAGE2TZ2|" + String(tz2, HEX));
  if (tz2 != 0x00) { exibirLinha(0, "Erro leitura 2"); delay(1500); resetar(); return; }

  uint8_t cm = as608CreateModel();
  Serial.println("DBG|CREATEMODEL|" + String(cm, HEX));
  if (cm != 0x00) { exibirLinha(0, "Digitais diff."); delay(1500); resetar(); return; }

  uint8_t sm = as608StoreModel(enrollId);
  Serial.println("DBG|STOREMODEL|" + String(sm, HEX));
  if (sm == 0x00) {
    exibirMensagem("Digital", "Cadastrada!");
    Serial.println("EVT|FINGER|ENROLLED|" + idDigitado);
    delay(1500);
    resetar();
  } else {
    exibirLinha(0, "Erro ao salvar");
    delay(1500);
    resetar();
  }
}
// ═══════════════════════════════════════
// RESETAR
// ═══════════════════════════════════════
void resetar() {
  idDigitado = ""; senhaDigitada = ""; enrollId = 0;
  estadoAtual = AGUARDANDO_ID;
  exibirMensagem("Sistema Pronto", "Digite o ID:");
}
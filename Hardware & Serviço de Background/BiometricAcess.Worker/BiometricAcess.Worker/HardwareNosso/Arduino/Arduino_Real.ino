#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <Keypad.h>
#include <Adafruit_Fingerprint.h>
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
// AS608
// ═══════════════════════════════════════
SoftwareSerial serialSensor(10, 11); // RX=10, TX=11
Adafruit_Fingerprint finger = Adafruit_Fingerprint(&serialSensor);
const int PIN_TCH = 12;

// ═══════════════════════════════════════
// ESTADOS DO SISTEMA
// ═══════════════════════════════════════
enum Estado {
  AGUARDANDO_ID,
  AGUARDANDO_SENHA,
  AGUARDANDO_RESPOSTA_CS,
  AGUARDANDO_DIGITAL,
  AGUARDANDO_ENROLL_1,   // primeira leitura do cadastro
  AGUARDANDO_ENROLL_2    // segunda leitura do cadastro
};

Estado estadoAtual   = AGUARDANDO_ID;
String idDigitado    = "";
String senhaDigitada = "";
int enrollId         = 0;

// ═══════════════════════════════════════
// SETUP
// ═══════════════════════════════════════
void setup() {
  Serial.begin(9600);
  delay(500);
  Wire.begin();
  lcd.init();
  lcd.init();
  lcd.backlight();

  pinMode(PIN_TCH, INPUT);

  // Inicia sensor
  finger.begin(57600);
  if (finger.verifyPassword()) {
    Serial.println("EVT|FINGER|SENSOR|OK");
  } else {
    Serial.println("EVT|FINGER|SENSOR|FAIL");
    exibirMensagem("Sensor", "Nao encontrado");
    delay(2000);
  }

  exibirMensagem("Sistema Pronto", "Digite o ID:");
  Serial.println("EVT|READY");
}

// ═══════════════════════════════════════
// FUNÇÕES DO LCD
// ═══════════════════════════════════════
void exibirMensagem(String linha1, String linha2) {
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print(linha1);
  lcd.setCursor(0, 1);
  lcd.print(linha2);
}

void exibirLinha(int linha, String texto) {
  lcd.setCursor(0, linha);
  lcd.print("                ");
  lcd.setCursor(0, linha);
  lcd.print(texto);
}

// ═══════════════════════════════════════
// LOOP
// ═══════════════════════════════════════
void loop() {
  lerTeclado();
  lerComandoSerial();
  lerSensorToque();
}

// ═══════════════════════════════════════
// SENSOR DE TOQUE
// ═══════════════════════════════════════
void lerSensorToque() {
  static bool dedoPosto = false;
  bool toque = digitalRead(PIN_TCH) == HIGH;

  if (toque && !dedoPosto) {
    dedoPosto = true;
    Serial.println("EVT|FINGER|PLACED");
  } else if (!toque && dedoPosto) {
    dedoPosto = false;
    Serial.println("EVT|FINGER|REMOVED");
  }
}

// ═══════════════════════════════════════
// LEITURA DO TECLADO
// ═══════════════════════════════════════
void lerTeclado() {
  char tecla = keypad.getKey();
  if (tecla == NO_KEY) return;

  // ── AGUARDANDO ID ──
  if (estadoAtual == AGUARDANDO_ID) {
    if (tecla == '#') {
      idDigitado = "";
      exibirMensagem("Cancelado", "Digite o ID:");
      return;
    }

    if (tecla == '*') {
      if (idDigitado.length() != 6) {
        exibirLinha(0, "ID: 6 digitos!");
        delay(1500);
        exibirMensagem("Digite o ID:", "");
        return;
      }
      estadoAtual = AGUARDANDO_RESPOSTA_CS;
      exibirMensagem("Verificando...", "Aguarde");
      Serial.println("EVT|ID|" + idDigitado);
      return;
    }

    if (idDigitado.length() < 6) {
      idDigitado += tecla;
      String asteriscos = "";
      for (int i = 0; i < idDigitado.length(); i++) asteriscos += "*";
      exibirLinha(1, "ID: " + asteriscos);
    }
    return;
  }

  // ── AGUARDANDO SENHA ──
  if (estadoAtual == AGUARDANDO_SENHA) {
    if (tecla == '#') {
      idDigitado = "";
      senhaDigitada = "";
      estadoAtual = AGUARDANDO_ID;
      exibirMensagem("Cancelado", "Digite o ID:");
      return;
    }

    if (tecla == '*') {
      if (senhaDigitada.length() != 6) {
        exibirLinha(0, "Senha: 6 dig!");
        delay(1500);
        exibirMensagem("1o Acesso", "Senha:");
        return;
      }
      estadoAtual = AGUARDANDO_RESPOSTA_CS;
      exibirMensagem("Verificando...", "Aguarde");
      Serial.println("EVT|SENHA|" + idDigitado + "|" + senhaDigitada);
      return;
    }

    if (senhaDigitada.length() < 6) {
      senhaDigitada += tecla;
      String asteriscos = "";
      for (int i = 0; i < senhaDigitada.length(); i++) asteriscos += "*";
      exibirLinha(1, "Senha: " + asteriscos);
    }
    return;
  }

  // ── AGUARDANDO DIGITAL — tecla # cancela ──
  if (estadoAtual == AGUARDANDO_DIGITAL ||
      estadoAtual == AGUARDANDO_ENROLL_1 ||
      estadoAtual == AGUARDANDO_ENROLL_2) {
    if (tecla == '#') {
      Serial.println("EVT|FINGER|CANCEL");
      resetar();
    }
  }
}

// ═══════════════════════════════════════
// LEITURA DE COMANDOS DO C#
// ═══════════════════════════════════════
void lerComandoSerial() {
  if (!Serial.available()) return;

  String comando = Serial.readStringUntil('\n');
  comando.trim();

  if (comando.startsWith("CMD|LCD|LINE1|")) {
    exibirLinha(0, comando.substring(14));
  }
  else if (comando.startsWith("CMD|LCD|LINE2|")) {
    exibirLinha(1, comando.substring(14));
  }
  else if (comando.startsWith("CMD|LCD|CLEAR")) {
    lcd.clear();
  }
  // C# pede senha — primeiro acesso
  else if (comando.startsWith("CMD|ASK|PASSWORD")) {
    estadoAtual = AGUARDANDO_SENHA;
    senhaDigitada = "";
    exibirMensagem("1o Acesso", "Senha:");
  }
  // C# pede verificação de digital
  else if (comando.startsWith("CMD|FINGER|START_VERIFY")) {
    estadoAtual = AGUARDANDO_DIGITAL;
    exibirMensagem("ID: " + idDigitado, "Coloque o dedo");
    verificarDigital();
  }
  // C# pede cadastro de digital — primeiro acesso
  else if (comando.startsWith("CMD|FINGER|START_ENROLL|")) {
    enrollId = comando.substring(24).toInt();
    estadoAtual = AGUARDANDO_ENROLL_1;
    exibirMensagem("Cadastrar digital", "Coloque o dedo");
    cadastrarDigital();
  }
  // C# libera acesso
  else if (comando.startsWith("CMD|BUZZER|OK")) {
    exibirLinha(0, "Acesso Liberado!");
    delay(1500);
    resetar();
  }
  // C# nega acesso
  else if (comando.startsWith("CMD|BUZZER|FAIL")) {
    exibirLinha(0, "Acesso Negado!");
    delay(1500);
    resetar();
  }
  // C# nega com motivo
  else if (comando.startsWith("CMD|ACCESS|DENIED|")) {
    String motivo = comando.substring(18);
    exibirMensagem("Acesso Negado!", motivo);
    delay(2000);
    resetar();
  }
  // C# cancela
  else if (comando.startsWith("CMD|FINGER|CANCEL")) {
    resetar();
  }
}

// ═══════════════════════════════════════
// VERIFICAR DIGITAL (acesso normal)
// ═══════════════════════════════════════
void verificarDigital() {
  exibirLinha(1, "Coloque o dedo");

  // Aguarda dedo por até 10 segundos
  int timeout = 100;
  while (finger.getImage() != FINGERPRINT_OK && timeout > 0) {
    delay(100);
    timeout--;
  }

  if (timeout == 0) {
    exibirLinha(0, "Timeout");
    delay(1500);
    Serial.println("EVT|FINGER|FAIL");
    resetar();
    return;
  }

  if (finger.image2Tz() != FINGERPRINT_OK) {
    Serial.println("EVT|FINGER|FAIL");
    resetar();
    return;
  }

  if (finger.fingerSearch() == FINGERPRINT_OK) {
    Serial.println("EVT|FINGER|OK|" + idDigitado);
  } else {
    Serial.println("EVT|FINGER|FAIL");
    exibirLinha(0, "Nao reconhecido");
    delay(1500);
    resetar();
  }
}

// ═══════════════════════════════════════
// CADASTRAR DIGITAL (primeiro acesso)
// Requer 2 leituras para confirmar
// ═══════════════════════════════════════
void cadastrarDigital() {
  // Leitura 1
  exibirMensagem("Coloque o dedo", "1a leitura");
  while (finger.getImage() != FINGERPRINT_OK) delay(100);
  if (finger.image2Tz(1) != FINGERPRINT_OK) {
    exibirLinha(0, "Erro na leitura");
    delay(1500);
    resetar();
    return;
  }

  exibirMensagem("Retire o dedo", "");
  delay(2000);

  // Leitura 2
  exibirMensagem("Coloque o dedo", "2a leitura");
  while (finger.getImage() != FINGERPRINT_OK) delay(100);
  if (finger.image2Tz(2) != FINGERPRINT_OK) {
    exibirLinha(0, "Erro na leitura");
    delay(1500);
    resetar();
    return;
  }

  // Cria modelo e salva
  if (finger.createModel() != FINGERPRINT_OK) {
    exibirLinha(0, "Digitais differ.");
    delay(1500);
    resetar();
    return;
  }

  if (finger.storeModel(enrollId) == FINGERPRINT_OK) {
    exibirMensagem("Digital", "Cadastrada!");
    Serial.println("EVT|FINGER|ENROLLED|" + idDigitado);
    delay(1500);
  } else {
    exibirLinha(0, "Erro ao salvar");
    delay(1500);
    resetar();
  }
}

// ═══════════════════════════════════════
// RESETAR ESTADO
// ═══════════════════════════════════════
void resetar() {
  idDigitado    = "";
  senhaDigitada = "";
  enrollId      = 0;
  estadoAtual   = AGUARDANDO_ID;
  exibirMensagem("Sistema Pronto", "Digite o ID:");
}
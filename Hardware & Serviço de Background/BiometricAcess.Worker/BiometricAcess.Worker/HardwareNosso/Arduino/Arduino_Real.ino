#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <Keypad.h>

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
// ESTADOS DO SISTEMA
// ═══════════════════════════════════════
enum Estado {
  AGUARDANDO_ID,
  AGUARDANDO_SENHA,
  AGUARDANDO_RESPOSTA_CS,
  AGUARDANDO_DIGITAL
};

Estado estadoAtual   = AGUARDANDO_ID;
String idDigitado    = "";
String senhaDigitada = "";

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
  delay(100);
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

      // Manda ID pro C# decidir o próximo passo
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

      // Manda senha pro C# validar
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
  // C# pede digital — pessoa já tem biometria
  else if (comando.startsWith("CMD|FINGER|START_VERIFY")) {
    estadoAtual = AGUARDANDO_DIGITAL;
    exibirMensagem("ID: " + idDigitado, "Coloque o dedo");
  }
  // C# pede cadastro de digital — primeiro acesso após senha validada
  else if (comando.startsWith("CMD|FINGER|START_ENROLL")) {
    estadoAtual = AGUARDANDO_DIGITAL;
    exibirMensagem("Cadastrar digital", "Coloque o dedo");
  }
  // C# confirma acesso liberado
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
  // C# cancela operação
  else if (comando.startsWith("CMD|FINGER|CANCEL")) {
    resetar();
  }
  // Resultado da digital — AS608 real vai gerar isso
  // No hardware real esse evento vem do sensor, não do C#
  // Mas mantemos aqui para testes manuais via Serial
  else if (comando.startsWith("CMD|FINGER|RESULT|OK")) {
    Serial.println("EVT|FINGER|OK|" + idDigitado);
    exibirLinha(0, "Acesso Liberado!");
    delay(1500);
    resetar();
  }
  else if (comando.startsWith("CMD|FINGER|RESULT|FAIL")) {
    Serial.println("EVT|FINGER|FAIL");
    exibirLinha(0, "Nao reconhecido");
    delay(1500);
    resetar();
  }
  else if (comando.startsWith("CMD|FINGER|RESULT|ENROLLED")) {
    Serial.println("EVT|FINGER|ENROLLED|" + idDigitado);
    exibirMensagem("Digital", "Cadastrada!");
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
  estadoAtual   = AGUARDANDO_ID;
  exibirMensagem("Sistema Pronto", "Digite o ID:");
}
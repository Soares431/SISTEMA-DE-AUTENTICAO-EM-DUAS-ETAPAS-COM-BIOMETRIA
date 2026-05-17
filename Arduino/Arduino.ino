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

Estado estadoAtual = AGUARDANDO_ID;
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
        exibirLinha(1, "ID: " + idDigitado);
        return;
      }
      // ID completo — pede a senha
      estadoAtual = AGUARDANDO_SENHA;
      senhaDigitada = "";
      exibirMensagem("ID: " + idDigitado, "Senha:");
      return;
    }

    if (idDigitado.length() < 6) {
      idDigitado += tecla;
      // Mostra asteriscos para privacidade
      String asteriscos = "";
      for (int i = 0; i < idDigitado.length(); i++) asteriscos += "*";
      exibirLinha(1, "ID: " + asteriscos);
    }
    return;
  }

  // ── AGUARDANDO SENHA ──
  if (estadoAtual == AGUARDANDO_SENHA) {
    if (tecla == '#') {
      // Volta para o início
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
        exibirMensagem("ID: " + idDigitado, "Senha:");
        return;
      }
      // Manda ID e senha pro C# decidir o próximo passo
      estadoAtual = AGUARDANDO_RESPOSTA_CS;
      exibirMensagem("Verificando...", "Aguarde");
      Serial.println("EVT|AUTH|" + idDigitado + "|" + senhaDigitada);
      return;
    }

    if (senhaDigitada.length() < 6) {
      senhaDigitada += tecla;
      // Mostra asteriscos para privacidade
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
  // C# manda esse comando no primeiro acesso
  else if (comando.startsWith("CMD|FINGER|START_ENROLL")) {
    estadoAtual = AGUARDANDO_DIGITAL;
    exibirMensagem("1o Acesso", "Coloque o dedo");
    delay(2000);
    // Mock — simula cadastro de digital bem sucedido
    exibirMensagem("Digital", "Cadastrada!");
    Serial.println("EVT|FINGER|ENROLLED|" + idDigitado);
    delay(1500);
    exibirMensagem("Acesso Liberado", "Bem vindo!");
    delay(1500);
    idDigitado = "";
    senhaDigitada = "";
    estadoAtual = AGUARDANDO_ID;
    exibirMensagem("Sistema Pronto", "Digite o ID:");
  }
  // C# manda esse comando quando pessoa já tem biometria
  else if (comando.startsWith("CMD|FINGER|START_VERIFY")) {
    estadoAtual = AGUARDANDO_DIGITAL;
    exibirMensagem("ID: " + idDigitado, "Coloque o dedo");
    delay(2000);

    static int tentativas = 0;
    tentativas++;

    if (tentativas % 3 == 0) {
      Serial.println("EVT|FINGER|FAIL");
      exibirLinha(0, "Nao reconhecido");
      delay(1500);
      idDigitado = "";
      senhaDigitada = "";
      estadoAtual = AGUARDANDO_ID;
      exibirMensagem("Sistema Pronto", "Digite o ID:");
    } else {
      Serial.println("EVT|FINGER|OK|" + idDigitado);
      // Mock local enquanto C# não está conectado
      exibirLinha(0, "Acesso Liberado!");
      delay(1500);
      idDigitado = "";
      senhaDigitada = "";
      estadoAtual = AGUARDANDO_ID;
      exibirMensagem("Sistema Pronto", "Digite o ID:");
    }
  }
  // C# manda esse quando acesso negado
  else if (comando.startsWith("CMD|ACCESS|DENIED|")) {
    String motivo = comando.substring(18);
    exibirLinha(0, "Acesso Negado!");
    exibirLinha(1, motivo);
    delay(2000);
    idDigitado = "";
    senhaDigitada = "";
    estadoAtual = AGUARDANDO_ID;
    exibirMensagem("Sistema Pronto", "Digite o ID:");
  }
  else if (comando.startsWith("CMD|FINGER|CANCEL")) {
    idDigitado = "";
    senhaDigitada = "";
    estadoAtual = AGUARDANDO_ID;
    exibirMensagem("Cancelado", "Digite o ID:");
  }
  else if (comando.startsWith("CMD|BUZZER|OK")) {
    exibirLinha(0, "Acesso Liberado!");
    delay(1500);
    idDigitado = "";
    senhaDigitada = "";
    estadoAtual = AGUARDANDO_ID;
    exibirMensagem("Sistema Pronto", "Digite o ID:");
  }
  else if (comando.startsWith("CMD|BUZZER|FAIL")) {
    exibirLinha(0, "Acesso Negado!");
    delay(1500);
    idDigitado = "";
    senhaDigitada = "";
    estadoAtual = AGUARDANDO_ID;
    exibirMensagem("Sistema Pronto", "Digite o ID:");
  }
}
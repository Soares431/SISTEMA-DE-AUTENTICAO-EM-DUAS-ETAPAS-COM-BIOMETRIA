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
// DADOS MOCKADOS
// ═══════════════════════════════════════
struct Pessoa {
  long id;
  char senha[7];
  bool ativa;
  bool temBiometria;
};

// Edite aqui para adicionar/remover pessoas de teste
Pessoa pessoas[] = {
  { 100001, "123456", true,  false },  // primeiro acesso
  { 100002, "654321", true,  true  },  // acesso normal
  { 100003, "111111", false, false },  // inativa
  { 100004, "999999", true,  true  },  // acesso normal
};
const int totalPessoas = 4;

// ═══════════════════════════════════════
// ESTADOS DO SISTEMA
// ═══════════════════════════════════════
enum Estado {
  AGUARDANDO_ID,
  AGUARDANDO_SENHA,
  AGUARDANDO_DIGITAL
};

Estado estadoAtual  = AGUARDANDO_ID;
String idDigitado   = "";
String senhaDigitada = "";
int pessoaAtualIdx  = -1;

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
// BUSCA PESSOA POR ID
// ═══════════════════════════════════════
int buscarPessoa(long id) {
  for (int i = 0; i < totalPessoas; i++) {
    if (pessoas[i].id == id) return i;
  }
  return -1;
}

// ═══════════════════════════════════════
// LOOP
// ═══════════════════════════════════════
void loop() {
  lerTeclado();
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

      long idNum = idDigitado.toInt();
      int idx = buscarPessoa(idNum);

      // Pessoa não existe
      if (idx == -1) {
        exibirMensagem("Acesso Negado!", "nao_cadastrado");
        delay(2000);
        idDigitado = "";
        exibirMensagem("Sistema Pronto", "Digite o ID:");
        return;
      }

      // Pessoa inativa
      if (!pessoas[idx].ativa) {
        exibirMensagem("Acesso Negado!", "inativo");
        delay(2000);
        idDigitado = "";
        exibirMensagem("Sistema Pronto", "Digite o ID:");
        return;
      }

      pessoaAtualIdx = idx;

      // Tem biometria — vai direto para digital
      if (pessoas[idx].temBiometria) {
        estadoAtual = AGUARDANDO_DIGITAL;
        exibirMensagem("ID: " + idDigitado, "Coloque o dedo");
        delay(2000);
        simularDigital();
        return;
      }

      // Primeiro acesso — pede senha
      estadoAtual = AGUARDANDO_SENHA;
      senhaDigitada = "";
      exibirMensagem("1o Acesso", "Senha:");
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
      pessoaAtualIdx = -1;
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

      // Valida senha
      if (senhaDigitada != String(pessoas[pessoaAtualIdx].senha)) {
        exibirMensagem("Acesso Negado!", "senha_incorreta");
        delay(2000);
        idDigitado = "";
        senhaDigitada = "";
        pessoaAtualIdx = -1;
        estadoAtual = AGUARDANDO_ID;
        exibirMensagem("Sistema Pronto", "Digite o ID:");
        return;
      }

      // Senha correta — cadastra biometria
      estadoAtual = AGUARDANDO_DIGITAL;
      exibirMensagem("Coloque o dedo", "para cadastrar");
      delay(2000);

      // Mock — simula cadastro
      pessoas[pessoaAtualIdx].temBiometria = true;
      exibirMensagem("Digital", "Cadastrada!");
      delay(1500);
      exibirMensagem("Acesso Liberado", "Bem vindo!");
      delay(1500);

      idDigitado = "";
      senhaDigitada = "";
      pessoaAtualIdx = -1;
      estadoAtual = AGUARDANDO_ID;
      exibirMensagem("Sistema Pronto", "Digite o ID:");
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
// SIMULAR DIGITAL
// ═══════════════════════════════════════
void simularDigital() {
  static int tentativas = 0;
  tentativas++;

  if (tentativas % 3 == 0) {
    // A cada 3 tentativas simula falha
    exibirMensagem("Nao reconhecido", "Tente novamente");
    delay(1500);
  } else {
    // Sucesso
    exibirMensagem("Acesso Liberado!", "Bem vindo!");
    delay(1500);
  }

  idDigitado = "";
  senhaDigitada = "";
  pessoaAtualIdx = -1;
  estadoAtual = AGUARDANDO_ID;
  exibirMensagem("Sistema Pronto", "Digite o ID:");
}
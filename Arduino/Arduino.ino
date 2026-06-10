// ═══════════════════════════════════════════════════════════════
// Arduino — Terminal de acesso (cliente Serial do Worker .NET)
// ═══════════════════════════════════════════════════════════════
// Sem dados locais. O Arduino apenas:
//   1. Coleta ID (6 dig) e senha (6 dig) pelo keypad
//   2. Envia EVT|ID|... e EVT|SENHA|... pelo Serial
//   3. Reage aos CMD que o Worker manda de volta
//
// Quem decide tudo (existe? está ativo? tem permissão? senha bate?
// tem biometria?) é o Worker (EventProcessorArduino.cs) lendo o banco.
//
// Digital ainda é SIMULADA — tecla A confirma sucesso, tecla B simula
// falha. Pra integrar sensor real (FPM10A / AS608), basta substituir
// os trechos "// [SIM-DIGITAL]" pelas chamadas do sensor.
// ═══════════════════════════════════════════════════════════════

#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <Keypad.h>

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

// ── BUZZER (opcional — se não tiver, mantém pino solto) ──────────
const byte BUZZER_PIN = 10;

// ── ESTADO ───────────────────────────────────────────────────────
enum Estado {
  DIGITANDO_ID,            // coletando 6 dígitos do ID
  AGUARDANDO_SERVIDOR_ID,  // EVT|ID enviado, esperando CMD do Worker
  DIGITANDO_SENHA,         // coletando 6 dígitos da senha
  AGUARDANDO_SERVIDOR_SENHA, // EVT|SENHA enviado, esperando CMD
  SIMULANDO_VERIFY,        // Worker pediu digital de verificação (A=OK, B=FAIL)
  SIMULANDO_ENROLL,        // Worker pediu cadastro de digital (A=concluir)
  MOSTRANDO_RESULTADO      // mensagem na tela por X ms antes de voltar pro início
};

Estado estado = DIGITANDO_ID;
String idDigitado = "";
String senhaDigitada = "";
String bufferSerial = "";
unsigned long tempoResultado = 0;
const unsigned long DURACAO_RESULTADO_MS = 2500;

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

  telaInicial();
  Serial.println("EVT|READY");
}

// ═══════════════════════════════════════════════════════════════
// LOOP
// ═══════════════════════════════════════════════════════════════
void loop() {
  receberSerial();
  voltarSeResultadoExpirou();
  lerTeclado();
}

// ═══════════════════════════════════════════════════════════════
// LCD HELPERS
// ═══════════════════════════════════════════════════════════════
void telaInicial() {
  estado = DIGITANDO_ID;
  idDigitado = "";
  senhaDigitada = "";
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

// Exibe uma tela final por DURACAO_RESULTADO_MS e depois reseta.
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

// Quebra "CMD|MOD|ACAO|DADO|DADO2" em campos.
// Retorna quantos campos foram preenchidos (até 5).
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

  // CMD|LCD|LINE1|texto
  if (c[1] == "LCD" && c[2] == "LINE1") { mostrarLinha(0, n >= 4 ? c[3] : ""); return; }
  if (c[1] == "LCD" && c[2] == "LINE2") { mostrarLinha(1, n >= 4 ? c[3] : ""); return; }
  if (c[1] == "LCD" && c[2] == "CLEAR") { lcd.clear(); return; }

  // CMD|ASK|PASSWORD — Worker pede senha pra essa pessoa
  if (c[1] == "ASK" && c[2] == "PASSWORD") {
    estado = DIGITANDO_SENHA;
    senhaDigitada = "";
    mostrarDuasLinhas("1o Acesso", "Senha:");
    return;
  }

  // CMD|FINGER|START_VERIFY — Worker pede pra verificar digital
  if (c[1] == "FINGER" && c[2] == "START_VERIFY") {
    estado = SIMULANDO_VERIFY;
    mostrarDuasLinhas("Coloque o dedo", "A=OK  B=Falha");
    return;
  }

  // CMD|FINGER|START_ENROLL — Worker pede pra cadastrar digital
  if (c[1] == "FINGER" && c[2] == "START_ENROLL") {
    estado = SIMULANDO_ENROLL;
    mostrarDuasLinhas("Cadastrando dig", "A=Confirmar");
    return;
  }

  // CMD|FINGER|CANCEL — Worker cancela operação biométrica
  if (c[1] == "FINGER" && c[2] == "CANCEL") {
    mostrarResultado("Cancelado", "");
    return;
  }

  // CMD|BUZZER|OK
  if (c[1] == "BUZZER" && c[2] == "OK") { beepOk(); return; }
  // CMD|BUZZER|FAIL
  if (c[1] == "BUZZER" && c[2] == "FAIL") { beepFalha(); return; }

  // CMD|ACCESS|DENIED|motivo
  if (c[1] == "ACCESS" && c[2] == "DENIED") {
    beepFalha();
    String motivo = n >= 4 ? c[3] : "negado";
    mostrarResultado("Acesso Negado!", motivo);
    return;
  }
}

// ═══════════════════════════════════════════════════════════════
// KEYPAD
// ═══════════════════════════════════════════════════════════════
void lerTeclado() {
  if (estado == MOSTRANDO_RESULTADO) return;
  if (estado == AGUARDANDO_SERVIDOR_ID || estado == AGUARDANDO_SERVIDOR_SENHA) return;

  char tecla = keypad.getKey();
  if (tecla == NO_KEY) return;

  // ── Verify de digital (simulada): A confirma, B nega ──────────
  if (estado == SIMULANDO_VERIFY) {
    if (tecla == 'A') {
      Serial.println("EVT|FINGER|OK|" + idDigitado);
      beepOk();
      mostrarResultado("Acesso Liberado", "Bem vindo!");
    } else if (tecla == 'B') {
      Serial.println("EVT|FINGER|FAIL");
      beepFalha();
      mostrarResultado("Digital falhou", "Tente novamente");
    }
    return;
  }

  // ── Enroll de digital (simulada): A conclui cadastro ───────────
  if (estado == SIMULANDO_ENROLL) {
    if (tecla == 'A') {
      Serial.println("EVT|FINGER|ENROLLED|" + idDigitado);
      beepOk();
      mostrarResultado("Digital", "Cadastrada!");
    }
    return;
  }

  // ── Digitando ID ───────────────────────────────────────────────
  if (estado == DIGITANDO_ID) {
    if (tecla == '#') { telaInicial(); return; }   // cancela
    if (tecla == '*') {                            // confirma
      if (idDigitado.length() != 6) {
        mostrarLinha(0, "ID: 6 digitos!");
        delay(1200);
        mostrarDuasLinhas("Sistema Pronto", "Digite o ID:");
        return;
      }
      // Envia ID pro Worker — ele decide o próximo passo
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

  // ── Digitando senha (após CMD|ASK|PASSWORD) ────────────────────
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

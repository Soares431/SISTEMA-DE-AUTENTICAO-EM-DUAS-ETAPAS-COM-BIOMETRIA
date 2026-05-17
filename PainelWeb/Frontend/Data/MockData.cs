// =============================================================================
// MockData.cs - Dados simulados para testes de interface
// Sistema de Controle de Acesso Biométrico do 5° CTA
// Nota: Em produção, estes dados serão substituídos por Entity Framework + SQLite
// =============================================================================

namespace FrontendControleAcesso.Data;

/// <summary>
/// Representa um ambiente físico monitorado pelo sistema de controle de acesso.
/// Cada ambiente possui um dispositivo T50 para leitura biométrica.
/// </summary>
public class Ambiente
{
    /// <summary>Identificador único do ambiente.</summary>
    public int Id { get; set; }

    /// <summary>Nome de exibição do ambiente (ex: "Recepção Principal").</summary>
    public string Nome { get; set; } = "";

    /// <summary>Descrição detalhada do ambiente.</summary>
    public string Descricao { get; set; } = "";

    /// <summary>Status do dispositivo T50: "online", "alerta" ou "offline".</summary>
    public string T50Status { get; set; } = "online";

    /// <summary>Percentual de capacidade utilizada do T50 (0-100).</summary>
    public int T50Capacidade { get; set; }

    /// <summary>Quantidade de pessoas autorizadas a acessar este ambiente.</summary>
    public int PessoasComAcesso { get; set; }

    /// <summary>Quantidade de câmeras vinculadas ao ambiente.</summary>
    public int Cameras { get; set; }

    /// <summary>Endereço IP do dispositivo T50 na rede local.</summary>
    public string T50Ip { get; set; } = "";

    /// <summary>Versão do firmware instalado no dispositivo T50.</summary>
    public string T50Firmware { get; set; } = "";

    /// <summary>Data e hora da última sincronização do T50 com o servidor.</summary>
    public string UltimaSincronizacao { get; set; } = "";
}

/// <summary>
/// Representa uma pessoa cadastrada no sistema de controle de acesso.
/// Inclui dados pessoais, status de biometria e permissões de acesso.
/// </summary>
public class Pessoa
{
    /// <summary>Identificador único da pessoa.</summary>
    public int Id { get; set; }

    /// <summary>Nome completo da pessoa.</summary>
    public string Nome { get; set; } = "";

    /// <summary>Endereço de e-mail corporativo.</summary>
    public string Email { get; set; } = "";

    /// <summary>Número de telefone para contato.</summary>
    public string Telefone { get; set; } = "";

    /// <summary>Cargo ocupado na organização.</summary>
    public string Cargo { get; set; } = "";

    /// <summary>Departamento ao qual a pessoa pertence.</summary>
    public string Departamento { get; set; } = "";

    /// <summary>Status da pessoa: "ativo" ou "inativo".</summary>
    public string Status { get; set; } = "ativo";

    /// <summary>Status da biometria: "cadastrada" ou "pendente".</summary>
    public string BiometriaStatus { get; set; } = "cadastrada";

    /// <summary>Número de ambientes que a pessoa tem acesso.</summary>
    public int AmbientesAcesso { get; set; }

    /// <summary>Data e hora do último acesso registrado.</summary>
    public string UltimoAcesso { get; set; } = "";
}

/// <summary>
/// Representa uma câmera de vigilância vinculada a um ambiente.
/// Suporta streaming RTSP e gravação automática por evento de acesso.
/// </summary>
public class Camera
{
    /// <summary>Identificador único da câmera.</summary>
    public int Id { get; set; }

    /// <summary>Nome de identificação da câmera.</summary>
    public string Nome { get; set; } = "";

    /// <summary>Endereço IP da câmera na rede local.</summary>
    public string Ip { get; set; } = "";

    /// <summary>URL RTSP para streaming de vídeo ao vivo.</summary>
    public string RtspUrl { get; set; } = "";

    /// <summary>Nome do ambiente onde a câmera está instalada.</summary>
    public string Ambiente { get; set; } = "";

    /// <summary>Identificador do ambiente associado (chave estrangeira).</summary>
    public int AmbienteId { get; set; }

    /// <summary>Status da câmera: "online" ou "offline".</summary>
    public string Status { get; set; } = "online";

    /// <summary>Indica se a câmera está gravando no momento.</summary>
    public bool Gravando { get; set; }
}

/// <summary>
/// Representa um registro de tentativa de acesso (permitido ou negado).
/// Usado no histórico de acessos com suporte a gravação de vídeo.
/// </summary>
public class RegistroAcesso
{
    /// <summary>Identificador único do registro.</summary>
    public int Id { get; set; }

    /// <summary>Data do acesso no formato dd/MM/yyyy.</summary>
    public string Data { get; set; } = "";

    /// <summary>Hora do acesso no formato HH:mm:ss.</summary>
    public string Hora { get; set; } = "";

    /// <summary>Nome da pessoa que tentou o acesso.</summary>
    public string Pessoa { get; set; } = "";

    /// <summary>ID da pessoa (null quando não identificada).</summary>
    public int? PessoaId { get; set; }

    /// <summary>Nome do ambiente onde ocorreu a tentativa.</summary>
    public string Ambiente { get; set; } = "";

    /// <summary>ID do ambiente onde ocorreu a tentativa.</summary>
    public int AmbienteId { get; set; }

    /// <summary>Resultado do acesso: "permitido" ou "negado".</summary>
    public string Status { get; set; } = "permitido";

    /// <summary>Método de autenticação utilizado.</summary>
    public string Metodo { get; set; } = "Biometria";

    /// <summary>Motivo da negação (null quando permitido).</summary>
    public string? Motivo { get; set; }

    /// <summary>Indica se existe gravação de vídeo associada ao evento.</summary>
    public bool TemGravacao { get; set; }
}

/// <summary>
/// Representa um registro de log do sistema para auditoria.
/// Rastreia ações administrativas como cadastros, alterações e exclusões.
/// </summary>
public class LogSistema
{
    /// <summary>Identificador único do log.</summary>
    public int Id { get; set; }

    /// <summary>Data da ação no formato dd/MM/yyyy.</summary>
    public string Data { get; set; } = "";

    /// <summary>Hora da ação no formato HH:mm:ss.</summary>
    public string Hora { get; set; } = "";

    /// <summary>E-mail do usuário que executou a ação.</summary>
    public string Usuario { get; set; } = "";

    /// <summary>Descrição curta da ação realizada.</summary>
    public string Acao { get; set; } = "";

    /// <summary>Descrição detalhada do que foi feito.</summary>
    public string Descricao { get; set; } = "";

    /// <summary>Categoria do log para filtragem (ex: "pessoas", "acessos").</summary>
    public string Categoria { get; set; } = "";

    /// <summary>Endereço IP de onde a ação foi executada.</summary>
    public string Ip { get; set; } = "";
}

/// <summary>
/// Representa um alerta gerado pelo dispositivo T50.
/// Alertas indicam problemas de capacidade ou conexão.
/// </summary>
public class AlertaT50
{
    /// <summary>Identificador único do alerta.</summary>
    public int Id { get; set; }

    /// <summary>Nome do ambiente que gerou o alerta.</summary>
    public string Ambiente { get; set; } = "";

    /// <summary>Tipo do alerta (ex: "T50 Cheio").</summary>
    public string Tipo { get; set; } = "";

    /// <summary>Mensagem descritiva do alerta.</summary>
    public string Mensagem { get; set; } = "";

    /// <summary>Hora em que o alerta foi gerado.</summary>
    public string Hora { get; set; } = "";

    /// <summary>Nível de gravidade: "alta", "media" ou "baixa".</summary>
    public string Gravidade { get; set; } = "";
}

/// <summary>
/// Representa um acesso negado recente, exibido no dashboard.
/// </summary>
public class AcessoNegado
{
    /// <summary>Identificador único do registro.</summary>
    public int Id { get; set; }

    /// <summary>Nome da pessoa que teve acesso negado.</summary>
    public string Pessoa { get; set; } = "";

    /// <summary>Ambiente onde o acesso foi negado.</summary>
    public string Ambiente { get; set; } = "";

    /// <summary>Motivo da negação de acesso.</summary>
    public string Motivo { get; set; } = "";

    /// <summary>Hora da tentativa negada.</summary>
    public string Hora { get; set; } = "";

    /// <summary>Indica se existe gravação de vídeo do evento.</summary>
    public bool TemGravacao { get; set; }
}

/// <summary>
/// Versão simplificada de Ambiente para uso em dropdowns de filtro.
/// </summary>
public class AmbienteSimples
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
}

/// <summary>
/// Representa uma pessoa vinculada a um ambiente (usado em tabelas de permissão).
/// </summary>
public class PessoaAmbiente
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string UltimoAcesso { get; set; } = "";
}

/// <summary>
/// Representa uma câmera vinculada a um ambiente específico.
/// </summary>
public class CameraAmbiente
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Status { get; set; } = "";
    public string RtspUrl { get; set; } = "";
}

/// <summary>
/// Registro simplificado de histórico de acesso de uma pessoa.
/// </summary>
public class HistoricoPessoa
{
    public int Id { get; set; }
    public string Data { get; set; } = "";
    public string Ambiente { get; set; } = "";
    public string Status { get; set; } = "";
}

/// <summary>
/// Ambiente simplificado vinculado a uma pessoa (lista de permissões).
/// </summary>
public class AmbientePessoa
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string UltimoAcesso { get; set; } = "";
}

/// <summary>
/// Classe estática que fornece dados simulados (mock) para toda a interface.
/// Centraliza todos os dados de teste do sistema de controle de acesso.
/// Em produção, será substituída por repositórios com Entity Framework Core + SQLite.
/// </summary>
public static class MockData
{
    /// <summary>
    /// Lista de ambientes cadastrados no sistema.
    /// Inclui dados do dispositivo T50 associado a cada ambiente.
    /// </summary>
    public static List<Ambiente> Ambientes =>
    [
        new() { Id = 1, Nome = "Recepção Principal", Descricao = "Entrada principal do edificio", T50Status = "online", T50Capacidade = 45, PessoasComAcesso = 342, Cameras = 2, T50Ip = "192.168.1.100", T50Firmware = "v2.3.1", UltimaSincronizacao = "2024-01-15 14:32:00" },
        new() { Id = 2, Nome = "Sala de Servidores", Descricao = "Data center principal", T50Status = "alerta", T50Capacidade = 95, PessoasComAcesso = 12, Cameras = 4, T50Ip = "192.168.1.101", T50Firmware = "v2.3.1", UltimaSincronizacao = "2024-01-15 14:32:00" },
        new() { Id = 3, Nome = "Laboratório 1", Descricao = "Laboratório de pesquisa", T50Status = "online", T50Capacidade = 60, PessoasComAcesso = 28, Cameras = 1, T50Ip = "192.168.1.102", T50Firmware = "v2.3.0", UltimaSincronizacao = "2024-01-15 12:00:00" },
        new() { Id = 4, Nome = "Laboratório 2", Descricao = "Laboratório de desenvolvimento", T50Status = "alerta", T50Capacidade = 85, PessoasComAcesso = 35, Cameras = 1, T50Ip = "192.168.1.103", T50Firmware = "v2.3.0", UltimaSincronizacao = "2024-01-15 10:15:00" },
        new() { Id = 5, Nome = "Almoxarifado", Descricao = "Estoque de materiais", T50Status = "offline", T50Capacidade = 0, PessoasComAcesso = 8, Cameras = 2, T50Ip = "192.168.1.104", T50Firmware = "v2.2.5", UltimaSincronizacao = "2024-01-14 08:00:00" },
        new() { Id = 6, Nome = "Diretoria", Descricao = "Sala da diretoria executiva", T50Status = "online", T50Capacidade = 30, PessoasComAcesso = 5, Cameras = 1, T50Ip = "192.168.1.105", T50Firmware = "v2.3.1", UltimaSincronizacao = "2024-01-15 14:00:00" },
    ];

    /// <summary>
    /// Lista de pessoas cadastradas com dados pessoais e status de biometria.
    /// </summary>
    public static List<Pessoa> Pessoas =>
    [
        new() { Id = 1, Nome = "Carlos Silva", Email = "carlos.silva@empresa.com", Telefone = "(11) 99999-1234", Cargo = "Administrador de Sistemas", Departamento = "TI", Status = "ativo", BiometriaStatus = "cadastrada", AmbientesAcesso = 5, UltimoAcesso = "2024-01-15 14:32:00" },
        new() { Id = 2, Nome = "Ana Santos", Email = "ana.santos@empresa.com", Telefone = "(11) 99999-5678", Cargo = "Engenheira de Redes", Departamento = "TI", Status = "ativo", BiometriaStatus = "cadastrada", AmbientesAcesso = 4, UltimoAcesso = "2024-01-15 11:15:00" },
        new() { Id = 3, Nome = "Pedro Oliveira", Email = "pedro.oliveira@empresa.com", Telefone = "(11) 99999-9012", Cargo = "Técnico de Suporte", Departamento = "TI", Status = "ativo", BiometriaStatus = "pendente", AmbientesAcesso = 3, UltimoAcesso = "2024-01-14 16:45:00" },
        new() { Id = 4, Nome = "Maria Costa", Email = "maria.costa@empresa.com", Telefone = "(11) 99999-3456", Cargo = "Gerente de Projetos", Departamento = "Projetos", Status = "inativo", BiometriaStatus = "cadastrada", AmbientesAcesso = 6, UltimoAcesso = "2024-01-13 09:30:00" },
        new() { Id = 5, Nome = "José Lima", Email = "jose.lima@empresa.com", Telefone = "(11) 99999-7890", Cargo = "Desenvolvedor Sênior", Departamento = "Desenvolvimento", Status = "ativo", BiometriaStatus = "cadastrada", AmbientesAcesso = 2, UltimoAcesso = "2024-01-15 13:15:00" },
        new() { Id = 6, Nome = "Paula Souza", Email = "paula.souza@empresa.com", Telefone = "(11) 99999-2345", Cargo = "Analista de Segurança", Departamento = "Segurança", Status = "ativo", BiometriaStatus = "cadastrada", AmbientesAcesso = 8, UltimoAcesso = "2024-01-15 12:15:00" },
    ];

    /// <summary>
    /// Lista de câmeras de vigilância com dados de conexão RTSP.
    /// </summary>
    public static List<Camera> CamerasData =>
    [
        new() { Id = 1, Nome = "Recepção - Entrada Principal", Ip = "192.168.1.101", RtspUrl = "rtsp://192.168.1.101/stream1", Ambiente = "Recepção Principal", AmbienteId = 1, Status = "online", Gravando = true },
        new() { Id = 2, Nome = "Recepção - Lobby", Ip = "192.168.1.102", RtspUrl = "rtsp://192.168.1.102/stream1", Ambiente = "Recepção Principal", AmbienteId = 1, Status = "online", Gravando = true },
        new() { Id = 3, Nome = "Servidores - Entrada", Ip = "192.168.1.103", RtspUrl = "rtsp://192.168.1.103/stream1", Ambiente = "Sala de Servidores", AmbienteId = 2, Status = "online", Gravando = true },
        new() { Id = 4, Nome = "Servidores - Racks", Ip = "192.168.1.104", RtspUrl = "rtsp://192.168.1.104/stream1", Ambiente = "Sala de Servidores", AmbienteId = 2, Status = "online", Gravando = true },
        new() { Id = 5, Nome = "Laboratório 1 - Geral", Ip = "192.168.1.105", RtspUrl = "rtsp://192.168.1.105/stream1", Ambiente = "Laboratório 1", AmbienteId = 3, Status = "offline", Gravando = false },
        new() { Id = 6, Nome = "Almoxarifado - Corredor", Ip = "192.168.1.106", RtspUrl = "rtsp://192.168.1.106/stream1", Ambiente = "Almoxarifado", AmbienteId = 5, Status = "online", Gravando = false },
    ];

    /// <summary>
    /// Histórico completo de tentativas de acesso (permitidas e negadas).
    /// Alimenta a página de Histórico de Acessos com filtros e tabela.
    /// </summary>
    public static List<RegistroAcesso> Historico =>
    [
        new() { Id = 1, Data = "15/01/2024", Hora = "14:32:15", Pessoa = "Carlos Silva", PessoaId = 1, Ambiente = "Sala de Servidores", AmbienteId = 2, Status = "permitido", TemGravacao = true },
        new() { Id = 2, Data = "15/01/2024", Hora = "14:28:03", Pessoa = "Ana Santos", PessoaId = 2, Ambiente = "Laboratório 3", AmbienteId = 3, Status = "negado", Motivo = "Acesso não autorizado", TemGravacao = true },
        new() { Id = 3, Data = "15/01/2024", Hora = "14:15:47", Pessoa = "Pedro Oliveira", PessoaId = 3, Ambiente = "Recepção Principal", AmbienteId = 1, Status = "permitido", TemGravacao = true },
        new() { Id = 4, Data = "15/01/2024", Hora = "13:58:22", Pessoa = "Desconhecido", PessoaId = null, Ambiente = "Almoxarifado", AmbienteId = 5, Status = "negado", Motivo = "Biometria não reconhecida", TemGravacao = true },
        new() { Id = 5, Data = "15/01/2024", Hora = "13:45:11", Pessoa = "Maria Costa", PessoaId = 4, Ambiente = "Diretoria", AmbienteId = 6, Status = "permitido", TemGravacao = false },
        new() { Id = 6, Data = "15/01/2024", Hora = "13:30:05", Pessoa = "José Lima", PessoaId = 5, Ambiente = "Sala de Servidores", AmbienteId = 2, Status = "negado", Motivo = "Horário restrito", TemGravacao = true },
        new() { Id = 7, Data = "15/01/2024", Hora = "12:15:33", Pessoa = "Paula Souza", PessoaId = 6, Ambiente = "Laboratório 1", AmbienteId = 3, Status = "permitido", TemGravacao = true },
        new() { Id = 8, Data = "15/01/2024", Hora = "11:45:18", Pessoa = "Carlos Silva", PessoaId = 1, Ambiente = "Recepção Principal", AmbienteId = 1, Status = "permitido", TemGravacao = true },
        new() { Id = 9, Data = "15/01/2024", Hora = "11:30:02", Pessoa = "Ana Santos", PessoaId = 2, Ambiente = "Laboratório 2", AmbienteId = 4, Status = "permitido", TemGravacao = true },
        new() { Id = 10, Data = "15/01/2024", Hora = "10:15:44", Pessoa = "Pedro Oliveira", PessoaId = 3, Ambiente = "Almoxarifado", AmbienteId = 5, Status = "negado", Motivo = "Acesso não autorizado", TemGravacao = true },
    ];

    /// <summary>
    /// Logs de auditoria do sistema, registrando ações administrativas.
    /// Alimenta a página de Logs do Sistema com filtros por categoria e usuário.
    /// </summary>
    public static List<LogSistema> Logs =>
    [
        new() { Id = 1, Data = "15/01/2024", Hora = "14:45:22", Usuario = "admin@empresa.com", Acao = "Cadastro de Pessoa", Descricao = "Cadastrou nova pessoa: Paula Souza", Categoria = "pessoas", Ip = "192.168.1.50" },
        new() { Id = 2, Data = "15/01/2024", Hora = "14:30:15", Usuario = "admin@empresa.com", Acao = "Alteração de Acesso", Descricao = "Concedeu acesso ao ambiente 'Sala de Servidores' para Carlos Silva", Categoria = "acessos", Ip = "192.168.1.50" },
        new() { Id = 3, Data = "15/01/2024", Hora = "13:15:08", Usuario = "gerente@empresa.com", Acao = "Reset de Biometria", Descricao = "Resetou biometria de Ana Santos", Categoria = "biometria", Ip = "192.168.1.55" },
        new() { Id = 4, Data = "15/01/2024", Hora = "12:45:33", Usuario = "admin@empresa.com", Acao = "Cadastro de Câmera", Descricao = "Cadastrou nova câmera: Laboratorio 2 - Entrada", Categoria = "cameras", Ip = "192.168.1.50" },
        new() { Id = 5, Data = "15/01/2024", Hora = "11:30:41", Usuario = "admin@empresa.com", Acao = "Alteração de Configuração", Descricao = "Alterou tempo de retenção de dados de 90 para 180 dias", Categoria = "configuracoes", Ip = "192.168.1.50" },
        new() { Id = 6, Data = "15/01/2024", Hora = "10:15:27", Usuario = "gerente@empresa.com", Acao = "Inativação de Pessoa", Descricao = "Inativou pessoa: Roberto Santos", Categoria = "pessoas", Ip = "192.168.1.55" },
        new() { Id = 7, Data = "15/01/2024", Hora = "09:45:12", Usuario = "admin@empresa.com", Acao = "Cadastro de Ambiente", Descricao = "Cadastrou novo ambiente: Sala de Reunioes 3", Categoria = "ambientes", Ip = "192.168.1.50" },
        new() { Id = 8, Data = "14/01/2024", Hora = "17:30:55", Usuario = "admin@empresa.com", Acao = "Remoção de Acesso", Descricao = "Removeu acesso de Maria Costa ao ambiente 'Almoxarifado'", Categoria = "acessos", Ip = "192.168.1.50" },
        new() { Id = 9, Data = "14/01/2024", Hora = "16:15:03", Usuario = "gerente@empresa.com", Acao = "Reenvio de Senha", Descricao = "Reenviou senha para Jose Lima", Categoria = "senhas", Ip = "192.168.1.55" },
        new() { Id = 10, Data = "14/01/2024", Hora = "15:00:18", Usuario = "admin@empresa.com", Acao = "Login no Sistema", Descricao = "Login realizado com sucesso", Categoria = "autenticacao", Ip = "192.168.1.50" },
    ];

    /// <summary>
    /// Alertas ativos dos dispositivos T50 exibidos no dashboard.
    /// </summary>
    public static List<AlertaT50> Alertas =>
    [
        new() { Id = 1, Ambiente = "Sala de Servidores", Tipo = "T50 Cheio", Mensagem = "Capacidade de armazenamento biométrico atingiu 95%", Hora = "14:32", Gravidade = "alta" },
        new() { Id = 2, Ambiente = "Recepção Principal", Tipo = "T50 Cheio", Mensagem = "Capacidade de armazenamento biométrico atingiu 90%", Hora = "12:15", Gravidade = "media" },
        new() { Id = 3, Ambiente = "Laboratório 2", Tipo = "T50 Cheio", Mensagem = "Capacidade de armazenamento biométrico atingiu 85%", Hora = "09:45", Gravidade = "baixa" },
    ];

    /// <summary>
    /// Últimos acessos negados exibidos na tabela do dashboard.
    /// </summary>
    public static List<AcessoNegado> UltimosNegados =>
    [
        new() { Id = 1, Pessoa = "Carlos Silva", Ambiente = "Sala de Servidores", Motivo = "Biometria não reconhecida", Hora = "15:47", TemGravacao = true },
        new() { Id = 2, Pessoa = "Ana Santos", Ambiente = "Laboratório 3", Motivo = "Acesso não autorizado", Hora = "15:23", TemGravacao = true },
        new() { Id = 3, Pessoa = "Pedro Oliveira", Ambiente = "Almoxarifado", Motivo = "Horário restrito", Hora = "14:58", TemGravacao = false },
        new() { Id = 4, Pessoa = "Maria Costa", Ambiente = "Recepção", Motivo = "Biometria não reconhecida", Hora = "14:32", TemGravacao = true },
        new() { Id = 5, Pessoa = "José Lima", Ambiente = "Diretoria", Motivo = "Acesso não autorizado", Hora = "13:15", TemGravacao = true },
    ];

    /// <summary>
    /// Lista simplificada de ambientes para preencher dropdowns de filtro.
    /// </summary>
    public static List<AmbienteSimples> AmbientesSimples =>
    [
        new() { Id = 1, Nome = "Recepção Principal" },
        new() { Id = 2, Nome = "Sala de Servidores" },
        new() { Id = 3, Nome = "Laboratório 1" },
        new() { Id = 4, Nome = "Laboratório 2" },
        new() { Id = 5, Nome = "Almoxarifado" },
        new() { Id = 6, Nome = "Diretoria" },
    ];
}

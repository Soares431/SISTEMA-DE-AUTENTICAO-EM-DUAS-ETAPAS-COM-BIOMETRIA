namespace Frontend.Data.Services
{
    // Faz backup manual do banco.db a partir do painel (botão em Configurações).
    // Mesma lógica do backup-banco.ps1: copia pra ../../backups/ + rotação automática.
    // SQLite em WAL permite leitura concorrente — backup com sistema ligado pode capturar
    // estado levemente atrás do WAL, mas é aceitável pra snapshot manual.
    public class BackupBancoService
    {
        private readonly string _dbPath;
        private readonly string _backupsDir;
        private const int MaxBackups = 3;

        public BackupBancoService(string dbPath)
        {
            _dbPath = dbPath;
            // backups/ na raiz do repo (mesmo dir que o backup-banco.ps1 usa).
            // dbPath é tipo: <raiz>/Banco/WebAbil8.../banco.db → sobe 3 níveis.
            var bancoDir = Path.GetDirectoryName(_dbPath) ?? "";
            var pastaBanco = Path.GetDirectoryName(bancoDir) ?? "";
            var raiz = Path.GetDirectoryName(pastaBanco) ?? "";
            _backupsDir = Path.Combine(raiz, "backups");
        }

        public BackupResultado Executar()
        {
            if (!File.Exists(_dbPath))
                throw new FileNotFoundException($"Banco não encontrado: {_dbPath}");

            if (!Directory.Exists(_backupsDir))
                Directory.CreateDirectory(_backupsDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
            var destino = Path.Combine(_backupsDir, $"banco-{timestamp}.db");
            int sufixo = 0;
            while (File.Exists(destino))
            {
                sufixo++;
                destino = Path.Combine(_backupsDir, $"banco-{timestamp}-{sufixo}.db");
            }

            File.Copy(_dbPath, destino);
            // File.Copy herda LastWriteTime do source — força "agora" pro novo arquivo
            // não empatar com os antigos (empate confundia a rotação e apagava o recém-criado).
            File.SetLastWriteTime(destino, DateTime.Now);

            // Captura info ANTES da rotação (rotação não pode apagar este; o nome é único,
            // mas mantemos a leitura defensiva caso algum outro processo mexa).
            var info = new FileInfo(destino);
            var nomeArquivo = info.Name;
            var tamanhoMB = Math.Round(info.Length / 1024.0 / 1024.0, 2);

            // Rotação: mantém só os MaxBackups mais recentes, exceto PRE-RESTORE.
            // Ordena pelo NOME (que contém o timestamp YYYY-MM-DD-HHmm) — robusto a empates
            // de LastWriteTime e ao caso do File.Copy herdar mtime do source.
            var todos = new DirectoryInfo(_backupsDir)
                .GetFiles("banco-*.db")
                .Where(f => !f.Name.Contains("PRE-RESTORE"))
                .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                .ToArray();

            int apagados = 0;
            foreach (var antigo in todos.Skip(MaxBackups))
            {
                try { antigo.Delete(); apagados++; }
                catch { /* arquivo em uso ou permissão — ignora */ }
            }

            return new BackupResultado
            {
                NomeArquivo = nomeArquivo,
                TamanhoMB = tamanhoMB,
                BackupsApagados = apagados,
                BackupsAtuais = Math.Min(todos.Length, MaxBackups)
            };
        }
    }

    public class BackupResultado
    {
        public string NomeArquivo { get; set; } = "";
        public double TamanhoMB { get; set; }
        public int BackupsApagados { get; set; }
        public int BackupsAtuais { get; set; }
    }
}

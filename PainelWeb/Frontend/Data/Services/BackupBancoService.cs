namespace Frontend.Data.Services
{

    public class BackupBancoService
    {
        private readonly string _dbPath;
        private readonly string _backupsDir;
        private const int MaxBackups = 3;

        public BackupBancoService(string dbPath)
        {
            _dbPath = dbPath;

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

            File.SetLastWriteTime(destino, DateTime.Now);

            var info = new FileInfo(destino);
            var nomeArquivo = info.Name;
            var tamanhoMB = Math.Round(info.Length / 1024.0 / 1024.0, 2);

            var todos = new DirectoryInfo(_backupsDir)
                .GetFiles("banco-*.db")
                .Where(f => !f.Name.Contains("PRE-RESTORE"))
                .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                .ToArray();

            int apagados = 0;
            foreach (var antigo in todos.Skip(MaxBackups))
            {
                try { antigo.Delete(); apagados++; }
                catch {  }
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


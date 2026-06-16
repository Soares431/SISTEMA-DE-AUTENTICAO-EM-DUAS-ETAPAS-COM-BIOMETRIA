using Anviz.SDK.Responses;
using Anviz.SDK.Utils;

namespace BiometricAcess.Worker.Services
{
    public class AnvizService : IAnvizService
    {
        private readonly AnvizConnector _connector;

        public AnvizService(AnvizConnector connector)
        {
            _connector = connector;
        }

        public bool AdicionarPessoa(int id, string nome, string senha)
        {
            var device = _connector.Device;
            if (device == null) { Console.WriteLine("AnvizService.AdicionarPessoa: dispositivo ainda não conectado."); return false; }
            try
            {
                var userInfo = new UserInfo((ulong)id, nome);
                // ATENÇÃO: O T50M usa formato especial de 3 bytes para senha.
                // O SDK .NET (Anviz.SDK NuGet) pode ou não fazer essa conversão internamente.
                // Formato nativo: bits 23-20 = comprimento da senha, bits 19-0 = valor numérico.
                // Se a autenticação por senha falhar com hardware real, investigar aqui primeiro.
                userInfo.Password = ulong.Parse(senha);
                device.SetEmployeesData(userInfo).Wait();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao adicionar pessoa: {ex.Message}");
                return false;
            }
        }

        public bool RemoverPessoa(int id)
        {
            var device = _connector.Device;
            if (device == null) { Console.WriteLine("AnvizService.RemoverPessoa: dispositivo ainda não conectado."); return false; }
            try
            {
                device.DeleteEmployeesData((ulong)id).Wait();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao remover pessoa: {ex.Message}");
                return false;
            }
        }

        public bool UploadTemplate(int id, byte[] template)
        {
            var device = _connector.Device;
            if (device == null) { Console.WriteLine("AnvizService.UploadTemplate: dispositivo ainda não conectado."); return false; }
            try
            {
                device.SetFingerprintTemplate((ulong)id, Finger.RightIndex, template).Wait();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer upload de template: {ex.Message}");
                return false;
            }
        }

        public byte[]? DownloadTemplate(int id)
        {
            var device = _connector.Device;
            if (device == null) { Console.WriteLine("AnvizService.DownloadTemplate: dispositivo ainda não conectado."); return null; }
            try
            {
                var result = device.GetFingerprintTemplate((ulong)id, Finger.RightIndex).Result;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer download de template: {ex.Message}");
                return null;
            }
        }

        public byte[]? IniciarCapturaDigital(int id)
        {
            var device = _connector.Device;
            if (device == null) { Console.WriteLine("AnvizService.IniciarCapturaDigital: dispositivo ainda não conectado."); return null; }
            try
            {
                // EnrollFingerprint bloqueia até o usuário colocar o dedo 2x (verifyCount=2) e retorna o template diretamente
                return device.EnrollFingerprint((ulong)id).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar captura de digital: {ex.Message}");
                return null;
            }
        }

        public bool AlterarModo(int id, string modo)
        {
            var device = _connector.Device;
            if (device == null) { Console.WriteLine("AnvizService.AlterarModo: dispositivo ainda não conectado."); return false; }
            try
            {
                var userInfo = new UserInfo((ulong)id, string.Empty);
                // Mode (Anviz UserInfo): bitmask de métodos aceitos. Doc oficial T-series:
                //   Mode 2 = Fingerprint
                //   Mode 4 = Password
                //   Mode 6 = Fingerprint OR Password (2|4) → atende doc §2.2 "duas opções
                //            disponíveis, usuário escolhe na hora"
                // Bug 4: após enroll usa Mode=6 (não Mode=2) pra permitir senha+ID também.
                if (modo == "digital_id" || modo == "ambos")
                {
                    userInfo.Mode = 6;
                }
                else
                {
                    userInfo.Mode = 4;
                }
                device.SetEmployeesData(userInfo).Wait();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao alterar modo: {ex.Message}");
                return false;
            }
        }

        public bool SincronizarHora()
        {
            var device = _connector.Device;
            if (device == null) { Console.WriteLine("AnvizService.SincronizarHora: dispositivo ainda não conectado."); return false; }
            try
            {
                device.SetDateTime(DateTime.Now).Wait();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao sincronizar hora: {ex.Message}");
                return false;
            }
        }
    }
}

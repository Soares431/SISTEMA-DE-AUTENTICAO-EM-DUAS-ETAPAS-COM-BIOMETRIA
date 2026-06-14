using Anviz.SDK;
using Anviz.SDK.Responses;
using Anviz.SDK.Utils;

namespace BiometricAcess.Worker.Services
{
    public class AnvizService : IAnvizService
    {
        private readonly AnvizDevice _device;

        public AnvizService(AnvizDevice device)
        {
            _device = device;
        }

        public bool AdicionarPessoa(int id, string nome, string senha)
        {
            try
            {
                var userInfo = new UserInfo((ulong)id, nome);
                // DEPOIS
                // ATENÇÃO: O T50M usa formato especial de 3 bytes para senha.
                // O SDK .NET (Anviz.SDK NuGet) pode ou não fazer essa conversão internamente.
                // Formato nativo: bits 23-20 = comprimento da senha, bits 19-0 = valor numérico.
                // Se a autenticação por senha falhar com hardware real, investigar aqui primeiro.
                userInfo.Password = ulong.Parse(senha);
                _device.SetEmployeesData(userInfo).Wait();
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
            try
            {
                _device.DeleteEmployeesData((ulong)id).Wait();
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
            try
            {
                _device.SetFingerprintTemplate((ulong)id, Finger.RightIndex, template).Wait();
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
            try
            {
                var result = _device.GetFingerprintTemplate((ulong)id, Finger.RightIndex).Result;
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
            try
            {
                // EnrollFingerprint bloqueia até o usuário colocar o dedo 2x (verifyCount=2) e retorna o template diretamente
                return _device.EnrollFingerprint((ulong)id).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar captura de digital: {ex.Message}");
                return null;
            }
        }

        public bool AlterarModo(int id, string modo)
        {
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
                _device.SetEmployeesData(userInfo).Wait();
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
            try
            {
                _device.SetDateTime(DateTime.Now).Wait();
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
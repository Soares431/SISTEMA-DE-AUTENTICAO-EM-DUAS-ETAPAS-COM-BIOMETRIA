using Microsoft.AspNetCore.Components.Server.Circuits;

namespace FrontendControleAcesso.Services;

public class AuthCircuitHandler : CircuitHandler
{
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Limpa a sessão quando o navegador fecha ou desconecta
        TokenStore.Token = null;
        TokenStore.AdminId = 0;
        TokenStore.NomeCompleto = "";
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}
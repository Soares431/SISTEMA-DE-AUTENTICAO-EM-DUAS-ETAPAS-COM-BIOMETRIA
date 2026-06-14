using Microsoft.AspNetCore.Components.Server.Circuits;

namespace FrontendControleAcesso.Services;

public class AuthCircuitHandler : CircuitHandler
{
    private readonly ITokenStore _tokenStore;

    public AuthCircuitHandler(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {

        _tokenStore.Token = null;
        _tokenStore.AdminId = 0;
        _tokenStore.NomeCompleto = "";
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}


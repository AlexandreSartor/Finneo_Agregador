using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CryptoController : ControllerBase
{
    private readonly CryptoService _cryptoService;

    public CryptoController(CryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    [HttpGet("{enderecoCarteira}")]
    public async Task<IActionResult> ObterTokens(string enderecoCarteira)
    {
        var tokens = await _cryptoService.ObterTokensMoralis(enderecoCarteira);
        return Ok(tokens);
    }
}

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

  

    // GET api/crypto/total/{enderecoCarteira}
    [HttpGet("total/{enderecoCarteira}")]
    public async Task<IActionResult> ObterTotalCarteira(string enderecoCarteira)
    {
        if (string.IsNullOrEmpty(enderecoCarteira))
            return BadRequest("Endereço da carteira é obrigatório.");

        try
        {
            decimal totalCarteira = await _cryptoService.ObterTotalCarteiraBr(enderecoCarteira);
            return Ok(new { Total_carteira = totalCarteira });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { erro = ex.Message });
        }
    }


    // GET: api/crypto/Resumo?enderecoCarteira=0x...
    [HttpGet("Resumo")]
    public async Task<IActionResult> ObterResumoCarteira([FromQuery] string enderecoCarteira)
    {
        if (string.IsNullOrWhiteSpace(enderecoCarteira))
            return BadRequest("O endereço da carteira é obrigatório.");

        // Busca todos os tokens do usuário
        var tokens = await _cryptoService.ObterTokensMoralis(enderecoCarteira);

        // Agrupa por símbolo + preço
        var resumo = _cryptoService.AgruparTokensPorSimboloEPreco(tokens);

        // Calcula o total da carteira em BRL
        decimal totalCarteiraBr = 0;
        foreach (var t in resumo)
            totalCarteiraBr += t.TotalEquivalente;

        // Adiciona o total geral no objeto de resposta
        var resposta = new
        {
            TotalCarteiraBr = totalCarteiraBr,
            Tokens = resumo
        };

        return Ok(resposta);
    }
}





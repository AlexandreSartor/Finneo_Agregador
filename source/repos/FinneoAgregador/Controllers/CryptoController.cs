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

    [HttpGet()]
    public async Task<IActionResult> ObterTokens()
    {

        var enderecoCarteira = _cryptoService.ObterCarteiraDoUsuario(); // OK

        if (string.IsNullOrWhiteSpace(enderecoCarteira))

            return BadRequest("Endereço da carteira não definido. Use o POST para definir.");
        var tokens = await _cryptoService.ObterTokensMoralis(enderecoCarteira);
        return Ok(tokens);
    }



    [HttpGet("total")]
    public async Task<IActionResult> ObterTotalCarteira()
    {
        var enderecoCarteira = _cryptoService.ObterCarteiraDoUsuario(); // OK
        if (string.IsNullOrWhiteSpace(enderecoCarteira))
            return BadRequest("Endereço da carteira não definido. Use o POST para definir.");

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




    [HttpGet("Resumo")]
    public async Task<IActionResult> ObterResumoCarteira()
    {
        var enderecoCarteira = _cryptoService.ObterCarteiraDoUsuario();

        if (string.IsNullOrWhiteSpace(enderecoCarteira))
            return BadRequest("Endereço da carteira não definido. Use o POST para definir.");

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


    // POST api/crypto/carteira
    [HttpPost("carteira")]
    public IActionResult DefinirCarteira([FromBody] string carteira)
    {
        if (string.IsNullOrWhiteSpace(carteira))
            return BadRequest("Carteira inválida.");

        _cryptoService.DefinirCarteira(carteira);
        return Ok(new { mensagem = "Carteira definida com sucesso!" });
    }
}




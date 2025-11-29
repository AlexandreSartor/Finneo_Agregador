// Services/CryptoService.cs
using System;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

public class CryptoService
{
    private readonly HttpClient _httpClient;
    private readonly string _moralisApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJub25jZSI6ImIzOWI2YTgxLTAyZTQtNGJlZC1iYzQzLWIyN2EzMjk3NzAzMCIsIm9yZ0lkIjoiNDgzMjEyIiwidXNlcklkIjoiNDk3MTM1IiwidHlwZUlkIjoiOGMxYzBlMzUtNWEyOC00MmM1LTgzZmYtZDczMThlYmM5N2ZiIiwidHlwZSI6IlBST0pFQ1QiLCJpYXQiOjE3NjQxMDUyMzIsImV4cCI6NDkxOTg2NTIzMn0.kzdbGSqQJk-Jv4AnaS4yNMSk5KkgcOEzditJVrrbxMo";
    public CryptoService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Adiciona o header apenas se ainda não existir
        if (!_httpClient.DefaultRequestHeaders.Contains("X-API-Key"))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _moralisApiKey);
        }
    }

    private async Task<decimal> ObterCotacaoDolarParaReal()
    {
        var response = await _httpClient.GetAsync("https://api.frankfurter.app/latest?from=USD&to=BRL");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("rates", out var rates) &&
            rates.TryGetProperty("BRL", out var brl))
        {
            return brl.GetDecimal();
        }

        // fallback caso não exista a cotação
        return 1m;
    }

    public async Task<List<TokenMoralis>> ObterTokensMoralis(string enderecoCarteira)
    {
        string[] redes = new string[]
        {
            "eth"
        };

        decimal cotacao = await ObterCotacaoDolarParaReal();
        var resultado = new List<TokenMoralis>();

        foreach (var rede in redes)
        {
            var url = $"https://deep-index.moralis.io/api/v2.2/wallets/{enderecoCarteira}/tokens?chain={rede}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) continue;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("result", out var tokens)) continue;

            foreach (var token in tokens.EnumerateArray())
            {
                string balanceStr = token.GetProperty("balance").GetString();
                if (!decimal.TryParse(balanceStr, out var balance)) continue;

                int decimals = token.GetProperty("decimals").GetInt32();
                decimal precoUsd = token.GetProperty("usd_price").GetDecimal();

                if (precoUsd == 0) continue;

                decimal SaldoFormatado = balance / (decimal)Math.Pow(10, decimals);



                decimal Truncar4(decimal valor) => Math.Floor(valor * 10000) / 10000;

                resultado.Add(new TokenMoralis
                {
                    Simbolo = token.GetProperty("symbol").GetString(),
                    Saldo = Truncar4(SaldoFormatado),
                    PrecoUsd = Truncar4(precoUsd),
                    PrecoBr = Truncar4(precoUsd * cotacao),
                    ValorTotalUsd = Truncar4(SaldoFormatado * precoUsd),
                    ValorTotalBr = Truncar4(SaldoFormatado * precoUsd * cotacao),
                    PercentualPortfolio = Truncar4(token.GetProperty("portfolio_percentage").GetDecimal()),
                    Mudanca24hPercent = Truncar4(token.GetProperty("usd_price_24hr_percent_change").GetDecimal()),
                    Mudanca24hUsd = Truncar4(token.GetProperty("usd_price_24hr_usd_change").GetDecimal()),
                    Mudanca24hBr = Truncar4(token.GetProperty("usd_price_24hr_usd_change").GetDecimal() * cotacao)
                });
            }
        }

        return resultado;
    }
}



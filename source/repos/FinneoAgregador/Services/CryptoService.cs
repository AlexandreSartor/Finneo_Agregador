
using System;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using FinneoAgregador.Models;


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
                    "eth","arbitrum","avalanche","optimism","polygon",


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
                    Mudanca24hBr = Truncar4(token.GetProperty("usd_price_24hr_usd_change").GetDecimal() * cotacao),
                    Rede = rede

                });
            }
        }

        return resultado;
    }

    public async Task<decimal> ObterTotalCarteiraBr(string enderecoCarteira)
    {
        string[] redes = new string[]
        {
         "bsc","eth","arbitrum","avalanche","optimism","polygon",
             "mantle","base","fantom","zetachain","linea","arbitrum-nova",
             "zksync","mode","opbnb","blast","manta","scroll","gnosis",
             "celo","moonbeam","kaia","conflux","zora","shibarium","hemi"
        };

        decimal cotacao = await ObterCotacaoDolarParaReal();
        decimal totalBr = 0m;

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

                decimal saldoFormatado = balance / (decimal)Math.Pow(10, decimals);

                totalBr += saldoFormatado * precoUsd * cotacao;
            }
        }

        // Mantém duas casas decimais
        return Math.Round(totalBr, 2);
    }




    public List<TokenResumo> AgruparTokensPorSimboloEPreco(List<TokenMoralis> tokens, decimal toleranciaPercentual = 1m)
    {
        var resumo = new List<TokenResumo>();

        foreach (var token in tokens)
        {
            // tenta achar um TokenResumo existente com mesmo símbolo e preço dentro da tolerância percentual
            var tokenResumo = resumo.FirstOrDefault(t =>
                t.Simbolo == token.Simbolo &&
                Math.Abs(t.Cotacao - token.PrecoBr) / token.PrecoBr * 100m <= toleranciaPercentual);

            if (tokenResumo == null)
            {
                // cria novo resumo
                tokenResumo = new TokenResumo
                {
                    Simbolo = token.Simbolo,
                    TotalEquivalente = 0m,
                    Cotacao = token.PrecoBr,
                    ValoresPorRedeBr = new Dictionary<string, decimal>(),
                    PercentualEquivalente = new Dictionary<string, decimal>()
                };
                resumo.Add(tokenResumo);
            }

            // soma o total equivalente em BRL
            tokenResumo.TotalEquivalente += token.ValorTotalBr;

            // soma valores por rede
            if (!tokenResumo.ValoresPorRedeBr.ContainsKey(token.Rede))
                tokenResumo.ValoresPorRedeBr[token.Rede] = token.ValorTotalBr;
            else
                tokenResumo.ValoresPorRedeBr[token.Rede] += token.ValorTotalBr;
        }

        // ---------- Calcula Percentual por Rede ----------
        foreach (var tokenResumo in resumo)
        {
            tokenResumo.PercentualEquivalente = new Dictionary<string, decimal>();
            foreach (var rede in tokenResumo.ValoresPorRedeBr.Keys)
            {
                decimal total = tokenResumo.TotalEquivalente;
                decimal percentual = total != 0 ? (tokenResumo.ValoresPorRedeBr[rede] / total) * 100m : 0m;
                tokenResumo.PercentualEquivalente[rede] = Math.Floor(percentual * 10000) / 10000;
            }
        }

        // ---------- Calcula PercentualTotal da crypto na carteira ----------
        decimal totalCarteiraBr = resumo.Sum(t => t.TotalEquivalente);
        foreach (var tokenResumo in resumo)
        {
            decimal percentual = totalCarteiraBr > 0 ? (tokenResumo.TotalEquivalente / totalCarteiraBr) * 100m : 0m;
            tokenResumo.PercentualTotal = Math.Floor(percentual * 10000) / 10000;
        }

        return resumo;
    }
}

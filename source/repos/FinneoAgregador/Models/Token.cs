public class TokenMoralis
{
    public string Simbolo { get; set; }
    public decimal Saldo { get; set; }
    public decimal PrecoUsd { get; set; }
    public decimal PrecoBr { get; set; } // Novo
    public decimal ValorTotalUsd { get; set; }
    public decimal ValorTotalBr { get; set; } // Novo
    public decimal PercentualPortfolio { get; set; }
    public decimal Mudanca24hPercent { get; set; }
    public decimal Mudanca24hUsd { get; set; }
    public decimal Mudanca24hBr { get; set; } // Novo
    public string Rede { get; set; }

    public decimal ValoremCarteira { get; set; }  // total de todas as redes para esse símbolo
}

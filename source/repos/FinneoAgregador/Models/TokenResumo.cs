namespace FinneoAgregador.Models
{
    public class TokenResumo
    {


            public string Simbolo { get; set; }
            public decimal TotalEquivalente { get; set; }

            public decimal Cotacao { get; set; }

            public Dictionary<string, decimal> ValoresPorRedeBr { get; set; } = new();
            public Dictionary<string, decimal> PercentualEquivalente { get; set; } = new();
            public decimal PercentualTotal { get; set; }


        }
    }


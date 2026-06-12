namespace SAV.Models
{
    public class DadosMapa
    {
        public List<Via> elements { get; set; }
    }

    public class Via
    {
        public long id { get; set; }
        public List<Ponto> geometry { get; set; }
        public Dictionary<string, string> tags { get; set; }
        public string StatusSimulacao { get; set; } = "Normal";
        public string CorSimulacao
        {
            get
            {
                switch (StatusSimulacao)
                {
                    case "Bloqueado": return "#2C2C2E";     // Preto (Estrada cortada / Obra / Acidente Grave)
                    case "Caótico": return "#FF2D55";       // Vermelho Escuro (Congestionamento extremo)
                    case "Lento": return "#FFCC00";         // Amarelo (Trânsito moderado / Chuva)
                    default: return "#4CD964";               // Verde (Fluxo Normal)
                }
            }
        }
    }

    public class Ponto
    {
        public double lat { get; set; }
        public double lon { get; set; }
    }
}
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
    }

    public class Ponto
    {
        public double lat { get; set; }
        public double lon { get; set; }
    }
}
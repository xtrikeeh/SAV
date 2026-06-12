using SAV.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using SAV.Views;

namespace SAV.ViewModels
{
    public class MainWindowViewModel
    {
        public Configuracao Config { get; set; }

        public MainWindowViewModel()
        {
            Config = CarregarConfiguracoes();

            if (Config != null)
            {
                AplicarCores(Config.TemaEscuro);
            }
        }

        private Configuracao CarregarConfiguracoes()
        {
            string caminho = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configuracoes.json");
            if (File.Exists(caminho))
            {
                string jsonString = File.ReadAllText(caminho);
                return JsonSerializer.Deserialize<Configuracao>(jsonString);
            }
            return null;
        }

        public void AplicarCores(bool escuro)
        {
            var conversorBrush = new BrushConverter();

            if (escuro)
            {
                Application.Current.Resources["corDestaquePrimaria"] = (Brush)conversorBrush.ConvertFrom("#1B1B1B")!;
                Application.Current.Resources["corDestaqueSecundaria"] = (Brush)conversorBrush.ConvertFrom("#F5F5F5")!;
                Application.Current.Resources["corDestaqueTerciaria"] = (Brush)conversorBrush.ConvertFrom("#292929")!;
                Application.Current.Resources["corFonte"] = (Brush)conversorBrush.ConvertFrom("#F5F5F5")!;
            }
            else
            {
                Application.Current.Resources["corDestaquePrimaria"] = (Brush)conversorBrush.ConvertFrom("#F5F5F5")!;
                Application.Current.Resources["corDestaqueSecundaria"] = (Brush)conversorBrush.ConvertFrom("#1B1B1B")!;
                Application.Current.Resources["corDestaqueTerciaria"] = (Brush)conversorBrush.ConvertFrom("#D4D4D4")!;
                Application.Current.Resources["corFonte"] = (Brush)conversorBrush.ConvertFrom("#1C2639")!;
            }
        }

        public async Task<DadosMapa> AbrirMapaExistente(string caminhoArquivo)
        {
            try
            {
                string json = await File.ReadAllTextAsync(caminhoArquivo);
                return JsonSerializer.Deserialize<DadosMapa>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao ler arquivo: " + ex.Message);
                return null;
            }
        }

        public async Task<DadosMapa> CriarNovoMapa(string area, string nomeArquivoASalvar)
        {
            if (string.IsNullOrEmpty(area)) return null;

            string query = $"[out:json][timeout:90];way({area})[highway];out geom;";
            string url = "https://overpass-api.de/api/interpreter?data=" + Uri.EscapeDataString(query);

            using (HttpClient cliente = new HttpClient())
            {
                cliente.DefaultRequestHeaders.Add("User-Agent", "SAV-App/1.0 (contato@seusistema.com)");

                try
                {
                    string busca = await cliente.GetStringAsync(url);

                    var opcoes = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var dados = JsonSerializer.Deserialize<DadosMapa>(busca, opcoes);

                    if (dados == null || dados.elements == null || dados.elements.Count == 0)
                    {
                        MessageBox.Show("A API não encontrou nenhuma rua nessa área selecionada.", "SAV");
                        return null;
                    }

                    string jsonFinal = JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(nomeArquivoASalvar, jsonFinal);

                    return dados;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao baixar os dados do servidor de mapas: " + ex.Message,
                                    "Erro de Conexão", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
        }

        public Point ConverterParaPixel(double lat, double lon, double latBase, double lonBase, double zoom)
        {
            double x = (lon - lonBase) * zoom;
            double y = (latBase - lat) * zoom;
            return new Point(x, y);
        }
        public async Task<bool> SalvarMapaEmDisco(string caminhoArquivo, DadosMapa dados)
        {
            try
            {
                var opcoes = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IgnoreNullValues = true
                };

                string jsonString = JsonSerializer.Serialize(dados, opcoes);
                await File.WriteAllTextAsync(caminhoArquivo, jsonString);
                return true;
            }
            catch
            {
                throw;
            }
        }

        public Point ConverterParaGeo(double pixelX, double pixelY, double latRef, double lonRef, double zoom)
        {
            double lon = (pixelX / zoom) + lonRef;
            double lat = latRef - (pixelY / zoom);
            return new Point(lat, lon);
        }
    }
}
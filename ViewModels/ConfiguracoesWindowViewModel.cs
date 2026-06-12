using SAV.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace SAV.ViewModels
{
    public class ConfiguracaoWindowViewModel : INotifyPropertyChanged
    {
        private Configuracao Config;
        private readonly string _caminhoArquivoConfig;

        public string TemaEscolhido => Config.TemaEscuro ? "Escuro" : "Claro";
        public string SalvamentoAutomaticoEscolhido =>
            Config.SalvamentoAutomatico == 0
            ? "Nunca" : $"{Config.SalvamentoAutomatico} minutos";

        public ConfiguracaoWindowViewModel()
        {
            string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string pastaSav = Path.Combine(pastaDocumentos, "SAV");

            if (!Directory.Exists(pastaSav))
            {
                Directory.CreateDirectory(pastaSav);
            }

            _caminhoArquivoConfig = Path.Combine(pastaSav, "configuracoes.json");

            var Inicializador = CarregarConfiguracoes();

            if (Inicializador != null)
            {
                Config = Inicializador;
            }
            else
            {
                string pastaProjetos = Path.Combine(pastaSav, "Projetos");

                if (!Directory.Exists(pastaProjetos))
                {
                    Directory.CreateDirectory(pastaProjetos);
                }

                Config = new Configuracao
                {
                    TemaEscuro = false,
                    SalvamentoAutomatico = 5,
                    DiretorioPadrao = pastaProjetos
                };

                SalvarConfiguracoes();
            }

            AplicarCores(Config.TemaEscuro);
        }

        public bool TemaEscuro
        {
            get => Config.TemaEscuro;
            set
            {
                Config.TemaEscuro = value;
                AplicarCores(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(TemaEscolhido));
                SalvarConfiguracoes();
            }
        }

        public string DiretorioPadrao
        {
            get => Config.DiretorioPadrao;
            set
            {
                Config.DiretorioPadrao = value;
                OnPropertyChanged();
                SalvarConfiguracoes();
            }
        }

        public int SalvamentoAutomatico
        {
            get => Config.SalvamentoAutomatico;
            set
            {
                Config.SalvamentoAutomatico = value;
                OnPropertyChanged(nameof(SalvamentoAutomatico));
                OnPropertyChanged(nameof(SalvamentoAutomaticoEscolhido));
                SalvarConfiguracoes();
            }
        }

        private void SalvarConfiguracoes()
        {
            try
            {
                var formatacao = new JsonSerializerOptions { WriteIndented = true };
                var dados = JsonSerializer.Serialize(Config, formatacao);
                File.WriteAllText(_caminhoArquivoConfig, dados);
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro);
            }
        }

        private Configuracao CarregarConfiguracoes()
        {
            try
            {
                if (File.Exists(_caminhoArquivoConfig))
                {
                    string jsonString = File.ReadAllText(_caminhoArquivoConfig);
                    return JsonSerializer.Deserialize<Configuracao>(jsonString);
                }
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro);
            }
            return null;
        }

        private void AplicarCores(bool escuro)
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

        public void AlternarTema()
        {
            TemaEscuro = !TemaEscuro;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
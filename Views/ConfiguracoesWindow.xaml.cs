using Microsoft.Win32;
using SAV.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SAV.Views
{
    public partial class ConfiguracoesWindow : Window
    {
        public ConfiguracaoWindowViewModel ViewModel => (ConfiguracaoWindowViewModel)this.DataContext;
        public ConfiguracoesWindow()
        {
            InitializeComponent();
            this.DataContext = new ConfiguracaoWindowViewModel();
        }

        private void BotaoTrocarTema_Click(object sender, RoutedEventArgs e)
        {

            if (botaoTrocarTema.Content == "Claro")
            {
                ViewModel.AlternarTema();
                botaoTrocarTema.Content = "Escuro";
            } else
            {
                ViewModel.AlternarTema();
                botaoTrocarTema.Content = "Claro";
            }
        }

        private void BotaoSalvamentoAutomatico_Click(object sender, EventArgs e)
        {
            Button botaoSalvamento = sender as Button;
            botaoSalvamento.ContextMenu.IsEnabled = true;
            botaoSalvamento.ContextMenu.PlacementTarget = botaoSalvamento;
            botaoSalvamento.ContextMenu.IsOpen = true;
        }

        private void OpcaoSalvamento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                string valorTag = item.Tag.ToString();

                if (int.TryParse(valorTag, out int minutos))
                {
                    ViewModel.SalvamentoAutomatico = minutos;
                }
                else
                {
                    ViewModel.SalvamentoAutomatico = 0;
                }
            }
        }

        private void BotaoEscolherDiretorio_Click(object sender, EventArgs e)
        {
            OpenFolderDialog selecionarPasta = new OpenFolderDialog();
            selecionarPasta.Title = "Selecione o diretório padrão";

            if (selecionarPasta.ShowDialog() == true)
            {
                ((ConfiguracaoWindowViewModel)this.DataContext).DiretorioPadrao = selecionarPasta.FolderName;

                textoDiretorio.Text = selecionarPasta.FolderName;
            }
        }
    }
}

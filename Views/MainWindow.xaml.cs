using Microsoft.Win32;
using SAV.Models;
using SAV.ViewModels;
using SAV.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SAV
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

        private enum ModoFerramenta { Editar, Adicionar, Deletar }
        private ModoFerramenta modoAtual = ModoFerramenta.Editar;

        private List<FrameworkElement> ruasSelecionadas = new List<FrameworkElement>();

        private Via viaSelecionadaAtual = null;

        private bool criandoRua = false;
        private FrameworkElement noOrigemSelecionado = null;
        private System.Windows.Shapes.Line linhaGuiaTemporaria = null;

        private double limiteMinX = 0;
        private double limiteMaxX = 0;
        private double limiteMinY = 0;
        private double limiteMaxY = 0;

        private Point pontoInicialMouse;
        private double deslocamentoInicialX;
        private double deslocamentoInicialY;
        private bool mouseMovimento = false;

        private int nivelZoomAtual = 0;
        private readonly double[] niveisDeEscala = { 0.05, 0.10, 0.25, 0.50, 1.0 };

        private SolidColorBrush CorTemaPrimaria => (SolidColorBrush)Application.Current.FindResource("corTemaPrimaria");
        private SolidColorBrush CorTemaTerciaria => (SolidColorBrush)Application.Current.FindResource("corTemaTerciaria");
        private SolidColorBrush CorTemaSecundaria => (SolidColorBrush)Application.Current.FindResource("corTemaSecundaria");
        private SolidColorBrush VermelhoSemantico => (SolidColorBrush)Application.Current.FindResource("vermelhoSemantico");
        private SolidColorBrush AmareloSemantico => (SolidColorBrush)Application.Current.FindResource("amareloSemantico");
        private SolidColorBrush VerdeSemantico => (SolidColorBrush)Application.Current.FindResource("verdeSemantico");

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();

            double zoomInicial = niveisDeEscala[nivelZoomAtual];
            TransformarZoom.ScaleX = zoomInicial;
            TransformarZoom.ScaleY = zoomInicial;

            AtualizarTextoZoom();
        }

        private void Ferramenta_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null) return;

            LimparSelecoes();

            switch (rb.Name)
            {
                case "botaoEditar":
                    modoAtual = ModoFerramenta.Editar;
                    if (CanvasMapa != null) CanvasMapa.Cursor = Cursors.Arrow;
                    EsconderNosDasRuas();
                    break;

                case "botaoAdicionar":
                    modoAtual = ModoFerramenta.Adicionar;
                    if (CanvasMapa != null) CanvasMapa.Cursor = Cursors.Cross;
                    MostrarNosDasRuas();
                    break;

                case "botaoDeletar":
                    modoAtual = ModoFerramenta.Deletar;
                    if (CanvasMapa != null) CanvasMapa.Cursor = Cursors.Hand;
                    EsconderNosDasRuas();
                    break;
            }
        }

        private void InteragirComElementoDoMapa(FrameworkElement elemento)
        {
            if (elemento == null) return;

            if (modoAtual == ModoFerramenta.Editar)
            {
                if (elemento.Tag?.ToString() == "Rua")
                {
                    LimparSelecoes();
                    ruasSelecionadas.Add(elemento);
                    AplicarEfeitoSelecao(elemento, true);
                    // TODO: Abrir o painel lateral de edição da via
                }
            }
            else if (modoAtual == ModoFerramenta.Adicionar)
            {
                if (elemento.Tag?.ToString() == "NoExtremidade")
                {
                    if (!criandoRua)
                    {
                        IniciarCriacaoDeRuaPartindoDe(elemento);
                    }
                    else
                    {
                        FinalizarCriacaoDeRuaEm(elemento);
                    }
                }
            }
            else if (modoAtual == ModoFerramenta.Deletar)
            {
                if (elemento.Tag?.ToString() == "Rua")
                {
                    if (ruasSelecionadas.Contains(elemento))
                    {
                        ruasSelecionadas.Remove(elemento);
                        AplicarEfeitoSelecao(elemento, false);
                    }
                    else
                    {
                        ruasSelecionadas.Add(elemento);
                        AplicarEfeitoSelecao(elemento, true);
                    }

                    GerenciarBotaoExcluir();
                }
            }
        }

        private void GerenciarBotaoExcluir()
        {
            Button btnExcluir = this.FindName("btnExcluir") as Button;
            if (btnExcluir == null) return;

            if (ruasSelecionadas.Count > 0)
            {
                btnExcluir.Visibility = Visibility.Visible;
                btnExcluir.Content = $"Excluir ({ruasSelecionadas.Count}) Vias";
            }
            else
            {
                btnExcluir.Visibility = Visibility.Collapsed;
            }
        }

        private void ExecutarExclusao_Click(object sender, RoutedEventArgs e)
        {
            int quantidade = ruasSelecionadas.Count;
            if (quantidade == 0) return;

            MessageBoxResult resultado = MessageBox.Show(
                $"Você tem certeza que deseja excluir as {quantidade} ruas selecionadas?\n",
                "Confirmar Exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (resultado == MessageBoxResult.Yes)
            {
                foreach (var rua in ruasSelecionadas)
                {
                    if (CanvasMapa.Children.Contains(rua))
                    {
                        CanvasMapa.Children.Remove(rua);
                    }
                }

                LimparSelecoes();
                MessageBox.Show($"{quantidade} vias removidas com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LimparSelecoes()
        { 
            foreach (var elemento in ruasSelecionadas)
            {
                AplicarEfeitoSelecao(elemento, false);
            }

            ruasSelecionadas.Clear();
            viaSelecionadaAtual = null;

            GerenciarBotaoExcluir();

            TxtAvisoSemSelecao?.SetValue(VisibilityProperty, Visibility.Visible);
            ScrollPropriedadesVia?.SetValue(VisibilityProperty, Visibility.Collapsed);
            GridBotoesAcoes?.SetValue(VisibilityProperty, Visibility.Collapsed);
            PainelDadosRua?.SetValue(VisibilityProperty, Visibility.Collapsed);
        }

        private void SelecionarRuaParaEdicao(System.Windows.Shapes.Polyline linhaWpf)
        {
            if (ruasSelecionadas.Contains(linhaWpf))
            {
                LimparSelecoes();
                return;
            }

            foreach (var antiga in ruasSelecionadas)
            {
                AplicarEfeitoSelecao(antiga, false);
            }

            ruasSelecionadas.Clear();

            ruasSelecionadas.Add(linhaWpf);
            AplicarEfeitoSelecao(linhaWpf, true);

            if (linhaWpf.DataContext is Via dadosDaVia)
            {
                viaSelecionadaAtual = dadosDaVia;

                PreencherCamposInterface(dadosDaVia);

                TxtAvisoSemSelecao?.SetValue(VisibilityProperty, Visibility.Collapsed);
                ScrollPropriedadesVia?.SetValue(VisibilityProperty, Visibility.Visible);
                GridBotoesAcoes?.SetValue(VisibilityProperty, Visibility.Visible);
                PainelDadosRua?.SetValue(VisibilityProperty, Visibility.Visible);
            }
        }

        private void BtnSalvarDadosRua_Click(object sender, RoutedEventArgs e)
        {
            if (viaSelecionadaAtual == null) return;

            if (viaSelecionadaAtual.tags == null) viaSelecionadaAtual.tags = new Dictionary<string, string>();
            viaSelecionadaAtual.tags["name"] = TxtNomeRua.Text;

            if (LstVelocidadeRua.SelectedItem is ListBoxItem itemVelocidade && !string.IsNullOrEmpty(itemVelocidade.Tag?.ToString()))
            {
                viaSelecionadaAtual.tags["maxspeed"] = itemVelocidade.Tag.ToString();
            }
            else
            {
                viaSelecionadaAtual.tags.Remove("maxspeed");
            }

            if (LstDirecaoRua.SelectedItem is ListBoxItem itemDirecao)
            {
                viaSelecionadaAtual.tags["oneway"] = itemDirecao.Tag.ToString();
            }

            MessageBox.Show("Alterações salvas com sucesso!", "SAV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void AplicarEfeitoSelecao(FrameworkElement elemento, bool selecionado)
        {
            var shape = elemento as System.Windows.Shapes.Shape;
            if (shape == null) return;

            if (selecionado)
            {
                if (modoAtual == ModoFerramenta.Deletar)
                {
                    shape.Stroke = VermelhoSemantico;
                }
                else
                {
                    shape.Stroke = CorTemaTerciaria;
                }
            }
            else
            {
                shape.Stroke = CorTemaPrimaria;
                shape.Effect = null;
            }
        }

        private void MostrarNosDasRuas()
        {
            if (CanvasMapa == null) return;

            var ruasAtuais = CanvasMapa.Children.OfType<System.Windows.Shapes.Polyline>().ToList();
            EsconderNosDasRuas();

            HashSet<string> nosDesenhados = new HashSet<string>();

            foreach (var child in ruasAtuais)
            {
                if (child != null && child.Points != null && child.Tag?.ToString() == "Rua" && child.Points.Count > 1)
                {
                    Point inicio = child.Points.First();
                    Point fim = child.Points.Last();

                    CriarNoVisualNoCanvas(inicio, nosDesenhados);
                    CriarNoVisualNoCanvas(fim, nosDesenhados);
                }
            }
        }

        private void EsconderNosDasRuas()
        {
            if (CanvasMapa == null) return;

            CancelarCriacaoRua();

            var nos = CanvasMapa.Children.OfType<System.Windows.Shapes.Ellipse>()
                                         .Where(e => e.Tag?.ToString() == "NoExtremidade")
                                         .ToList();

            foreach (var no in nos)
            {
                CanvasMapa.Children.Remove(no);
            }
        }

        private void CriarNoVisualNoCanvas(Point ponto, HashSet<string> registro)
        {
            if (registro != null && registro.Count > 0)
            {
                string chave = $"{Math.Round(ponto.X, 1)}_{Math.Round(ponto.Y, 1)}";
                if (registro.Contains(chave)) return;
                registro.Add(chave);
            }

            double tamanhoNo = 30;

            var noVisual = new System.Windows.Shapes.Ellipse
            {
                Width = tamanhoNo,
                Height = tamanhoNo,
                Fill = AmareloSemantico,
                Stroke = CorTemaSecundaria,
                StrokeThickness = 2,
                Cursor = System.Windows.Input.Cursors.Pen,
                Tag = "NoExtremidade",
                DataContext = ponto
            };

            Canvas.SetLeft(noVisual, ponto.X - (tamanhoNo / 2));
            Canvas.SetTop(noVisual, ponto.Y - (tamanhoNo / 2));
            Panel.SetZIndex(noVisual, 100);

            CanvasMapa.Children.Add(noVisual);
        }

        private void IniciarCriacaoDeRuaPartindoDe(FrameworkElement noClicado)
        {
            if (noClicado?.DataContext is Point pontoOrigem)
            {
                criandoRua = true;
                noOrigemSelecionado = noClicado;

                linhaGuiaTemporaria = new System.Windows.Shapes.Line
                {
                    Stroke = AmareloSemantico,
                    StrokeThickness = 4,
                    StrokeDashArray = new System.Windows.Media.DoubleCollection() { 3, 3 },
                    X1 = pontoOrigem.X,
                    Y1 = pontoOrigem.Y,
                    X2 = pontoOrigem.X,
                    Y2 = pontoOrigem.Y
                };

                CanvasMapa.Children.Add(linhaGuiaTemporaria);
            }
        }

        private void FinalizarCriacaoDeRuaEm(FrameworkElement noDestino)
        {
            if (noOrigemSelecionado == noDestino) return;

            if (noOrigemSelecionado?.DataContext is Point p1 && noDestino?.DataContext is Point p2)
            {
                var novaRua = new System.Windows.Shapes.Polyline
                {
                    Stroke = CorTemaPrimaria,
                    StrokeThickness = 20,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    StrokeLineJoin = System.Windows.Media.PenLineJoin.Round,
                    StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
                    StrokeEndLineCap = System.Windows.Media.PenLineCap.Round,
                    ToolTip = "Nova Via Criada",
                    Tag = "Rua"
                };

                novaRua.Points.Add(p1);
                novaRua.Points.Add(p2);

                novaRua.MouseEnter += (s, e) =>
                {
                    if (modoAtual == ModoFerramenta.Adicionar) return;

                    if (s is System.Windows.Shapes.Polyline l)
                    {
                        if (viaSelecionadaAtual != l.DataContext && !ruasSelecionadas.Contains(l))
                        {
                            l.Stroke = modoAtual == ModoFerramenta.Deletar ? VermelhoSemantico : CorTemaTerciaria;
                            l.StrokeThickness = 24;
                        }
                    }
                };

                novaRua.MouseLeave += (s, e) =>
                {
                    if (s is System.Windows.Shapes.Polyline l)
                    {
                        if (viaSelecionadaAtual != l.DataContext && !ruasSelecionadas.Contains(l))
                        {
                            l.Stroke = CorTemaPrimaria;
                            l.StrokeThickness = 20;
                        }
                    }
                };

                CanvasMapa.Children.Add(novaRua);

                CancelarCriacaoRua();
                MostrarNosDasRuas();
            }
        }

        private void CancelarCriacaoRua()
        {
            criandoRua = false;
            noOrigemSelecionado = null;
            if (linhaGuiaTemporaria != null)
            {
                CanvasMapa.Children.Remove(linhaGuiaTemporaria);
                linhaGuiaTemporaria = null;
            }
        }

        private FrameworkElement ObterNoPorProximidade(Point pontoClique, double raioTolerancia = 20)
        {
            if (CanvasMapa == null) return null;

            foreach (var elipse in CanvasMapa.Children.OfType<System.Windows.Shapes.Ellipse>())
            {
                if (elipse.Tag?.ToString() == "NoExtremidade" && elipse.DataContext is Point pontoNo)
                {
                    double distancia = Math.Sqrt(Math.Pow(pontoClique.X - pontoNo.X, 2) + Math.Pow(pontoClique.Y - pontoNo.Y, 2));
                    if (distancia <= raioTolerancia) return elipse;
                }
            }
            return null;
        }

        private void DesenharNoCanvas(DadosMapa dados)
        {
            CanvasMapa.Children.Clear();
            if (dados == null || dados.elements == null || dados.elements.Count == 0) return;

            var primeiroPonto = dados.elements.FirstOrDefault(v => v.geometry != null && v.geometry.Count > 0)?.geometry[0];
            if (primeiroPonto == null) return;

            double latRef = primeiroPonto.lat;
            double lonRef = primeiroPonto.lon;
            double zoom = 500000;

            bool primeiroPontoCalculado = false;
            double menorX = 0, maiorX = 0, menorY = 0, maiorY = 0;
            var vm = ViewModel;

            foreach (var via in dados.elements)
            {
                if (via.geometry == null) continue;
                foreach (var pontoGeo in via.geometry)
                {
                    Point p = vm.ConverterParaPixel(pontoGeo.lat, pontoGeo.lon, latRef, lonRef, zoom);

                    if (!primeiroPontoCalculado)
                    {
                        menorX = maiorX = p.X;
                        menorY = maiorY = p.Y;
                        primeiroPontoCalculado = true;
                    }
                    else
                    {
                        if (p.X < menorX) menorX = p.X;
                        if (p.X > maiorX) maiorX = p.X;
                        if (p.Y < menorY) menorY = p.Y;
                        if (p.Y > maiorY) maiorY = p.Y;
                    }
                }
            }

            limiteMinX = -maiorX + 20;
            limiteMaxX = -menorX - 20;
            limiteMinY = -maiorY + 20;
            limiteMaxY = -menorY - 20;

            foreach (var via in dados.elements)
            {
                if (via.geometry != null && via.geometry.Count > 1)
                {
                    string nomeDaRua = via.tags != null && via.tags.ContainsKey("name") ? via.tags["name"] : "Via sem nome";

                    var linhaWpf = new System.Windows.Shapes.Polyline
                    {
                        Stroke = CorTemaPrimaria, // Cor Base: Tema Primária
                        StrokeThickness = 20,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        StrokeLineJoin = System.Windows.Media.PenLineJoin.Round,
                        StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
                        StrokeEndLineCap = System.Windows.Media.PenLineCap.Round,
                        ToolTip = nomeDaRua,
                        DataContext = via,
                        Tag = "Rua"
                    };

                    linhaWpf.MouseEnter += (s, e) =>
                    {
                        if (modoAtual == ModoFerramenta.Adicionar) return;

                        if (s is System.Windows.Shapes.Polyline l)
                        {
                            if (viaSelecionadaAtual != l.DataContext && !ruasSelecionadas.Contains(l))
                            {
                                l.Stroke = modoAtual == ModoFerramenta.Deletar ? VermelhoSemantico : CorTemaTerciaria;
                                l.StrokeThickness = 24;
                            }
                        }
                    };

                    linhaWpf.MouseLeave += (s, e) =>
                    {
                        if (s is System.Windows.Shapes.Polyline l)
                        {
                            if (viaSelecionadaAtual != l.DataContext && !ruasSelecionadas.Contains(l))
                            {
                                l.Stroke = CorTemaPrimaria;
                                l.StrokeThickness = 20;
                            }
                        }
                    };

                    linhaWpf.MouseDown += (s, e) =>
                    {
                        if (e.ChangedButton != MouseButton.Left) return;

                        if (modoAtual == ModoFerramenta.Editar)
                        {
                            e.Handled = true;

                            if (s is System.Windows.Shapes.Polyline linhaClicada)
                            {
                                if (linhaClicada.DataContext is Via dadosDaVia)
                                {
                                    SelecionarRuaParaEdicao(linhaClicada);
                                }
                            }
                        }
                    };

                    foreach (var pontoGeo in via.geometry)
                    {
                        var pontoPixel = vm.ConverterParaPixel(pontoGeo.lat, pontoGeo.lon, latRef, lonRef, zoom);
                        linhaWpf.Points.Add(pontoPixel);
                    }

                    CanvasMapa.Children.Add(linhaWpf);
                }
            }

            CentralizarMapa();
        }

        private void PreencherCamposInterface(Via dadosDaVia)
        {
            TxtNomeRua.Text = (dadosDaVia.tags != null && dadosDaVia.tags.TryGetValue("name", out string nome)) ? nome : "";

            LstVelocidadeRua.SelectedIndex = 0;
            if (dadosDaVia.tags != null && dadosDaVia.tags.TryGetValue("maxspeed", out string velocidade))
            {
                foreach (ListBoxItem item in LstVelocidadeRua.Items)
                {
                    if (item.Tag?.ToString() == velocidade)
                    {
                        LstVelocidadeRua.SelectedItem = item;
                        break;
                    }
                }
            }

            LstDirecaoRua.SelectedIndex = 0;
            if (dadosDaVia.tags != null && dadosDaVia.tags.TryGetValue("oneway", out string sentido))
            {
                foreach (ListBoxItem item in LstDirecaoRua.Items)
                {
                    if (item.Tag?.ToString() == sentido)
                    {
                        LstDirecaoRua.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void BtnDesfazer_Click(object sender, RoutedEventArgs e)
        {
            if (viaSelecionadaAtual != null)
            {
                PreencherCamposInterface(viaSelecionadaAtual);
            }
        }

        private void BtnRetratil_Checked(object sender, RoutedEventArgs e)
        {
            if (ContainerMenuDireito == null || BtnRetratil == null) return;
            ContainerMenuDireito.Width = 250;
            BtnRetratil.Content = "▶";
        }

        private void BtnRetratil_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ContainerMenuDireito == null || BtnRetratil == null) return;
            ContainerMenuDireito.Width = 0;
            BtnRetratil.Content = "◀";
        }

        private void ManusearMapa_Zoom(object sender, MouseWheelEventArgs e)
        {
            Grid area = sender as Grid;
            if (area == null || CanvasMapa == null) return;

            int antigoNivel = nivelZoomAtual;

            if (e.Delta > 0)
            {
                if (nivelZoomAtual < niveisDeEscala.Length - 1) nivelZoomAtual++;
            }
            else if (e.Delta < 0)
            {
                if (nivelZoomAtual > 0) nivelZoomAtual--;
            }

            if (antigoNivel != nivelZoomAtual)
            {
                double novaEscala = niveisDeEscala[nivelZoomAtual];
                double antigaEscala = niveisDeEscala[antigoNivel];

                CanvasMapa.RenderTransformOrigin = new Point(0, 0);
                Point posicaoMouseNaTela = e.GetPosition(area);

                TransformarZoom.ScaleX = novaEscala;
                TransformarZoom.ScaleY = novaEscala;

                double fator = novaEscala / antigaEscala;
                TransformarArrasto.X = posicaoMouseNaTela.X - (posicaoMouseNaTela.X - TransformarArrasto.X) * fator;
                TransformarArrasto.Y = posicaoMouseNaTela.Y - (posicaoMouseNaTela.Y - TransformarArrasto.Y) * fator;

                AtualizarTextoZoom();
            }

            e.Handled = true;
        }

        private void ManusearMapa_Click(object sender, MouseButtonEventArgs e)
        {
            Grid area = sender as Grid;
            if (area == null) return;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (modoAtual == ModoFerramenta.Adicionar)
                {
                    Point posicaoMouseNoCanvas = e.GetPosition(CanvasMapa);
                    FrameworkElement noEncontrado = ObterNoPorProximidade(posicaoMouseNoCanvas, raioTolerancia: 25);

                    if (noEncontrado == null)
                    {
                        HashSet<string> registroFake = new HashSet<string>();
                        CriarNoVisualNoCanvas(posicaoMouseNoCanvas, registroFake);
                        noEncontrado = ObterNoPorProximidade(posicaoMouseNoCanvas, raioTolerancia: 5);
                    }

                    if (noEncontrado != null)
                    {
                        InteragirComElementoDoMapa(noEncontrado);
                    }
                }
                else if (e.OriginalSource is FrameworkElement elemento)
                {
                    InteragirComElementoDoMapa(elemento);
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (criandoRua)
                {
                    CancelarCriacaoRua();
                    return;
                }

                mouseMovimento = true;
                pontoInicialMouse = e.GetPosition(area);
                deslocamentoInicialX = TransformarArrasto.X;
                deslocamentoInicialY = TransformarArrasto.Y;
                area.CaptureMouse();
            }
        }

        private void ManusearMapa_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                mouseMovimento = false;
                Grid area = sender as Grid;
                area?.ReleaseMouseCapture();
            }
        }

        private void ManusearMapa_Move(object sender, MouseEventArgs e)
        {
            Grid area = sender as Grid;
            if (area == null) return;

            if (criandoRua && linhaGuiaTemporaria != null)
            {
                Point posicaoMouseNoCanvas = e.GetPosition(CanvasMapa);
                linhaGuiaTemporaria.X2 = posicaoMouseNoCanvas.X;
                linhaGuiaTemporaria.Y2 = posicaoMouseNoCanvas.Y;
            }

            if (mouseMovimento)
            {
                Point pontoAtualMouse = e.GetPosition(area);
                double deltaX = pontoAtualMouse.X - pontoInicialMouse.X;
                double deltaY = pontoAtualMouse.Y - pontoInicialMouse.Y;

                TransformarArrasto.X = deslocamentoInicialX + deltaX;
                TransformarArrasto.Y = deslocamentoInicialY + deltaY;
            }
        }

        private void BotaoCentralizar_Click(object sender, RoutedEventArgs e) => CentralizarMapa();

        private void CentralizarMapa()
        {
            if (CanvasMapa == null) return;

            CanvasMapa.RenderTransformOrigin = new Point(0, 0);

            TransformarZoom.ScaleX = niveisDeEscala[nivelZoomAtual];
            TransformarZoom.ScaleY = niveisDeEscala[nivelZoomAtual];
            AtualizarTextoZoom();

            TransformarArrasto.X = 0;
            TransformarArrasto.Y = 0;
        }

        private void AtualizarTextoZoom()
        {
            double porcentagem = niveisDeEscala[nivelZoomAtual] * 100;
            TxtNivelZoom.Text = $"Zoom: {porcentagem}%";
        }

        private void AbrirMenuProjeto_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock botaoProjeto = sender as TextBlock;
            if (botaoProjeto == null) return;

            botaoProjeto.ContextMenu.IsEnabled = true;
            botaoProjeto.ContextMenu.PlacementTarget = botaoProjeto;
            botaoProjeto.ContextMenu.IsOpen = true;
        }

        private DadosMapa dadosMapaAtual;

        private async void OpcaoProjeto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                string valorTag = item.Tag.ToString();

                if (valorTag == "AbrirProjeto")
                {
                    OpenFileDialog seletor = new OpenFileDialog();
                    seletor.Filter = "Arquivos JSON (*.json)|*.json|Todos os arquivos (*.*)|*.*";
                    seletor.Title = "Selecione um projeto";

                    if (seletor.ShowDialog() == true)
                    {
                        var dados = await ViewModel.AbrirMapaExistente(seletor.FileName);
                        if (dados != null)
                        {
                            dadosMapaAtual = dados;
                            DesenharNoCanvas(dados);
                        }
                    }
                }
                else if (valorTag == "NovoProjeto")
                {
                    NovoProjetoWindow janelaCidade = new NovoProjetoWindow();
                    janelaCidade.Owner = this;

                    if (janelaCidade.ShowDialog() == true)
                    {
                        SaveFileDialog salvador = new SaveFileDialog();
                        salvador.Filter = "Arquivos JSON (*.json)|*.json";
                        salvador.Title = "Salvar novo mapa da cidade";

                        if (salvador.ShowDialog() == true)
                        {
                            string areaInteresse = janelaCidade.AreaInteresseResultado;
                            var dados = await ViewModel.CriarNovoMapa(areaInteresse, salvador.FileName);

                            if (dados != null)
                            {
                                dadosMapaAtual = dados;
                                DesenharNoCanvas(dados);
                                MessageBox.Show("Mapa da cidade gerado e salvo com sucesso!");
                            }
                        }
                    }
                }
                else if (valorTag == "SalvarProjeto")
                {
                    if (dadosMapaAtual == null) return;

                    SaveFileDialog salvador = new SaveFileDialog();
                    salvador.Filter = "Arquivos JSON (*.json)|*.json";

                    if (salvador.ShowDialog() == true)
                    {
                        await ViewModel.SalvarMapaEmDisco(salvador.FileName, dadosMapaAtual);
                        MessageBox.Show("Projeto salvo com sucesso!");
                    }
                }
            }
        }

        private void AbrirConfiguracoes_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ConfiguracoesWindow Configuracoes = new ConfiguracoesWindow();
                Configuracoes.Owner = this;
                Configuracoes.ShowDialog();
            }
        }
    }
}
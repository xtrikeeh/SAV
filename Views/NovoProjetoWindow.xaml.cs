using System;
using System.Windows;
using System.Security.Permissions;

namespace SAV.Views
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public partial class NovoProjetoWindow : Window
    {
        public string AreaInteresseResultado { get; private set; }

        public NovoProjetoWindow()
        {
            InitializeComponent();

            NavegadorMapa.ObjectForScripting = this;

            Loaded += NovoProjetoWindow_Loaded;
        }

        private void NovoProjetoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string htmlMapa = @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta http-equiv='X-UA-Compatible' content='IE=edge,chrome=1'>
                <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
                <link rel='stylesheet' href='https://unpkg.com/leaflet-draw@1.0.4/dist/leaflet.draw.css'/>
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                <script src='https://unpkg.com/leaflet-draw@1.0.4/dist/leaflet.draw.js'></script>
                <style>
                    html, body, #map { height: 100%; margin: 0; padding: 0; }
                </style>
            </head>
            <body>
                <div id='map'></div>
                <script>
                    // Centraliza o mapa inicialmente no Brasil (São Paulo)
                    var map = L.map('map').setView([-23.5505, -46.6333], 12);
                    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);

                    var drawnItems = new L.FeatureGroup();
                    map.addLayer(drawnItems);

                    var drawControl = new L.Control.Draw({
                        draw: { rectangle: true, polyline: false, circle: false, marker: false, polygon: false, circlemarker: false },
                        edit: { featureGroup: drawnItems, remove: true }
                    });
                    map.addControl(drawControl);

                    // Função que move o mapa quando o usuário busca uma coordenada
                    function focarMapa(lat, lon) {
                        map.setView([lat, lon], 14);
                    }

                    // Evento disparado ao terminar de desenhar o retângulo
                    map.on(L.Draw.Event.CREATED, function (e) {
                        drawnItems.clearLayers();
                        var layer = e.layer;
                        drawnItems.addLayer(layer);
                        
                        var bounds = layer.getBounds();
                        var norte = bounds.getNorth();
                        var sul = bounds.getSouth();
                        var leste = bounds.getEast();
                        var oeste = bounds.getWest();

                        // Envia os dados de volta para o C# chamando a função 'ReceberCoordenadas'
                        var bbox = sul + ',' + oeste + ',' + norte + ',' + leste;
                        window.external.ReceberCoordenadas(bbox);
                    });
                </script>
            </body>
            </html>";

            NavegadorMapa.NavigateToString(htmlMapa);
        }

        public void ReceberCoordenadas(string bbox)
        {
            if (!string.IsNullOrEmpty(bbox))
            {
                AreaInteresseResultado = bbox;
                TxtStatusArea.Text = "Área capturada com sucesso!";
                BtnCriar.IsEnabled = true;
            }
        }

        private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string local = TxtBusca.Text.Trim();
            if (string.IsNullOrEmpty(local)) return;

            try
            {
                using (System.Net.Http.HttpClient cliente = new System.Net.Http.HttpClient())
                {
                    cliente.DefaultRequestHeaders.Add("User-Agent", "SAV-App/1.0");
                    string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(local)}&format=json&limit=1";
                    string resposta = await cliente.GetStringAsync(url);

                    using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(resposta))
                    {
                        var raiz = doc.RootElement;
                        if (raiz.GetArrayLength() > 0)
                        {
                            string lat = raiz[0].GetProperty("lat").GetString();
                            string lon = raiz[0].GetProperty("lon").GetString();

                            NavegadorMapa.InvokeScript("focarMapa", new object[] { lat, lon });
                        }
                        else
                        {
                            MessageBox.Show("Não encontramos essa região. Tente mudar o termo da busca.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao buscar região: " + ex.Message);
            }
        }

        private void BtnCriar_Click(object sender, RoutedEventArgs e) => this.DialogResult = true;
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    }
}
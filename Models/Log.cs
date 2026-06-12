using System;
using System.IO;

namespace SAV.Models
{
    public class SistemaLog
    {
        private readonly string _caminhoArquivo;

        public SistemaLog(string nomeArquivo = "historico_acoes.txt")
        {
            string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string pastaSav = Path.Combine(pastaDocumentos, "SAV");

            if (!Directory.Exists(pastaSav))
            {
                Directory.CreateDirectory(pastaSav);
            }

            _caminhoArquivo = Path.Combine(pastaSav, nomeArquivo);
        }
        public void Registrar(string descricao, string tipoElemento = "")
        {
            try
            {
                if (File.Exists(_caminhoArquivo))
                {
                    File.SetAttributes(_caminhoArquivo, FileAttributes.Normal);
                }

                string novaLinha = $"({DateTime.Now:dd-MM-yyyy HH:mm}) - {descricao}{Environment.NewLine}";

                File.AppendAllText(_caminhoArquivo, novaLinha);

                File.SetAttributes(_caminhoArquivo, FileAttributes.ReadOnly);
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro);
            }
        }
    }
}
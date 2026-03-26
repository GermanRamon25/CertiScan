using CertiScan.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertiScan
{
    public class DocumentoViewModel : ObservableObject
    {
        public Documento Documento { get; }

        public int Id => Documento.Id;
        public string NombreArchivo => Documento.NombreArchivo;
        public DateTime FechaCarga => Documento.FechaCarga;

        // Propiedades vinculadas al DataGrid
        private string _rfc;
        public string RFC { get => _rfc; set => SetProperty(ref _rfc, value); }

        private string _situacion;
        public string Situacion { get => _situacion; set => SetProperty(ref _situacion, value); }

        public DocumentoViewModel(Documento documento)
        {
            Documento = documento;
            ExtraerDatos();
        }

        private void ExtraerDatos()
        {
            // Si el contenido tiene nuestro formato estructurado con pipes |
            if (!string.IsNullOrEmpty(Documento.Contenido) && Documento.Contenido.Contains("|"))
            {
                var lineas = Documento.Contenido.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lineas.Length > 0)
                {
                    var partes = lineas[0].Split('|');
                    if (partes.Length >= 3)
                    {
                        this.RFC = partes[0].Trim();
                        this.Situacion = partes[2].Trim();
                    }
                }
            }
            else
            {
                this.RFC = "N/A";
                this.Situacion = "Pendiente";
            }
        }
    }
}

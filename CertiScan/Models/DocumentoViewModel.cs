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
        public string RutaArchivo { get; set; }


        private bool _isSearchResult;
        public bool IsSearchResult
        {
            get => _isSearchResult;
            set => SetProperty(ref _isSearchResult, value);
        }

        public int Id => Documento.Id;
        public string NombreArchivo => Documento.NombreArchivo;
        public DateTime FechaCarga => Documento.FechaCarga;

        public DocumentoViewModel(Documento documento)
        {
            Documento = documento;
        }
    }
}

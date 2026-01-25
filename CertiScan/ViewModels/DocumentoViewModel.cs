using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CertiScan.Models;

namespace CertiScan.Models
{
    public class DocumentoViewModel : ObservableObject
    {
        private readonly Documento _model;

        public DocumentoViewModel(Documento model)
        {
            _model = model;
        }

        public int Id => _model.Id;
        public string NombreArchivo => _model.NombreArchivo;
        public string RutaArchivo => _model.RutaArchivo; // Esta es la ruta que usará el WebView2

        private bool _isSearchResult;
        public bool IsSearchResult
        {
            get => _isSearchResult;
            set => SetProperty(ref _isSearchResult, value);
        }
    }
}
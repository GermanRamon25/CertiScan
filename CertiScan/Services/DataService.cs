// Se especifica explícitamente que se usará el cliente de Microsoft.
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using CertiScan.Models; // Asegúrate que el namespace a tus Modelos sea correcto

namespace CertiScan.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Server=localhost\\SQLEXPRESS;Database=NotariaUIF;Trusted_Connection=True;TrustServerCertificate=True;";

        public void GuardarDocumento(string nombreArchivo, string rutaFisica, string contenidoTexto)
        {
            // Al usar 'var', el compilador infiere el tipo correcto (Microsoft.Data.SqlClient.SqlConnection)
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "INSERT INTO Documentos (NombreArchivo, RutaFisica, ContenidoTexto) VALUES (@NombreArchivo, @RutaFisica, @ContenidoTexto)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NombreArchivo", nombreArchivo);
                    command.Parameters.AddWithValue("@RutaFisica", rutaFisica);
                    command.Parameters.AddWithValue("@ContenidoTexto", contenidoTexto);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Documento> BuscarTermino(string termino)
        {
            var resultados = new List<Documento>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, NombreArchivo, FechaCarga FROM Documentos WHERE CONTAINS(ContenidoTexto, @Termino)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Termino", $"\"{termino}\"");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultados.Add(new Documento
                            {
                                Id = reader.GetInt32(0),
                                NombreArchivo = reader.GetString(1),
                                FechaCarga = reader.GetDateTime(2)
                            });
                        }
                    }
                }
            }
            return resultados;
        }

        public void RegistrarBusqueda(string terminoBuscado, bool resultadoEncontrado)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "INSERT INTO Busquedas (TerminoBuscado, ResultadoEncontrado) VALUES (@Termino, @Encontrado)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Termino", terminoBuscado);
                    command.Parameters.AddWithValue("@Encontrado", resultadoEncontrado);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
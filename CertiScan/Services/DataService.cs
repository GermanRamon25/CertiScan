using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using CertiScan.Models;
using System.Configuration;
using System.Data;
using System.Threading.Tasks; // Necesario para Task

namespace CertiScan.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["CertiScanDBConnection"].ConnectionString;

        // ==========================================
        // MÉTODOS PARA GESTIÓN DE NOTARÍA
        // ==========================================

        public async Task<NotariaInfo> ObtenerDatosNotariaAsync(int notariaId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT Id, NombreNotario, NumeroNotaria, Direccion, Telefono, Email FROM Notaria WHERE Id = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", notariaId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new NotariaInfo
                            {
                                Id = reader.GetInt32(0),
                                NombreNotario = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                NumeroNotaria = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Direccion = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Telefono = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Email = reader.IsDBNull(5) ? "" : reader.GetString(5)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> ActualizarNotariaAsync(NotariaInfo info)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"UPDATE Notaria 
                             SET NombreNotario = @Nombre, 
                                 NumeroNotaria = @Numero, 
                                 Direccion = @Direccion, 
                                 Telefono = @Telefono, 
                                 Email = @Email 
                             WHERE Id = @Id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", (object)info.NombreNotario ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Numero", (object)info.NumeroNotaria ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Direccion", (object)info.Direccion ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Telefono", (object)info.Telefono ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Email", (object)info.Email ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Id", info.Id);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        // ==========================================
        // GESTIÓN DE DOCUMENTOS Y LISTADO SAT 69-B
        // ==========================================

        public async Task GuardarDocumentoAsync(string nombreArchivo, string rutaFisica, string contenidoTexto, int usuarioId, string tipoModulo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "INSERT INTO Documentos (NombreArchivo, RutaFisica, ContenidoTexto, UsuarioId, TipoModulo) VALUES (@NA, @RF, @CT, @UID, @TM)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NA", nombreArchivo);
                    command.Parameters.AddWithValue("@RF", rutaFisica);
                    command.Parameters.AddWithValue("@CT", contenidoTexto);
                    command.Parameters.AddWithValue("@UID", usuarioId);
                    command.Parameters.AddWithValue("@TM", tipoModulo);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // MÉTODO CLAVE: Carga masiva asíncrona para evitar congelamientos
        public async Task CargaMasivaListadoSatAsync(DataTable dtSat)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Borrado previo asíncrono
                using (var cmd = new SqlCommand("DELETE FROM ListadoSat69B", connection))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Inserción masiva asíncrona
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "ListadoSat69B";
                    bulkCopy.BatchSize = 5000;
                    bulkCopy.ColumnMappings.Add("RFC", "RFC");
                    bulkCopy.ColumnMappings.Add("NombreContribuyente", "NombreContribuyente");
                    bulkCopy.ColumnMappings.Add("Situacion", "Situacion");
                    bulkCopy.ColumnMappings.Add("UsuarioId", "UsuarioId");

                    await bulkCopy.WriteToServerAsync(dtSat);
                }
            }
        }

        public async Task<List<Documento>> GetDocumentsByUserAsync(int usuarioId, string tipoModulo)
        {
            var resultados = new List<Documento>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT Id, NombreArchivo, FechaCarga FROM Documentos WHERE UsuarioId = @UID AND TipoModulo = @TM ORDER BY FechaCarga DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UID", usuarioId);
                    command.Parameters.AddWithValue("@TM", tipoModulo);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resultados.Add(new Documento { Id = reader.GetInt32(0), NombreArchivo = reader.GetString(1), FechaCarga = reader.GetDateTime(2) });
                        }
                    }
                }
            }
            return resultados;
        }

        public async Task RegistrarBusquedaAsync(string terminoBuscado, bool resultadoEncontrado, int usuarioId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "INSERT INTO Busquedas (TerminoBuscado, ResultadoEncontrado, UsuarioId) VALUES (@T, @RE, @UID)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@T", terminoBuscado);
                    command.Parameters.AddWithValue("@RE", resultadoEncontrado);
                    command.Parameters.AddWithValue("@UID", usuarioId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // ==========================================
        // MÉTODOS SÍNCRONOS (Para mantener compatibilidad si es necesario)
        // ==========================================

        public NotariaInfo ObtenerDatosNotaria(int notariaId) => ObtenerDatosNotariaAsync(notariaId).GetAwaiter().GetResult();
        public void GuardarDocumento(string na, string rf, string ct, int uid, string tm) => GuardarDocumentoAsync(na, rf, ct, uid, tm).GetAwaiter().GetResult();
        public void CargaMasivaListadoSat(DataTable dt) => CargaMasivaListadoSatAsync(dt).GetAwaiter().GetResult();
        public List<Documento> GetDocumentsByUser(int uid, string tm) => GetDocumentsByUserAsync(uid, tm).GetAwaiter().GetResult();
    }
}
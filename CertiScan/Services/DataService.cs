using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using CertiScan.Models;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Linq;

namespace CertiScan.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["CertiScanDBConnection"].ConnectionString;

        // ==========================================
        // GESTIÓN DE NOTARÍA
        // ==========================================
        public NotariaInfo ObtenerDatosNotaria(int notariaId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, NombreNotario, NumeroNotaria, Direccion, Telefono, Email FROM Notaria WHERE Id = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", notariaId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
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
        // Agrega este método dentro de la clase DatabaseService, 
        // preferiblemente en la sección de GESTIÓN DE NOTARÍA.

        public bool ExisteTelefonoEnOtraNotaria(string telefono, int miNotariaId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Esta consulta busca si el teléfono ya existe en un registro 
                // que TENGA un ID diferente al de la notaría que está intentando guardar.
                var query = "SELECT COUNT(1) FROM Notaria WHERE Telefono = @Telefono AND Id <> @Id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Telefono", (object)telefono ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Id", miNotariaId);

                    // Si el conteo es mayor a 0, significa que otra notaría ya usa ese número.
                    return (int)command.ExecuteScalar() > 0;
                }
            }
        }


        public bool ActualizarNotaria(NotariaInfo info)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"UPDATE Notaria 
                             SET NombreNotario = @Nombre, NumeroNotaria = @Numero, 
                                 Direccion = @Direccion, Telefono = @Telefono, Email = @Email 
                             WHERE Id = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", (object)info.NombreNotario ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Numero", (object)info.NumeroNotaria ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Direccion", (object)info.Direccion ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Telefono", (object)info.Telefono ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Email", (object)info.Email ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Id", info.Id);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        // ==========================================
        // GESTIÓN DE USUARIOS
        // ==========================================
        public Usuario GetUserByUsername(string username)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, NombreCompleto, NombreUsuario, NotariaId FROM Usuarios WHERE NombreUsuario = @Username";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Usuario
                            {
                                Id = reader.GetInt32(0),
                                NombreCompleto = reader.GetString(1),
                                NombreUsuario = reader.GetString(2),
                                NotariaId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public bool AddUser(string fullName, string username, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    var insertNotariaQuery = "INSERT INTO Notaria (NombreNotario) OUTPUT INSERTED.Id VALUES ('Nueva Notaría')";
                    int newNotariaId;
                    using (var notariaCommand = new SqlCommand(insertNotariaQuery, connection, transaction))
                    {
                        newNotariaId = (int)notariaCommand.ExecuteScalar();
                    }
                    var insertUserQuery = "INSERT INTO Usuarios (NombreCompleto, NombreUsuario, Password, NotariaId) VALUES (@FullName, @Username, @Password, @NotariaId)";
                    using (var userCommand = new SqlCommand(insertUserQuery, connection, transaction))
                    {
                        userCommand.Parameters.AddWithValue("@FullName", fullName);
                        userCommand.Parameters.AddWithValue("@Username", username);
                        userCommand.Parameters.AddWithValue("@Password", password);
                        userCommand.Parameters.AddWithValue("@NotariaId", newNotariaId);
                        userCommand.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public bool ValidateUser(string username, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT COUNT(1) FROM Usuarios WHERE NombreUsuario = @Username AND Password = @Password";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    return (int)command.ExecuteScalar() > 0;
                }
            }
        }


        // ============================================================
        // DOCUMENTOS Y CARGAS
        // ============================================================
        public List<Documento> GetDocumentsByUser(int usuarioId, string tipoModulo)
        {
            var resultados = new List<Documento>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT TOP 100 Id, NombreArchivo, FechaCarga FROM Documentos WHERE UsuarioId = @UID AND TipoModulo = @TM ORDER BY FechaCarga DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UID", usuarioId);
                    command.Parameters.AddWithValue("@TM", tipoModulo);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultados.Add(new Documento { Id = reader.GetInt32(0), NombreArchivo = reader.GetString(1), FechaCarga = reader.GetDateTime(2) });
                        }
                    }
                }
            }
            return resultados;
        }

        public List<Documento> BuscarTermino(string termino, int usuarioId, string tipoModulo)
        {
            var resultados = new List<Documento>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"SELECT TOP 100 Id, NombreArchivo, FechaCarga 
                             FROM Documentos 
                             WHERE UsuarioId = @UID AND TipoModulo = @TM 
                             AND CONTAINS(ContenidoTexto, @Termino)
                             ORDER BY FechaCarga DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UID", usuarioId);
                    command.Parameters.AddWithValue("@TM", tipoModulo);
                    command.Parameters.AddWithValue("@Termino", $"\"*{termino}*\"");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultados.Add(new Documento { Id = reader.GetInt32(0), NombreArchivo = reader.GetString(1), FechaCarga = reader.GetDateTime(2) });
                        }
                    }
                }
            }
            return resultados;
        }

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

        public async Task CargaMasivaListadoSatAsync(DataTable dtSat)
        {
            try
            {
                for (int i = dtSat.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow dr = dtSat.Rows[i];
                    string primeraColumna = dr[0].ToString();
                    string segundaColumna = dr[1].ToString();

                    if (primeraColumna.Contains("Información actualizada") ||
                        primeraColumna.Contains("Listado completo") ||
                        primeraColumna.Equals("No") ||
                        segundaColumna.Equals("RFC"))
                    {
                        dr.Delete();
                    }
                }
                dtSat.AcceptChanges();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("DELETE FROM ListadoSat69B", connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = "ListadoSat69B";
                        bulkCopy.BatchSize = 5000;
                        bulkCopy.ColumnMappings.Clear();
                        bulkCopy.ColumnMappings.Add(1, "RFC");
                        bulkCopy.ColumnMappings.Add(2, "NombreContribuyente");
                        bulkCopy.ColumnMappings.Add(3, "Situacion");
                        await bulkCopy.WriteToServerAsync(dtSat);
                    }
                }
                System.Windows.MessageBox.Show("Carga finalizada con éxito.", "CertiScan");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error al cargar los datos: " + ex.Message, "Error de Sistema");
            }
        }

        public string GetDocumentoContent(int documentoId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT ContenidoTexto FROM Documentos WHERE Id = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", documentoId);
                    var result = command.ExecuteScalar();
                    return result != null ? result.ToString() : "Contenido no encontrado.";
                }
            }
        }

        // ==========================================
        // HISTORIAL Y OTROS - CORREGIDO PARA SEPARAR MÓDULOS
        // ==========================================
        public List<BusquedaHistorial> GetSearchHistory(int usuarioId, string nombreUsuario, string tipoModulo)
        {
            var historial = new List<BusquedaHistorial>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                bool esAdmin = (nombreUsuario.ToLower() == "admin");

                // Filtramos obligatoriamente por el módulo (UIF o SAT)
                string query = @"SELECT u.NombreUsuario, b.TerminoBuscado, b.FechaCarga, b.ResultadoEncontrado, b.Modulo 
                                FROM Busquedas b JOIN Usuarios u ON b.UsuarioId = u.Id 
                                WHERE b.Modulo = @Modulo";

                if (!esAdmin) query += " AND b.UsuarioId = @UsuarioId";

                query += " ORDER BY b.FechaCarga DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Modulo", tipoModulo);
                    if (!esAdmin) command.Parameters.AddWithValue("@UsuarioId", usuarioId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            historial.Add(new BusquedaHistorial
                            {
                                NombreUsuario = reader.GetString(0),
                                TerminoBuscado = reader.GetString(1),
                                FechaCarga = reader.GetDateTime(2),
                                ResultadoEncontrado = reader.GetBoolean(3),
                                Modulo = reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return historial;
        }

        public void RegistrarBusqueda(string t, bool re, int uid, string tipoModulo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Ahora insertamos también el nombre del módulo
                var query = "INSERT INTO Busquedas (TerminoBuscado, ResultadoEncontrado, UsuarioId, Modulo) VALUES (@T, @RE, @UID, @MOD)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@T", t);
                    command.Parameters.AddWithValue("@RE", re);
                    command.Parameters.AddWithValue("@UID", uid);
                    command.Parameters.AddWithValue("@MOD", tipoModulo);
                    command.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        // MÉTODO DE BÚSQUEDA INTELIGENTE SAT
        // ============================================================
        public DataTable BuscarEnListadoSat(string termino)
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrWhiteSpace(termino)) return dt;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                termino = termino.Trim();
                string[] palabras = termino.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string query = @"SELECT TOP 100 RFC, NombreContribuyente, Situacion 
                                 FROM ListadoSat69B 
                                 WHERE LTRIM(RTRIM(RFC)) LIKE @TerminoExacto 
                                 OR (";

                for (int i = 0; i < palabras.Length; i++)
                {
                    if (i > 0) query += " AND ";
                    query += $"LTRIM(RTRIM(NombreContribuyente)) LIKE @P{i}";
                }
                query += ")";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TerminoExacto", "%" + termino + "%");
                    for (int i = 0; i < palabras.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@P{i}", "%" + palabras[i] + "%");
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public void DeleteDocument(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand("DELETE FROM Documentos WHERE Id = @Id", connection))
                {
                    cmd.Parameters.AddWithValue("@Id", documentId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
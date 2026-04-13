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
        // Esta es la ÚNICA declaración permitida de la cadena de conexión
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["CertiScanDBConnection"].ConnectionString;

        // ==========================================
        // GESTIÓN DE NOTARÍA
        // ==========================================

        /// <summary>
        /// Obtiene la información de la notaría incluyendo los campos de control de anualidad.
        /// </summary>
        public NotariaInfo ObtenerDatosNotaria(int notariaId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // SELECT actualizado para incluir FechaVencimiento y EstaActivo
                var query = "SELECT Id, NombreNotario, NumeroNotaria, Direccion, Telefono, Email, FechaVencimiento, EstaActivo FROM Notaria WHERE Id = @Id";
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
                                Email = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                // Nuevos campos de suscripción
                                FechaVencimiento = reader.GetDateTime(6),
                                EstaActivo = reader.GetBoolean(7)
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Valida si la notaría tiene permitido el acceso según su fecha de vencimiento y estado activo.
        /// </summary>
        public bool ValidarAccesoSistema(int notariaId, out string mensaje)
        {
            mensaje = string.Empty;
            var info = ObtenerDatosNotaria(notariaId);

            if (info == null)
            {
                mensaje = "Error: No se encontró información de la notaría asociada.";
                return false;
            }

            // 1. Validar el Kill Switch manual
            if (!info.EstaActivo)
            {
                mensaje = "ACCESO DENEGADO: El sistema ha sido desactivado por el administrador.";
                return false;
            }

            // 2. Validar fecha de vencimiento anual
            if (DateTime.Now > info.FechaVencimiento)
            {
                mensaje = $"SUSCRIPCIÓN VENCIDA: Su anualidad terminó el {info.FechaVencimiento:dd/MM/yyyy}.\nContacte a soporte para renovar su servicio.";
                return false;
            }

            return true;
        }

        public bool ExisteTelefonoEnOtraNotaria(string telefono, int miNotariaId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT COUNT(1) FROM Notaria WHERE Telefono = @Telefono AND Id <> @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Telefono", (object)telefono ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Id", miNotariaId);
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
                    // Al insertar una nueva notaría, se le da 1 año de vigencia por defecto
                    var insertNotariaQuery = "INSERT INTO Notaria (NombreNotario, FechaVencimiento, EstaActivo) OUTPUT INSERTED.Id VALUES ('Nueva Notaría', DATEADD(year, 1, GETDATE()), 1)";
                    int newNotariaId;
                    using (var notariaCommand = new SqlCommand(insertNotariaQuery, connection, transaction))
                    {
                        newNotariaId = Convert.ToInt32(notariaCommand.ExecuteScalar());
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
                var query = "SELECT TOP 100 Id, NombreArchivo, FechaCarga, ContenidoTexto FROM Documentos WHERE UsuarioId = @UID AND TipoModulo = @TM ORDER BY FechaCarga DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UID", usuarioId);
                    command.Parameters.AddWithValue("@TM", tipoModulo);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultados.Add(new Documento
                            {
                                Id = reader.GetInt32(0),
                                NombreArchivo = reader.GetString(1),
                                FechaCarga = reader.GetDateTime(2),
                                ContenidoTexto = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                            });
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
                // OPTIMIZACIÓN: Se puede cambiar a CONTAINS si se configuró el FullText Index
                var query = @"SELECT TOP 100 Id, NombreArchivo, FechaCarga, ContenidoTexto
                             FROM Documentos 
                             WHERE UsuarioId = @UID AND TipoModulo = @TM 
                             AND ContenidoTexto LIKE @Termino
                             ORDER BY FechaCarga DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UID", usuarioId);
                    command.Parameters.AddWithValue("@TM", tipoModulo);
                    command.Parameters.AddWithValue("@Termino", "%" + termino + "%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultados.Add(new Documento
                            {
                                Id = reader.GetInt32(0),
                                NombreArchivo = reader.GetString(1),
                                FechaCarga = reader.GetDateTime(2),
                                ContenidoTexto = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                            });
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
        // HISTORIAL Y REGISTRO
        // ==========================================
        public List<BusquedaHistorial> GetSearchHistory(int usuarioId, string nombreUsuario, string tipoModulo)
        {
            var historial = new List<BusquedaHistorial>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                bool esAdmin = (nombreUsuario.ToLower() == "admin");
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

        public DataTable BuscarEnListadoSat(string termino)
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrWhiteSpace(termino)) return dt;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string terminoLimpio = termino.Replace(".", "").Replace(",", "").Trim();
                string[] palabras = terminoLimpio.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string query = @"SELECT TOP 100 RFC, NombreContribuyente, Situacion 
                         FROM ListadoSat69B 
                         WHERE RFC LIKE @TerminoExacto COLLATE Latin1_General_CI_AI
                         OR (";

                for (int i = 0; i < palabras.Length; i++)
                {
                    if (i > 0) query += " AND ";
                    query += $"REPLACE(REPLACE(NombreContribuyente, '.', ''), ',', '') LIKE @P{i} COLLATE Latin1_General_CI_AI";
                }
                query += ")";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TerminoExacto", "%" + terminoLimpio + "%");
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
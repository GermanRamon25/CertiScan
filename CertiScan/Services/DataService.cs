using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using CertiScan.Models;
using System.Configuration;

namespace CertiScan.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["CertiScanDBConnection"].ConnectionString;

        // ==========================================
        // MÉTODOS PARA GESTIÓN DE NOTARÍA
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

        public bool ActualizarNotaria(NotariaInfo info)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
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

                    int filas = command.ExecuteNonQuery();
                    return filas > 0;
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
                    int userCount = (int)command.ExecuteScalar();
                    return userCount > 0;
                }
            }
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
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        // ==========================================
        // MÉTODOS DE DOCUMENTOS (ACTUALIZADOS POR USUARIO)
        // ==========================================

        // Se agrega el parámetro usuarioId para vincular el archivo al subirlo
        public void GuardarDocumento(string nombreArchivo, string rutaFisica, string contenidoTexto, int usuarioId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "INSERT INTO Documentos (NombreArchivo, RutaFisica, ContenidoTexto, UsuarioId) VALUES (@NombreArchivo, @RutaFisica, @ContenidoTexto, @UsuarioId)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NombreArchivo", nombreArchivo);
                    command.Parameters.AddWithValue("@RutaFisica", rutaFisica);
                    command.Parameters.AddWithValue("@ContenidoTexto", contenidoTexto);
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Se reemplaza GetAllDocuments por uno que filtre por el usuario actual
        public List<Documento> GetDocumentsByUser(int usuarioId)
        {
            var resultados = new List<Documento>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, NombreArchivo, FechaCarga FROM Documentos WHERE UsuarioId = @UsuarioId ORDER BY FechaCarga DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);
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

        // Filtrar búsqueda para que solo encuentre palabras en documentos del usuario
        public List<Documento> BuscarTermino(string termino, int usuarioId)
        {
            var resultados = new List<Documento>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, NombreArchivo, FechaCarga FROM Documentos WHERE UsuarioId = @UsuarioId AND CONTAINS(ContenidoTexto, @Termino)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);
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

        public string DeleteDocument(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string filePath = null;
                var selectQuery = "SELECT RutaFisica FROM Documentos WHERE Id = @Id";
                using (var selectCommand = new SqlCommand(selectQuery, connection))
                {
                    selectCommand.Parameters.AddWithValue("@Id", documentId);
                    var result = selectCommand.ExecuteScalar();
                    if (result != null) filePath = result.ToString();
                }

                if (filePath == null) throw new Exception("No se encontró el documento.");

                var deleteQuery = "DELETE FROM Documentos WHERE Id = @Id";
                using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@Id", documentId);
                    deleteCommand.ExecuteNonQuery();
                }
                return filePath;
            }
        }

        // ==========================================
        // HISTORIAL DE BÚSQUEDAS
        // ==========================================

        public void RegistrarBusqueda(string terminoBuscado, bool resultadoEncontrado, int usuarioId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "INSERT INTO Busquedas (TerminoBuscado, ResultadoEncontrado, UsuarioId) VALUES (@Termino, @Encontrado, @UsuarioId)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Termino", terminoBuscado);
                    command.Parameters.AddWithValue("@Encontrado", resultadoEncontrado);
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<BusquedaHistorial> GetSearchHistory(int usuarioId, string nombreUsuario)
        {
            var historial = new List<BusquedaHistorial>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                bool esAdmin = (nombreUsuario.ToLower() == "admin");

                string query = @"
                SELECT
                u.NombreUsuario,
                b.TerminoBuscado,
                b.FechaCarga,
                b.ResultadoEncontrado
                FROM Busquedas b
                JOIN Usuarios u ON b.UsuarioId = u.Id";

                if (!esAdmin) query += " WHERE b.UsuarioId = @UsuarioId";
                query += " ORDER BY b.FechaCarga DESC";

                using (var command = new SqlCommand(query, connection))
                {
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
                                ResultadoEncontrado = reader.GetBoolean(3)
                            });
                        }
                    }
                }
            }
            return historial;
        }
    }
}
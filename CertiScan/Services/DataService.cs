using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using CertiScan.Models;

namespace CertiScan.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Server=GERMAN25\\SQLEXPRESS;Database=CertiScanDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public void GuardarDocumento(string nombreArchivo, string rutaFisica, string contenidoTexto)
        {
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

        // --- MÉTODO MODIFICADO ---
        public void RegistrarBusqueda(string terminoBuscado, bool resultadoEncontrado, int usuarioId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Se actualiza la consulta para incluir el UsuarioId
                var query = "INSERT INTO Busquedas (TerminoBuscado, ResultadoEncontrado, UsuarioId) VALUES (@Termino, @Encontrado, @UsuarioId)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Termino", terminoBuscado);
                    command.Parameters.AddWithValue("@Encontrado", resultadoEncontrado);
                    // Se añade el nuevo parámetro para el ID del usuario
                    command.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    command.ExecuteNonQuery();
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

        public bool ValidateUser(string username, string password)
        {
            string passwordHash = PasswordHasher.ComputeSha256Hash(password);
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT COUNT(1) FROM Usuarios WHERE NombreUsuario = @Username AND PasswordHash = @PasswordHash";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    int userCount = (int)command.ExecuteScalar();
                    return userCount > 0;
                }
            }
        }

        // --- MÉTODO NUEVO AÑADIDO ---
        public Usuario GetUserByUsername(string username)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, NombreCompleto, NombreUsuario FROM Usuarios WHERE NombreUsuario = @Username";
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
                                NombreUsuario = reader.GetString(2)
                            };
                        }
                    }
                }
            }
            return null; // Usuario no encontrado
        }

        // --- MÉTODO NUEVO AÑADIDO ---
        public List<BusquedaHistorial> GetSearchHistory()
        {
            var historial = new List<BusquedaHistorial>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT
                        u.NombreUsuario,
                        b.TerminoBuscado,
                        b.FechaBusqueda,
                        b.ResultadoEncontrado
                    FROM Busquedas b
                    JOIN Usuarios u ON b.UsuarioId = u.Id
                    ORDER BY b.FechaBusqueda DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            historial.Add(new BusquedaHistorial
                            {
                                NombreUsuario = reader.GetString(0),
                                TerminoBuscado = reader.GetString(1),
                                FechaBusqueda = reader.GetDateTime(2),
                                ResultadoEncontrado = reader.GetBoolean(3)
                            });
                        }
                    }
                }
            }
            return historial;
        }

        public bool AddUser(string fullName, string username, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var checkUserQuery = "SELECT COUNT(1) FROM Usuarios WHERE NombreUsuario = @Username";
                using (var checkUserCommand = new SqlCommand(checkUserQuery, connection))
                {
                    checkUserCommand.Parameters.AddWithValue("@Username", username);
                    int userCount = (int)checkUserCommand.ExecuteScalar();
                    if (userCount > 0)
                    {
                        throw new Exception("El nombre de usuario ya está en uso. Por favor, elige otro.");
                    }
                }

                string passwordHash = PasswordHasher.ComputeSha256Hash(password);
                var insertQuery = "INSERT INTO Usuarios (NombreCompleto, NombreUsuario, PasswordHash) VALUES (@FullName, @Username, @PasswordHash)";
                using (var insertCommand = new SqlCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@FullName", fullName);
                    insertCommand.Parameters.AddWithValue("@Username", username);
                    insertCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    int rowsAffected = insertCommand.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public List<Documento> GetAllDocuments()
        {
            var resultados = new List<Documento>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, NombreArchivo, FechaCarga FROM Documentos ORDER BY FechaCarga DESC";
                using (var command = new SqlCommand(query, connection))
                {
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
                    if (result != null)
                    {
                        filePath = result.ToString();
                    }
                }

                if (filePath == null)
                {
                    throw new Exception("No se encontró el documento en la base de datos.");
                }

                var deleteQuery = "DELETE FROM Documentos WHERE Id = @Id";
                using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@Id", documentId);
                    deleteCommand.ExecuteNonQuery();
                }

                return filePath;
            }
        }
    }
}
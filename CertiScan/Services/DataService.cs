using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using CertiScan.Models;

namespace CertiScan.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Server=ALONDRA\\SQLEXPRESS;Database=CertiScanDB;Trusted_Connection=True;TrustServerCertificate=True;";

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

        // --- MÉTODO NUEVO AÑADIDO PARA ELIMINAR ---
        public string DeleteDocument(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string filePath = null;

                // 1. Primero, obtenemos la ruta física del archivo para poder borrarlo del disco.
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
                    // Si no se encuentra, lanzamos una excepción controlada.
                    throw new Exception("No se encontró el documento en la base de datos.");
                }

                // 2. Después, borramos el registro de la base de datos.
                var deleteQuery = "DELETE FROM Documentos WHERE Id = @Id";
                using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@Id", documentId);
                    deleteCommand.ExecuteNonQuery();
                }

                // Devolvemos la ruta del archivo para que el ViewModel lo borre del disco.
                return filePath;
            }
        }
    }
}
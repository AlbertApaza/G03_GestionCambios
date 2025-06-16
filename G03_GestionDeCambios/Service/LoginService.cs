using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using G03_GestionDeCambios.Models;
using System.IO; // Para Path
using Renci.SshNet; // Para SSH.NET

namespace G03_GestionDeCambios.Service
{
    public class LoginService
    {
        private const string VPS_PHOTO_BASE_URL = "https://tunelvps.sytes.net/rup_manager_uploads/fotos"; // URL base para visualización

        public int Login(string email, string contrasena)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    var user = _dbContext.tbUsuarios
                        .FirstOrDefault(u => u.email == email &&
                                             u.contrasena == contrasena &&
                                             u.metodo_registro == "Credenciales" &&
                                             u.estado == 1);
                    return user?.idUsuario ?? 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en LoginService.Login: " + ex.Message);
                return 0;
            }
        }

        public tbUsuarios ObtenerOCrearUsuarioGoogle(string email, string nombrePila, string apellido, string fotoUrl)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    var usuarioExistente = _dbContext.tbUsuarios.FirstOrDefault(u => u.email == email);
                    if (usuarioExistente != null)
                    {
                        bool changed = false;
                        if (usuarioExistente.metodo_registro != "Google")
                        {
                            usuarioExistente.metodo_registro = "Google";
                            usuarioExistente.contrasena = null;
                            changed = true;
                        }
                        // Para usuarios de Google, fotoUrl es la URL completa de Google
                        if ((string.IsNullOrWhiteSpace(usuarioExistente.foto_perfil) || usuarioExistente.foto_perfil != fotoUrl) && !string.IsNullOrEmpty(fotoUrl))
                        {
                            usuarioExistente.foto_perfil = fotoUrl; // Guardamos la URL de Google directamente
                            changed = true;
                        }
                        if (string.IsNullOrWhiteSpace(usuarioExistente.nombre) && !string.IsNullOrWhiteSpace(nombrePila))
                        {
                            usuarioExistente.nombre = nombrePila;
                            changed = true;
                        }
                        else if (!string.IsNullOrWhiteSpace(nombrePila) && usuarioExistente.nombre != nombrePila)
                        {
                            usuarioExistente.nombre = nombrePila;
                            changed = true;
                        }
                        if (string.IsNullOrWhiteSpace(usuarioExistente.apellido) && !string.IsNullOrWhiteSpace(apellido))
                        {
                            usuarioExistente.apellido = apellido;
                            changed = true;
                        }
                        else if (!string.IsNullOrWhiteSpace(apellido) && usuarioExistente.apellido != apellido)
                        {
                            usuarioExistente.apellido = apellido;
                            changed = true;
                        }
                        if (changed)
                        {
                            _dbContext.SaveChanges();
                        }
                        return usuarioExistente;
                    }
                    else
                    {
                        string usernameBase = email.Split('@')[0].Replace(".", "").Replace("-", "");
                        string username = usernameBase;
                        int count = 1;
                        while (_dbContext.tbUsuarios.Any(u => u.usuario == username))
                        {
                            username = usernameBase + count++;
                        }
                        var nuevoUsuario = new tbUsuarios
                        {
                            usuario = username,
                            email = email,
                            nombre = nombrePila,
                            apellido = apellido,
                            contrasena = null,
                            metodo_registro = "Google",
                            foto_perfil = fotoUrl, // Guardamos la URL de Google directamente
                            fechaCreacion = DateTime.Now.Date,
                            estado = 1
                        };
                        _dbContext.tbUsuarios.Add(nuevoUsuario);
                        _dbContext.SaveChanges();
                        return nuevoUsuario;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en LoginService.ObtenerOCrearUsuarioGoogle: " + ex.Message);
                if (ex.InnerException != null) Debug.WriteLine("Inner Exception: " + ex.InnerException.Message);
                return null;
            }
        }

        public tbUsuarios ObtenerUsuarioPorId(int idUsuario)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbUsuarios.FirstOrDefault(u => u.idUsuario == idUsuario && u.estado == 1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error obteniendo usuario por ID {idUsuario}: {ex.Message}");
                return null;
            }
        }

        public bool EmailExists(string email)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbUsuarios.Any(u => u.email == email);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error verificando email {email}: {ex.Message}");
                return true;
            }
        }

        public bool RegisterUser(string nombre, string apellido, string email, string contrasena)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    if (_dbContext.tbUsuarios.Any(u => u.email == email))
                    {
                        return false;
                    }
                    string usernameBase = email.Split('@')[0].Replace(".", "").Replace("-", "");
                    string username = usernameBase;
                    int count = 1;
                    while (_dbContext.tbUsuarios.Any(u => u.usuario == username))
                    {
                        username = usernameBase + count++;
                    }
                    var nuevoUsuario = new tbUsuarios
                    {
                        usuario = username,
                        nombre = nombre,
                        apellido = apellido,
                        email = email,
                        contrasena = contrasena,
                        metodo_registro = "Credenciales",
                        fechaCreacion = DateTime.Now.Date,
                        estado = 1,
                        foto_perfil = null // Para credenciales, empieza sin foto o con un nombre de archivo por defecto si prefieres
                    };
                    _dbContext.tbUsuarios.Add(nuevoUsuario);
                    _dbContext.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en LoginService.RegisterUser: " + ex.Message);
                if (ex.InnerException != null) Debug.WriteLine("Inner Exception: " + ex.InnerException.Message);
                return false;
            }
        }

        public int ContarProyectosDeUsuario(int idUsuario)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbProyectoUsuario.Count(pu => pu.idUsuario == idUsuario);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error contando proyectos del usuario {idUsuario}: {ex.Message}");
                return 0;
            }
        }

        public bool ActualizarPerfilUsuario(int idUsuario, string nombrePila, string apellido, string nombreUsuarioEditable, string nuevaContrasena, string linkFotoPerfil, HttpPostedFileBase fotoSubida, string metodoRegistro)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    var usuario = _dbContext.tbUsuarios.FirstOrDefault(u => u.idUsuario == idUsuario && u.estado == 1);
                    if (usuario == null) return false;

                    bool changed = false;

                    if (!string.IsNullOrWhiteSpace(nombrePila) && usuario.nombre != nombrePila)
                    {
                        usuario.nombre = nombrePila;
                        changed = true;
                    }
                    if (usuario.apellido != apellido)
                    {
                        usuario.apellido = string.IsNullOrWhiteSpace(apellido) ? null : apellido;
                        changed = true;
                    }
                    if (!string.IsNullOrWhiteSpace(nombreUsuarioEditable) && usuario.usuario != nombreUsuarioEditable)
                    {
                        if (_dbContext.tbUsuarios.Any(u => u.usuario == nombreUsuarioEditable && u.idUsuario != idUsuario))
                        {
                            throw new InvalidOperationException("El nuevo nombre de usuario ya está en uso.");
                        }
                        usuario.usuario = nombreUsuarioEditable;
                        changed = true;
                    }

                    if (!string.IsNullOrWhiteSpace(nuevaContrasena) && metodoRegistro == "Credenciales")
                    {
                        usuario.contrasena = nuevaContrasena;
                        changed = true;
                    }

                    string nombreArchivoFotoParaDb = usuario.foto_perfil; // Mantiene la actual si no hay cambios
                                                                          // Si es de Google, esto será una URL completa. Si es subida, será un nombre de archivo.

                    if (fotoSubida != null && fotoSubida.ContentLength > 0)
                    {
                        string vpsHost = "161.132.38.250";
                        string vpsUsername = "root";
                        string vpsPassword = "patitochera123";
                        string remotePhotoFolderOnVps = "/var/www/html/rup_manager_uploads/fotos"; // Carpeta donde van las fotos

                        var fileExtension = Path.GetExtension(fotoSubida.FileName);
                        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                        // Corregir la ruta de subida al VPS para que esté DENTRO de la carpeta 'fotos'
                        string remoteFilePathOnVps = Path.Combine(remotePhotoFolderOnVps, uniqueFileName).Replace("\\", "/");

                        try
                        {
                            using (var client = new SftpClient(vpsHost, vpsUsername, vpsPassword))
                            {
                                client.Connect();
                                if (!client.Exists(remotePhotoFolderOnVps)) // Asegurar que la carpeta 'fotos' exista
                                {
                                    client.CreateDirectory(remotePhotoFolderOnVps);
                                }
                                using (var memoryStream = new MemoryStream())
                                {
                                    fotoSubida.InputStream.CopyTo(memoryStream);
                                    memoryStream.Position = 0;
                                    client.UploadFile(memoryStream, remoteFilePathOnVps, true);
                                }
                                client.Disconnect();
                            }
                            nombreArchivoFotoParaDb = uniqueFileName; // Guardar solo el nombre del archivo en la BD
                        }
                        catch (Exception exSftp)
                        {
                            Debug.WriteLine($"Error subiendo archivo a VPS: {exSftp.Message}");
                            throw new InvalidOperationException("Error al subir la foto de perfil al servidor.", exSftp);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(linkFotoPerfil))
                    {
                        nombreArchivoFotoParaDb = linkFotoPerfil; // Si es un link externo, guardamos el link
                    }

                    if (usuario.foto_perfil != nombreArchivoFotoParaDb)
                    {
                        usuario.foto_perfil = nombreArchivoFotoParaDb;
                        changed = true;
                    }

                    if (changed)
                    {
                        _dbContext.SaveChanges();
                    }
                    return true;
                }
            }
            catch (InvalidOperationException exOp)
            {
                Debug.WriteLine($"Error de operación actualizando perfil: {exOp.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando perfil del usuario {idUsuario}: {ex.Message}");
                if (ex.InnerException != null) Debug.WriteLine($"Inner Ex: {ex.InnerException.Message}");
                return false;
            }
        }

        public static string GetFullPhotoUrl(string photoFileNameOrUrl)
        {
            if (string.IsNullOrWhiteSpace(photoFileNameOrUrl))
            {
                return "https://w7.pngwing.com/pngs/708/467/png-transparent-avatar-default-head-person-unknown-user-anonym-user-pictures-icon-thumbnail.png"; // Default general
            }
            // Si ya es una URL completa (ej. de Google o un link ingresado)
            if (photoFileNameOrUrl.StartsWith("http://") || photoFileNameOrUrl.StartsWith("https://"))
            {
                return photoFileNameOrUrl;
            }
            // Si es solo un nombre de archivo, construir la URL del VPS
            return $"{VPS_PHOTO_BASE_URL}/{photoFileNameOrUrl}";
        }
    }
}
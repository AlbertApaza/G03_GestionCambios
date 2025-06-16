using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using G03_GestionDeCambios.Models;
using Renci.SshNet;
using System.Diagnostics;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;

namespace G03_GestionDeCambios.Service
{
    public class DocumentoService : IDisposable
    {
        private readonly BD_GestionDeCambiosEntities _dbContext;
        private const string VPS_DOCUMENT_BASE_URL = "https://tunelvps.sytes.net/rup_manager_uploads/documentos";
        private const string VPS_BASE_UPLOAD_PATH = "/var/www/html/rup_manager_uploads/documentos";

        public DocumentoService()
        {
            _dbContext = new BD_GestionDeCambiosEntities();
        }

        public tbProyectos GetProyectoConCicloActual(int idProyecto)
        {
            return _dbContext.tbProyectos
                             .Include(p => p.tbCiclos)
                             .FirstOrDefault(p => p.idProyecto == idProyecto);
        }

        public List<DocumentoInfoViewModel> GetDocumentosPorProyectoYCiclo(int idProyecto, string codCiclo)
        {
            if (string.IsNullOrWhiteSpace(codCiclo))
            {
                return new List<DocumentoInfoViewModel>();
            }
            string nombreProyecto = GetNombreProyecto(idProyecto);
            string nombreProyectoNormalizado = GetNombreNormalizado(nombreProyecto);
            string codCicloNormalizado = GetNombreNormalizado(codCiclo);

            return _dbContext.tbDocumentos
                .Where(d => d.idProyecto == idProyecto && d.codCiclo == codCiclo)
                .Include(d => d.tbUsuarios)
                .OrderByDescending(d => d.fechaSubida)
                .ToList()
                .Select(d => new DocumentoInfoViewModel
                {
                    IdDocumento = d.idDocumento,
                    NombreArchivoOriginal = d.nombreArchivo,
                    NombreArchivoEnServidor = d.rutaArchivo,
                    Version = d.version,
                    Estado = d.estado,
                    FechaSubida = d.fechaSubida ?? DateTime.MinValue,
                    Comentarios = d.comentarios,
                    NombreUsuarioSubida = (d.tbUsuarios != null) ? $"{d.tbUsuarios.nombre} {d.tbUsuarios.apellido}".Trim() : "N/A",
                    UrlDescarga = GetFullDocumentUrl(d.rutaArchivo, nombreProyectoNormalizado, codCicloNormalizado)
                })
                .ToList();
        }

        private string GetNombreProyecto(int idProyecto)
        {
            var proyecto = _dbContext.tbProyectos.Find(idProyecto);
            return proyecto?.nombre ?? "Proyecto_Desconocido";
        }

        private string GetNombreNormalizado(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "sin_nombre";
            string nombreNormalizado = System.Text.RegularExpressions.Regex.Replace(nombre.ToLowerInvariant(), @"\s+", "_");
            nombreNormalizado = System.Text.RegularExpressions.Regex.Replace(nombreNormalizado, @"[^a-z0-9_]+", "");
            nombreNormalizado = nombreNormalizado.Replace(".", "");
            return nombreNormalizado.Length > 50 ? nombreNormalizado.Substring(0, 50) : nombreNormalizado;
        }

        public bool SubirDocumento(int idProyecto, string codCiclo, HttpPostedFileBase archivo, string version, string comentarios, int idUsuarioSubida, string nombreProyecto)
        {
            if (archivo == null || archivo.ContentLength == 0 || string.IsNullOrWhiteSpace(codCiclo))
            {
                return false;
            }
            string nombreProyectoNormalizado = GetNombreNormalizado(nombreProyecto);
            string cicloNormalizado = GetNombreNormalizado(codCiclo);
            string originalFileName = Path.GetFileName(archivo.FileName);
            string fileExtension = Path.GetExtension(originalFileName);
            string uniqueFileNameWithoutPath = $"{Guid.NewGuid()}{fileExtension}";
            string remoteProjectFolder = Path.Combine(VPS_BASE_UPLOAD_PATH, nombreProyectoNormalizado).Replace("\\", "/");
            string remoteCicleFolder = Path.Combine(remoteProjectFolder, cicloNormalizado).Replace("\\", "/");
            string remoteFilePathOnVps = Path.Combine(remoteCicleFolder, uniqueFileNameWithoutPath).Replace("\\", "/");
            string vpsHost = "161.132.38.250";
            string vpsUsername = "root";
            string vpsPassword = "patitochera123";

            try
            {
                using (var client = new SftpClient(vpsHost, vpsUsername, vpsPassword))
                {
                    client.Connect();
                    if (!client.Exists(VPS_BASE_UPLOAD_PATH)) client.CreateDirectory(VPS_BASE_UPLOAD_PATH);
                    if (!client.Exists(remoteProjectFolder)) client.CreateDirectory(remoteProjectFolder);
                    if (!client.Exists(remoteCicleFolder)) client.CreateDirectory(remoteCicleFolder);
                    using (var memoryStream = new MemoryStream())
                    {
                        archivo.InputStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        client.UploadFile(memoryStream, remoteFilePathOnVps, true);
                    }
                    client.Disconnect();
                }
            }
            catch (Exception exSftp)
            {
                Debug.WriteLine($"Error subiendo archivo a VPS: {exSftp.ToString()}");
                throw;
            }

            try
            {
                // ASUMIENDO QUE idDocumento ES IDENTITY AHORA. SI NO, NECESITAS TU LÓGICA DE nextIdDocumento.
                var nuevoDocumento = new tbDocumentos
                {
                    // idDocumento = nextIdDocumento, // Comentado si es IDENTITY
                    idProyecto = idProyecto,
                    codCiclo = codCiclo,
                    nombreArchivo = originalFileName.Length > 50 ? originalFileName.Substring(0, 50) : originalFileName,
                    rutaArchivo = uniqueFileNameWithoutPath,
                    version = version?.Length > 10 ? version.Substring(0, 10) : version,
                    comentarios = comentarios,
                    estado = "Pendiente",
                    fechaSubida = DateTime.Now,
                    idUsuarioSubida = idUsuarioSubida
                };
                _dbContext.tbDocumentos.Add(nuevoDocumento);
                _dbContext.SaveChanges();
                return true;
            }
            catch (DbEntityValidationException dbEx)
            {
                Debug.WriteLine("DbEntityValidationException al guardar documento:");
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        Debug.WriteLine($"Propiedad: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                    }
                }
                throw;
            }
            catch (SqlException sqlEx)
            {
                Debug.WriteLine($"SqlException al guardar documento: {sqlEx.Message}");
                Debug.WriteLine($"Número de error SQL: {sqlEx.Number}");
                Debug.WriteLine($"StackTrace: {sqlEx.ToString()}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error general al guardar documento en BD: {ex.ToString()}");
                throw;
            }
        }

        public static string GetFullDocumentUrl(string nombreArchivoEnServidor, string nombreProyectoNormalizado, string codCicloNormalizado)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivoEnServidor) ||
                string.IsNullOrWhiteSpace(nombreProyectoNormalizado) ||
                string.IsNullOrWhiteSpace(codCicloNormalizado))
            {
                return "#";
            }
            string safeNombreProyecto = Uri.EscapeDataString(nombreProyectoNormalizado);
            string safeCodCiclo = Uri.EscapeDataString(codCicloNormalizado);
            string safeNombreArchivo = Uri.EscapeDataString(nombreArchivoEnServidor);
            return $"{VPS_DOCUMENT_BASE_URL}/{safeNombreProyecto}/{safeCodCiclo}/{safeNombreArchivo}";
        }

        public tbDocumentos GetDocumentoParaDescarga(int idDocumento, out string rutaCompletaEnServidorVps, out string nombreOriginalParaCliente)
        {
            rutaCompletaEnServidorVps = null;
            nombreOriginalParaCliente = null;
            var docDb = _dbContext.tbDocumentos
                                .Include(d => d.tbProyectos)
                                .FirstOrDefault(d => d.idDocumento == idDocumento);
            if (docDb == null || docDb.tbProyectos == null || string.IsNullOrWhiteSpace(docDb.codCiclo) || string.IsNullOrWhiteSpace(docDb.rutaArchivo))
            {
                return null;
            }
            string nombreProyectoNormalizado = GetNombreNormalizado(docDb.tbProyectos.nombre);
            string cicloNormalizado = GetNombreNormalizado(docDb.codCiclo);
            rutaCompletaEnServidorVps = Path.Combine(VPS_BASE_UPLOAD_PATH, nombreProyectoNormalizado, cicloNormalizado, docDb.rutaArchivo).Replace("\\", "/");
            nombreOriginalParaCliente = docDb.nombreArchivo;
            return docDb;
        }

        public bool EliminarDocumento(int idDocumento, out string mensajeError)
        {
            mensajeError = string.Empty;
            var documento = _dbContext.tbDocumentos
                                      .Include(d => d.tbProyectos)
                                      .FirstOrDefault(d => d.idDocumento == idDocumento);

            if (documento == null)
            {
                mensajeError = "Documento no encontrado en la base de datos.";
                return false;
            }

            if (documento.tbProyectos == null || string.IsNullOrWhiteSpace(documento.codCiclo) || string.IsNullOrWhiteSpace(documento.rutaArchivo))
            {
                mensajeError = "Información del documento incompleta para la eliminación del archivo físico.";
                return false;
            }

            string nombreProyectoNormalizado = GetNombreNormalizado(documento.tbProyectos.nombre);
            string cicloNormalizado = GetNombreNormalizado(documento.codCiclo);
            string nombreArchivoEnServidor = documento.rutaArchivo;
            string remoteFilePathOnVps = Path.Combine(VPS_BASE_UPLOAD_PATH, nombreProyectoNormalizado, cicloNormalizado, nombreArchivoEnServidor).Replace("\\", "/");

            string vpsHost = "161.132.38.250";
            string vpsUsername = "root";
            string vpsPassword = "patitochera123";

            try
            {
                using (var client = new SftpClient(vpsHost, vpsUsername, vpsPassword))
                {
                    client.Connect();
                    if (client.Exists(remoteFilePathOnVps))
                    {
                        client.DeleteFile(remoteFilePathOnVps);
                        Debug.WriteLine($"Archivo {remoteFilePathOnVps} eliminado del VPS.");
                    }
                    else
                    {
                        Debug.WriteLine($"Archivo {remoteFilePathOnVps} no encontrado en el VPS, procediendo a eliminar de BD.");
                    }
                    client.Disconnect();
                }
            }
            catch (Exception exSftp)
            {
                Debug.WriteLine($"Error eliminando archivo del VPS: {exSftp.ToString()}");
                mensajeError = "Error al eliminar el archivo del servidor remoto. El registro en la base de datos no fue eliminado.";
                return false;
            }

            try
            {
                _dbContext.tbDocumentos.Remove(documento);
                _dbContext.SaveChanges();
                Debug.WriteLine($"Registro del documento {idDocumento} eliminado de la BD.");
                return true;
            }
            catch (Exception exDb)
            {
                Debug.WriteLine($"Error eliminando registro del documento de la BD: {exDb.ToString()}");
                mensajeError = "El archivo fue eliminado del servidor, pero ocurrió un error al eliminar el registro de la base de datos.";
                return false;
            }
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
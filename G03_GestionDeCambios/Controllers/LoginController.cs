using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using G03_GestionDeCambios.Models;
using Newtonsoft.Json.Linq;
using G03_GestionDeCambios.Service;

namespace G03_GestionDeCambios.Controllers
{
    public class LoginController : Controller
    {
        private readonly LoginService _loginService = new LoginService();
        private readonly string googleClientId = ConfigurationManager.AppSettings["GoogleClientId"];
        private readonly string googleClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];
        private readonly string googleRedirectUri = ConfigurationManager.AppSettings["GoogleRedirectUri"];

        public ActionResult Index(string returnUrl)
        {
            if (Session["idUsuario"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            if (TempData["GoogleLoginError"] != null)
            {
                ModelState.AddModelError("", TempData["GoogleLoginError"].ToString());
            }
            if (TempData["Message"] != null)
            {
                ViewBag.GeneralMessage = TempData["Message"].ToString();
            }
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
            }
            if (TempData["ErrorMessage"] != null)
            {
                ModelState.AddModelError("", TempData["ErrorMessage"].ToString());
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View("Index", model);
            }

            var idUsuario = _loginService.Login(model.Email, model.Contrasena);

            if (idUsuario > 0)
            {
                var usuarioDb = _loginService.ObtenerUsuarioPorId(idUsuario);
                if (usuarioDb != null)
                {
                    SetUserSession(usuarioDb);
                    TempData["Message"] = "Has iniciado sesión correctamente.";

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Error al obtener datos del usuario después del login.");
                }
            }
            else
            {
                using (var db = new BD_GestionDeCambiosEntities())
                {
                    var existingUser = db.tbUsuarios.FirstOrDefault(u => u.email == model.Email);
                    if (existingUser != null && existingUser.metodo_registro == "Google")
                    {
                        ModelState.AddModelError("", "Esta cuenta fue registrada con Google. Por favor, inicia sesión usando el botón de Google.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Correo electrónico o contraseña incorrectos, o la cuenta no está activa.");
                    }
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View("Index", model);
        }

        public ActionResult GoogleLogin(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl))
            {
                TempData["ReturnUrlGoogle"] = returnUrl;
            }

            if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret) || string.IsNullOrEmpty(googleRedirectUri))
            {
                TempData["GoogleLoginError"] = "Error de configuración de la aplicación para el inicio de sesión con Google. Contacte al administrador.";
                Debug.WriteLine("Google OAuth: Faltan ClientID, ClientSecret o RedirectUri en Web.config.");
                return RedirectToAction("Index", new { returnUrl });
            }

            string url = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"scope=openid%20email%20profile&" +
                         $"response_type=code&" +
                         $"redirect_uri={Uri.EscapeDataString(googleRedirectUri)}&" +
                         $"client_id={googleClientId}&" +
                         $"access_type=online&" +
                         $"prompt=select_account";

            return Redirect(url);
        }

        public ActionResult GoogleCallback(string code, string error, string state)
        {
            string returnUrl = TempData["ReturnUrlGoogle"] as string;

            if (!string.IsNullOrEmpty(error))
            {
                TempData["GoogleLoginError"] = $"Error de Google: {error}. Por favor, inténtalo de nuevo.";
                Debug.WriteLine($"Google Auth Error en Callback: {error}");
                return RedirectToAction("Index", new { returnUrl });
            }
            if (string.IsNullOrEmpty(code))
            {
                TempData["GoogleLoginError"] = "No se recibió el código de autorización de Google. Por favor, inténtalo de nuevo.";
                Debug.WriteLine("Google Auth Error: Código no recibido en Callback.");
                return RedirectToAction("Index", new { returnUrl });
            }

            string googleUserEmail, googleUserPicture, googleUserGivenName, googleUserFamilyName;

            try
            {
                using (var webClient = new WebClient())
                {
                    var values = new System.Collections.Specialized.NameValueCollection();
                    values["code"] = code;
                    values["client_id"] = googleClientId;
                    values["client_secret"] = googleClientSecret;
                    values["redirect_uri"] = googleRedirectUri;
                    values["grant_type"] = "authorization_code";

                    byte[] responseBytes = webClient.UploadValues("https://oauth2.googleapis.com/token", "POST", values);
                    string responseString = Encoding.UTF8.GetString(responseBytes);
                    JObject jsonToken = JObject.Parse(responseString);

                    string accessToken = jsonToken["access_token"]?.ToString();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        string tokenError = jsonToken["error"]?.ToString();
                        string tokenErrorDesc = jsonToken["error_description"]?.ToString();
                        TempData["GoogleLoginError"] = $"No se pudo obtener el token de acceso de Google. {tokenErrorDesc}";
                        Debug.WriteLine($"Google Token Error: {tokenError} - {tokenErrorDesc}. Response: {responseString}");
                        return RedirectToAction("Index", new { returnUrl });
                    }

                    webClient.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + accessToken);
                    string userInfoString = webClient.DownloadString("https://www.googleapis.com/oauth2/v3/userinfo");
                    JObject userInfoJson = JObject.Parse(userInfoString);

                    googleUserEmail = userInfoJson["email"]?.ToString();
                    googleUserPicture = userInfoJson["picture"]?.ToString();
                    googleUserGivenName = userInfoJson["given_name"]?.ToString();
                    googleUserFamilyName = userInfoJson["family_name"]?.ToString();

                    if (string.IsNullOrEmpty(googleUserEmail))
                    {
                        TempData["GoogleLoginError"] = "No se pudo obtener el correo electrónico de Google.";
                        return RedirectToAction("Index", new { returnUrl });
                    }
                }
            }
            catch (WebException webEx)
            {
                string responseFromServer = "No response from server.";
                if (webEx.Response != null)
                {
                    try
                    {
                        using (var errorResponse = (HttpWebResponse)webEx.Response)
                        using (var readerEx = new System.IO.StreamReader(errorResponse.GetResponseStream()))
                        {
                            responseFromServer = readerEx.ReadToEnd();
                        }
                        Debug.WriteLine($"Web Error during Google Callback: {webEx.Message}. Status: {((HttpWebResponse)webEx.Response).StatusCode}. Server Response: {responseFromServer}");
                    }
                    catch (Exception exRead)
                    {
                        responseFromServer = "Error reading response stream: " + exRead.Message;
                        Debug.WriteLine($"Web Error (reading response stream) during Google Callback: {webEx.Message}. Original Server Response: {responseFromServer}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Web Error (no response) during Google Callback: {webEx.Message}. Status: {webEx.Status}");
                }
                TempData["GoogleLoginError"] = $"Error de comunicación con Google. Por favor, inténtalo más tarde.";
                return RedirectToAction("Index", new { returnUrl });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General Error during Google Callback: {ex.ToString()}");
                TempData["GoogleLoginError"] = "Ocurrió un error inesperado procesando la información de Google.";
                return RedirectToAction("Index", new { returnUrl });
            }

            var usuarioApp = _loginService.ObtenerOCrearUsuarioGoogle(googleUserEmail, googleUserGivenName, googleUserFamilyName, googleUserPicture);

            if (usuarioApp != null && usuarioApp.estado == 1)
            {
                SetUserSession(usuarioApp);
                TempData["Message"] = "Has iniciado sesión correctamente con Google.";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            else if (usuarioApp != null && usuarioApp.estado != 1)
            {
                TempData["GoogleLoginError"] = "Tu cuenta está inactiva. Contacta al administrador.";
                return RedirectToAction("Index", new { returnUrl });
            }
            else
            {
                TempData["GoogleLoginError"] = "No se pudo procesar tu inicio de sesión con Google en la aplicación.";
                return RedirectToAction("Index", new { returnUrl });
            }
        }

        private void SetUserSession(tbUsuarios user)
        {
            Session["idUsuario"] = user.idUsuario;
            Session["usuario"] = user.usuario;
            Session["nombreCompleto"] = $"{user.nombre} {user.apellido}".Trim();
            Session["emailUsuario"] = user.email;
            Session["fotoPerfil"] = string.IsNullOrWhiteSpace(user.foto_perfil) ? "https://w7.pngwing.com/pngs/708/467/png-transparent-avatar-default-head-person-unknown-user-anonym-user-pictures-icon-thumbnail.png" : user.foto_perfil;
            Session["metodoRegistro"] = user.metodo_registro;
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                Response.Cookies["ASP.NET_SessionId"].Value = string.Empty;
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddMonths(-20);
            }
            TempData["Message"] = "Has cerrado sesión correctamente.";
            return RedirectToAction("Index", "Login");
        }

        public ActionResult Register()
        {
            if (Session["idUsuario"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (Session["idUsuario"] != null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (_loginService.EmailExists(model.Email))
            {
                ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
            }

            if (ModelState.IsValid)
            {
                bool registrationSuccess = _loginService.RegisterUser(model.Nombre, model.Apellido, model.Email, model.Contrasena);
                if (registrationSuccess)
                {
                    TempData["SuccessMessage"] = "¡Registro exitoso! Ahora puedes iniciar sesión.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Ocurrió un error durante el registro. Por favor, inténtalo de nuevo.");
                }
            }

            return View(model);
        }
    }
}
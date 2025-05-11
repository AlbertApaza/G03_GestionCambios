using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AppGCS.Models;
using MySql.Data.MySqlClient;

namespace AppGCS.Controllers
{
    public class SesionController : Controller
    {
        // GET: Sesion
        public ActionResult Index()
        {
            if (Session["Usuario"] == null)
            {
                return RedirectToAction("Login", "Sesion");
            }
            return View();
        }
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string usuario, string contrasena)
        {
            ClsUsuario user = null;
            string cadena = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (MySqlConnection conn = new MySqlConnection(cadena))
            {
                conn.Open();
                string query = "SELECT * FROM usuario WHERE Usuario = @usuario AND Contrasena = @contrasena AND Estado = 1";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    cmd.Parameters.AddWithValue("@contrasena", contrasena);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new ClsUsuario
                            {
                                IdUsuario = reader["IdUsuario"] != DBNull.Value ? Convert.ToInt32(reader["IdUsuario"]) : 0,
                                Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString() : "",
                                Contrasena = reader["Contrasena"] != DBNull.Value ? reader["Contrasena"].ToString() : "",
                                IdRol = reader["IdRol"] != DBNull.Value ? Convert.ToInt32(reader["IdRol"]) : 0,
                                Estado = reader["Estado"] != DBNull.Value ? Convert.ToBoolean(reader["Estado"]) : false
                            };
                        }
                    }
                }
            }

            if (user != null)
            {
                Session["Usuario"] = user.Usuario;
                Session["Rol"] = user.IdRol;
                return RedirectToAction("Index", "Home"); // o a la vista deseada
            }

            ViewBag.Mensaje = "Usuario o contraseña incorrectos.";
            return View();
        }

        public ActionResult CerrarSesion()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
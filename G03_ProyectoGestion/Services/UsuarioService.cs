using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using G03_ProyectoGestion.Models;

namespace G03_ProyectoGestion.Services
{
    public class UsuarioService
    {
        private readonly g03_databaseEntities _dbContext;
        public UsuarioService()
        {
            _dbContext = new g03_databaseEntities();
        }
        public tbUsuarios Login(tbUsuarios usuario)
        {
            return _dbContext.tbUsuarios.FirstOrDefault(u =>
                u.nombreUsuario == usuario.nombreUsuario &&
                u.contrasena == usuario.contrasena);
        }


        public void Registro(tbUsuarios usuario)
        {
            _dbContext.tbUsuarios.Add(usuario);
            _dbContext.SaveChanges();
        }
    }
}
using ApiOAuthEmpleados.Helpers;
using ApiOAuthEmpleados.Models;
using ApiOAuthEmpleados.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ApiOAuthEmpleados.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private RepositoryHospital repo;
        private HelperActionOAuthService helper;
        public AuthController(RepositoryHospital repo
            , HelperActionOAuthService helper)
        {
            this.repo = repo;
            this.helper = helper;
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult>
            Login(LoginModel model)
        {
            Empleado empleado = await
               this.repo.LogInEmpleadoAsync
               (model.UserName, int.Parse(model.Password));
            if (empleado == null)
            {
                return Unauthorized();
            }
            else
            {
                //DEBEMOS CREAR UNAS CREDENCIALES CON NUESTRO 
                //TOKEN
                SigningCredentials credentials =
                    new SigningCredentials
                    (this.helper.GetKeyToken(),
                    SecurityAlgorithms.HmacSha256);

                //CREAMOS NUESTRO MODELO PARA ALMACENARLO EN 
                //EL TOKEN
                EmpleadoModel modelEmp = new EmpleadoModel
                {
                    IdEmpleado = empleado.IdEmpleado,
                    Apellido = empleado.Apellido,
                    Oficio = empleado.Oficio,
                    Salario = empleado.Salario,
                    IdDepartamento = empleado.IdDepartamento
                };

                string jsonEmpleado =
                    JsonConvert.SerializeObject(modelEmp);
                string jsonCypher =
                    HelperCryptography.CifrarString(jsonEmpleado);

                //CREAMOS UN ARRAY DE CLAIMS PARA EL TOKEN
                //AQUI ALMACENAMOS EL ROLE DEL USUARIO
                Claim[] informacion = new[]
                {
                    new Claim("UserData", jsonCypher),
                    new Claim(ClaimTypes.Role, empleado.Oficio)
                };

                //EL TOKEN SE GENERA CON UNA CLASE Y DEBEMOS 
                //ALMACENAR LOS DATOS DE ISSUER, CREDENTIALS...
                JwtSecurityToken token =
                    new JwtSecurityToken(
                        claims: informacion,
                        issuer: this.helper.Issuer,
                        audience: this.helper.Audience,
                        signingCredentials: credentials,
                        expires: DateTime.UtcNow.AddMinutes(20),
                        notBefore: DateTime.UtcNow
                        );
                //POR ULTIMO, DEVOLVEMOS LA RESPUESTA AFIRMATIVA 
                //CON EL TOKEN
                return Ok(new
                {
                    response = 
                    new JwtSecurityTokenHandler()
                    .WriteToken(token)
                });
            }
        }
    }
}

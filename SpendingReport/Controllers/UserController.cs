using SpendingReport.DataContext;
using SpendingReport.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Security;

namespace SpendingReport.Controllers
{
    public class UserController : ApiController
    {
        // GET api/user
        public IEnumerable<User> Get()
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                return connection.Get<User>();
            }
        }

        [Route("api/user/{userName}")]
        public User Get(string userName)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                return connection.GetSingle<User>
                (
                    new DbParameter("UserName", userName)
                );
            }
        }

        [Route("api/user")]
        public User GetByEmail(string emailAddress = null)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                return connection.GetSingle<User>
                (
                    new DbParameter("EmailAddress", emailAddress)
                );
            }
        }

        [HttpPut]
        [Route("api/user/{userName}")]
        public void Put(string userName, [FromBody]User value)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                User user = connection.GetSingle<User>
                (
                    new DbParameter("UserName", userName)
                );

                user.Put(value);
                connection.Put(user);
            }
        }

        [HttpDelete]
        [Route("api/user/{userName}")]
        public void Delete(string userName)
        {
        }
    }
}

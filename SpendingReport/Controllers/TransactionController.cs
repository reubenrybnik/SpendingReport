using SpendingReport.DataContext;
using SpendingReport.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SpendingReport.Controllers
{
    public class TransactionController : ApiController
    {
        private User GetApplicationUser(IDbConnection connection, string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }

            return connection.GetSingle<User>
            (
                new DbParameter("UserName", userName)
            );
        }

        private DbParameter GetApplicationUserParameter(IDbConnection connection, string userName)
        {
            User user = this.GetApplicationUser(connection, userName);

            if (user == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return new DbParameter("UserId", user.UserId);
        }

        [HttpGet]
        [Route("api/user/{userName}/transaction")]
        public IHttpActionResult Get(string userName, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (userName == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction[] transactions = connection.Get<Transaction>
                (
                    this.GetApplicationUserParameter(connection, userName),
                    new DbParameter("StartDate", startDate),
                    new DbParameter("EndDate", endDate)
                );

                return this.Json(transactions);
            }
        }

        [Route("api/user/{userName}/transaction/{transactionId:guid}")]
        public IHttpActionResult Get(string userName, Guid transactionId)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction transaction = this.GetById(connection, userName, transactionId);

                if (transaction == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }

                return this.Json(transaction);
            }
        }

        private Transaction GetById(IDbConnection connection, string userName, Guid transactionId)
        {
            return connection.GetSingle<Transaction>
            (
                this.GetApplicationUserParameter(connection, userName),
                new DbParameter("TransactionId", transactionId)
            );
        }

        [HttpPost]
        [Route("api/user/{userName}/transaction")]
        public IHttpActionResult Post(string userName, [FromBody]Transaction value)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                User user = this.GetApplicationUser(connection, userName);
                value.UserId = user.UserId;

                connection.Put(value);

                return this.Json(value);
                // TODO: see if I can find a way to return 201 (Created) with a Json value
                //return this.Created(string.Format("api/users/{0}/transaction/{1}", userName, value.TransactionId), value);
            }
        }

        [HttpPut]
        [Route("api/user/{userName}/transaction/{transactionId:guid}")]
        public IHttpActionResult Put(string userName, Guid transactionId, [FromBody]Transaction value)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction transaction = this.GetById(connection, userName, transactionId);

                if (transaction == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }

                transaction.Put(value);
                connection.Put(transaction);
                return this.Json(transaction);
            }
        }

        [HttpDelete]
        [Route("api/user/{userName}/transaction/{transactionId:guid}")]
        public IHttpActionResult Delete(string userName, Guid transactionId)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction transaction = this.GetById(connection, userName, transactionId);

                if (transaction == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }

                connection.Delete(transaction);

                return this.Ok();
            }
        }
    }
}

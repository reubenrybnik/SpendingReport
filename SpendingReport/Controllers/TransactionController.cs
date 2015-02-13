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
            return connection.GetSingle<User>
            (
                new DbParameter("UserName", userName)
            );
        }

        private DbParameter GetApplicationUserParameter(IDbConnection connection, string userName)
        {
            return new DbParameter("UserId", this.GetApplicationUser(connection, userName).UserId);
        }

        [Route("api/user/{userName}/transaction")]
        public IHttpActionResult Get(string userName)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction[] transactions = connection.Get<Transaction>
                (
                    this.GetApplicationUserParameter(connection, userName)
                );

                return this.Json<Transaction[]>(transactions);
            }
        }

        //[Route("api/user/{userName}/transaction?startDate={startDate:datetime}&endDate={endDate:datetime}")]
        //public IHttpActionResult Get([FromUri]string userName, [FromUri]DateTime startDate, [FromUri]DateTime endDate)
        //{
        //    using (IDbConnection connection = DbConnectionFactory.CreateConnection())
        //    {
        //        Transaction[] transactions = connection.Get<Transaction>
        //        (
        //            this.GetApplicationUserParameter(connection, userName),
        //            new DbParameter("StartDate", startDate),
        //            new DbParameter("EndDate", endDate)
        //        );

        //        return this.Json<Transaction[]>(transactions);
        //    }
        //}

        [Route("api/user/{userName}/transaction/{transactionId:guid}")]
        public IHttpActionResult Get(string userName, Guid transactionId)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction transaction = this.GetById(connection, userName, transactionId);
                return this.Json<Transaction>(transaction);
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

                return this.Created(new Uri(string.Format("api/users/{0}/transaction/{1}", userName, value.TransactionId)), value);
            }
        }

        [HttpPut]
        [Route("api/user/{userName}/transaction/{transactionId:guid}")]
        public IHttpActionResult Put(string userName, Guid transactionId, [FromBody]Transaction value)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction transaction = this.GetById(connection, userName, transactionId);

                transaction.Put(value);
                connection.Put(transaction);
                return this.Ok(transaction);
            }
        }

        [HttpDelete]
        [Route("api/user/{userName}/transaction/{transactionId:guid}")]
        public IHttpActionResult Delete(string userName, Guid transactionId)
        {
            return this.Ok();
        }
    }
}

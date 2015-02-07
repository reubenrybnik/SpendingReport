using SpendingReport.DataContext;
using SpendingReport.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace SpendingReport.Services
{
    public class SpendingService : ISpendingService
    {
        // TODO: authenticate users
        public User GetUser(string userName)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                return this.GetUser(userName, connection);
            }
        }

        private User GetUser(string userName, IDbConnection connection)
        {
            return connection.GetSingle<User>
            (
                new DbParameter("UserName", userName)
            );
        }

        public User GetUserByEmail(string emailAddress)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                return connection.GetSingle<User>
                (
                    new DbParameter("EmailAddress", emailAddress)
                );
            }
        }

        public void PutUser(string userName, User newUser)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                User user = this.GetUser(userName, connection);

                if (user != null)
                {
                    user.Put(newUser);
                }
                else
                {
                    user = newUser;
                }

                connection.Put(user);
            }
        }

        public void DeleteUser(string userName)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                User user = this.GetUser(userName, connection);

                if (user != null)
                {
                    connection.Delete(user);
                }
            }
        }

        public UserCategory GetUserCategory(string userName, string categoryName)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                return this.GetUserCategory(userName, categoryName, connection);
            }
        }

        private UserCategory GetUserCategory(string userName, string categoryName, IDbConnection connection)
        {
            User user = this.GetUser(userName, connection);

            return connection.GetSingle<UserCategory>
            (
                new DbParameter("UserId", user.Id),
                new DbParameter("Name", categoryName)
            );
        }

        public UserCategory[] GetUserCategories(string userName, string parentCategoryName)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                User user = this.GetUser(userName, connection);

                return connection.Get<UserCategory>
                (
                    new DbParameter("UserId", user.Id),
                    new DbParameter("ParentName", parentCategoryName)
                );
            }
        }


        public void PutUserCategory(string userName, string categoryName, UserCategory newUserCategory)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                UserCategory userCategory = this.GetUserCategory(userName, categoryName, connection);

                if (userCategory != null)
                {
                    userCategory.Put(newUserCategory);
                }
                else
                {
                    userCategory = newUserCategory;
                }

                connection.Put(userCategory);
            }
        }

        public void DeleteUserCategory(string userName, string categoryName)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                UserCategory userCategory = this.GetUserCategory(userName, categoryName, connection);

                if (userCategory != null)
                {
                    connection.Delete(userCategory);
                }
            }
        }

        public Transaction GetTransaction(string userName, string transactionId)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                return this.GetTransaction(userName, new Guid(transactionId), connection);
            }
        }

        private Transaction GetTransaction(string userName, Guid transactionId, IDbConnection connection)
        {
            User user = this.GetUser(userName, connection);

            return connection.GetSingle<Transaction>
            (
                new DbParameter("UserId", user.Id),
                new DbParameter("TransactionId", transactionId)
            );
        }

        public Transaction[] GetTransactions(string userName, string categoryName, string payeeName, string startDate, string endDate)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                User user = this.GetUser(userName, connection);

                return connection.Get<Transaction>
                (
                    new DbParameter("UserId", user.Id),
                    new DbParameter("CategoryName", categoryName),
                    new DbParameter("PayeeName", payeeName),
                    new DbParameter("StartDate", startDate),
                    new DbParameter("EndDate", endDate)
                );
            }
        }

        // TODO: I expect this isn't allowed, but I should check to see if I can post multiple transactions.
        public void PostTransaction(string userName, Transaction newTransaction)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction transaction = this.GetTransaction(userName, newTransaction.TransactionId, connection);

                if (transaction != null)
                {
                    transaction.Put(newTransaction);
                }
                else
                {
                    transaction = newTransaction;
                }

                connection.Put(transaction);
            }
        }

        public void DeleteTransaction(string userName, string transactionId)
        {
            using (IDbConnection connection = DbConnectionFactory.CreateConnection())
            {
                Transaction transaction = this.GetTransaction(userName, new Guid(transactionId), connection);

                if (transaction != null)
                {
                    connection.Delete(transaction);
                }
            }
        }
    }
}

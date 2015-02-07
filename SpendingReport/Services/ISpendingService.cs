using SpendingReport.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace SpendingReport.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ISpendingService" in both code and config file together.
    [ServiceContract]
    public interface ISpendingService
    {
        [OperationContract]
        [WebGet(UriTemplate = "users/{userName}")]
        User GetUser(string userName);

        [OperationContract]
        [WebGet(UriTemplate = "users?emailAddress={emailAddress}")]
        User GetUserByEmail(string emailAddress);

        [OperationContract]
        [WebInvoke(Method = "PUT", UriTemplate = "users/{userName}")]
        void PutUser(string userName, User newUser);

        [OperationContract]
        [WebInvoke(Method = "DELETE", UriTemplate = "users/{userName}")]
        void DeleteUser(string userName);

        [OperationContract]
        [WebGet(UriTemplate = "users/{userName}/categories/{categoryName}")]
        UserCategory GetUserCategory(string userName, string categoryName);

        [OperationContract]
        [WebGet(UriTemplate = "users/{userName}/categories?parentCategoryName={parentCategoryName}")]
        UserCategory[] GetUserCategories(string userName, string parentCategoryName);

        [OperationContract]
        [WebInvoke(Method = "PUT", UriTemplate = "users/{userName}/categories/{categoryName}")]
        void PutUserCategory(string userName, string categoryName, UserCategory newUserCategory);

        [OperationContract]
        [WebInvoke(Method = "DELETE", UriTemplate = "users/{userName}/categories/{categoryName}")]
        void DeleteUserCategory(string userName, string categoryName);

        [OperationContract]
        [WebGet(UriTemplate = "users/{userName}/transactions/{transactionId}")]
        Transaction GetTransaction(string userName, string transactionId);

        [OperationContract]
        [WebGet(UriTemplate = "users/{userName}/transactions?categoryName={categoryName}&payeeName={payeeName}&startDate={startDate}&endDate={endDate}")]
        Transaction[] GetTransactions(string userName, string categoryName, string payeeName, string startDate, string endDate);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "users/{userName}/transactions")]
        void PostTransaction(string userName, Transaction newTransaction);

        [OperationContract]
        [WebInvoke(Method = "DELETE", UriTemplate = "users/{userName}/transactions/{transactionId}")]
        void DeleteTransaction(string userName, string transactionId);
    }
}

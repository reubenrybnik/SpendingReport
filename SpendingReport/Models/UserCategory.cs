using SpendingReport.DataContext;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace SpendingReport.Models
{
    [Serializable]
    public sealed class UserCategory : DbEntity<UserCategory>
    {
        private static readonly IReadOnlyDictionary<DbOperation, DbOperationInfo> entityDbOperations = new ReadOnlyDictionary<DbOperation, DbOperationInfo>
        (
            new Dictionary<DbOperation, DbOperationInfo>()
            {
                {
                    DbOperation.Get,
                    new DbOperationInfo()
                    {
                        Procedure = "SP_UserCategories_Get"
                    }
                },
                {
                    DbOperation.Put,
                    new DbOperationInfo()
                    {
                        ParameterType = "UDT_UserCategories_Put",
                        Procedure = "SP_UserCategories_Put",
                        ParameterName = "UserCategories"
                    }
                },
                {
                    DbOperation.Delete,
                    new DbOperationInfo()
                    {
                        ParameterType = "UDT_UserCategories_Del",
                        Procedure = "SP_UserCategories_Del",
                        ParameterName = "UserCategories"
                    }
                }
            }
        );

        public override IReadOnlyDictionary<DbOperation, DbOperationInfo> DbOperations
        {
            get { return UserCategory.entityDbOperations; }
        }

        public long UserCategoryId
        {
            get;
            private set;
        }

        public long UserId
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            set;
        }

        public string ParentName
        {
            get;
            set;
        }

        public UserCategory[] ChildCategories
        {
            get
            {
                return this.DelayLoadEntitySet<UserCategory>
                (
                    "ChildCategories",
                    new DbParameter("ParentUserCategoryId", this.UserCategoryId)
                );
            }
        }

        // arguably something like this should be in the controller, but I like it better here for now
        public Transaction[] GetAssociatedTransactions(DateTime startDate, DateTime endDate)
        {
            return this.DelayLoadEntitySet<Transaction>
            (
                null,
                new DbParameter("UserCategoryId", this.UserCategoryId),
                new DbParameter("StartDate", startDate),
                new DbParameter("EndDate", endDate)
            );
        }
    }
}
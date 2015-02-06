using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace SpendingReport.DataContext
{
    public sealed class UserCategorySpending : DbEntity<UserCategorySpending>
    {
        private static readonly IReadOnlyDictionary<DbOperation, DbOperationInfo> entityDbOperations = new ReadOnlyDictionary<DbOperation, DbOperationInfo>
        (
            new Dictionary<DbOperation, DbOperationInfo>()
            {
                {
                    DbOperation.Get,
                    new DbOperationInfo()
                    {
                        Procedure = "SP_UserCategorySpendings_Get"
                    }
                }
            }
        );

        public long UserCategoryId
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public double Spending
        {
            get;
            private set;
        }

        public double RecursiveSpending
        {
            get;
            private set;
        }
    }
}
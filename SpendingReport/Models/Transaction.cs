using SpendingReport.DataContext;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace SpendingReport.Models
{
    [Serializable]
    public sealed class Transaction : DbEntity<Transaction>
    {
        private static readonly IReadOnlyDictionary<DbOperation, DbOperationInfo> entityDbOperations = new ReadOnlyDictionary<DbOperation, DbOperationInfo>
        (
            new Dictionary<DbOperation, DbOperationInfo>()
            {
                {
                    DbOperation.Get,
                    new DbOperationInfo()
                    {
                        Procedure = "SP_Users_Get"
                    }
                },
                {
                    DbOperation.Put,
                    new DbOperationInfo()
                    {
                        Procedure = "SP_Transactions_Put",
                        ParameterName = "Transactions",
                        ParameterType = "UDT_Transactions_Put"
                    }
                },
                {
                    DbOperation.Delete,
                    new DbOperationInfo()
                    {
                        Procedure = "SP_Transactions_Del",
                        ParameterName = "Transactions",
                        ParameterType = "UDT_Transactions_Del"
                    }
                }
            }
        );

        public override IReadOnlyDictionary<DbOperation, DbOperationInfo> DbOperations
        {
            get { return Transaction.entityDbOperations; }
        }

        public Guid TransactionId
        {
            get;
            private set;
        }

        public long UserId
        {
            get;
            set;
        }

        public string CategoryName
        {
            get;
            set;
        }

        public string PayeeName
        {
            get;
            set;
        }

        public double Amount
        {
            get;
            set;
        }

        public DateTime TransactionDate
        {
            get;
            set;
        }

        public DateTime AddedDate
        {
            get;
            private set;
        }

        public DateTime ModifiedDate
        {
            get;
            private set;
        }

        public Transaction Clone()
        {
            return base.CloneInternal();
        }
    }
}
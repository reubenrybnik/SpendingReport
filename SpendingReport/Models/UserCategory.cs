using SpendingReport.DataContext;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SpendingReport.Models
{
    [DataContract]
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

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public string ParentName
        {
            get;
            set;
        }
    }
}
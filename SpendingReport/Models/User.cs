using SpendingReport.DataContext;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Web;

namespace SpendingReport.Models
{
    [DataContract]
    public sealed class User : DbEntity<User>
    {
        private static readonly Random passwordSaltGenerator = new Random();
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
                        Procedure = "SP_Users_Put",
                        ParameterName = "Users",
                        ParameterType = "UDT_Users_Put"
                    }
                },
                {
                    DbOperation.Delete,
                    new DbOperationInfo()
                    {
                        Procedure = "SP_Users_Del",
                        ParameterName = "Users",
                        ParameterType = "UDT_Users_Del"
                    }
                }
            }
        );

        private int passwordSalt;

        public override IReadOnlyDictionary<DbOperation, DbOperationInfo> DbOperations
        {
            get { return User.entityDbOperations; }
        }

        public long UserId
        {
            get;
            private set;
        }

        [DataMember]
        public string UserName
        {
            get;
            set;
        }

        public int PasswordSalt
        {
            get
            {
                if (this.passwordSalt == 0)
                {
                    this.passwordSalt = User.passwordSaltGenerator.Next(1, int.MaxValue);
                }

                return this.passwordSalt;
            }

            private set
            {
                this.passwordSalt = value;
            }
        }

        public string PasswordHash
        {
            get;
            set;
        }

        [DataMember]
        public string FirstName
        {
            get;
            set;
        }

        [DataMember]
        public string MiddleInitial
        {
            get;
            set;
        }

        [DataMember]
        public string LastName
        {
            get;
            set;
        }

        [DataMember]
        public string EmailAddress
        {
            get;
            set;
        }

        public UserCategory[] Categories
        {
            get
            {
                return this.DelayLoadEntitySet<UserCategory>("Categories", this.GetUserParameters());
            }
        }

        public string[] Payees
        {
            get
            {
                return this.DelayLoadScalarSet<string>("Payees", "SP_UserPayees_Get", this.GetUserParameters());
            }
        }

        private DbParameter[] GetUserParameters()
        {
            return new DbParameter[]
            {
                new DbParameter("UserId", this.UserId)
            };
        }
    }
}
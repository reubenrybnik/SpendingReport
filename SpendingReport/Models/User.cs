using Microsoft.AspNet.Identity;
using SpendingReport.DataContext;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace SpendingReport.Models
{
    [Serializable]
    public sealed class User : DbEntity<User>, IUser
    {
        private static readonly Random saltGenerator = new Random();
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

        private int salt;

        public override IReadOnlyDictionary<DbOperation, DbOperationInfo> DbOperations
        {
            get { return User.entityDbOperations; }
        }

        public string Id
        {
            get { return UserId.ToString(); }
        }

        public long UserId
        {
            get;
            private set;
        }

        public string UserName
        {
            get;
            set;
        }

        public int Salt
        {
            get
            {
                if (this.salt == 0)
                {
                    this.salt = User.saltGenerator.Next(1, int.MaxValue);
                }

                return this.salt;
            }

            private set
            {
                this.salt = value;
            }
        }

        public string PasswordHash
        {
            get;
            set;
        }

        public string FirstName
        {
            get;
            set;
        }

        public char MiddleInitial
        {
            get;
            set;
        }

        public string LastName
        {
            get;
            set;
        }

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
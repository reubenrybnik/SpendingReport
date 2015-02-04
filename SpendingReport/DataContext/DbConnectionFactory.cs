using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpendingReport.DataContext
{
    public static class DbConnectionFactory
    {
        private static Type dbConnectionType;

        static DbConnectionFactory()
        {
            DbConnectionFactory.SetDbConnectionType<DbConnection>();
        }

        internal static void SetDbConnectionType<TConnection>()
            where TConnection : IDbConnection
        {
            DbConnectionFactory.dbConnectionType = typeof(TConnection);
        }

        public static IDbConnection CreateConnection()
        {
            return (IDbConnection)Activator.CreateInstance(DbConnectionFactory.dbConnectionType);
        }
    }
}
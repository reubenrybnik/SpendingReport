using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace SpendingReport.DataContext
{
    public sealed class DbConnection : IDbConnection
    {
        private const string entityIdFieldName = "EntityId";
        private const string getSchemaCommandFormat = "DECLARE @schemaTable [{0}]; SELECT * FROM @schemaTable;";
        private static readonly Dictionary<Type, DbTypeInfo> dbTypeInfo = new Dictionary<Type, DbTypeInfo>();

        private readonly SqlConnection dbConnection = new SqlConnection(Properties.Settings.Default.DbConnectionString);

        public void Dispose()
        {
            this.dbConnection.Dispose();
        }

        public bool IsReservedColumn(DataColumn column)
        {
            return (column.ColumnName == DbConnection.entityIdFieldName);
        }

        public TProperty GetScalar<TProperty>(string procedure, params DbParameter[] parameters)
            where TProperty : IConvertible
        {
            using (SqlCommand getCommand = new SqlCommand(procedure, this.dbConnection))
            {
                getCommand.CommandType = CommandType.StoredProcedure;

                foreach (DbParameter parameter in parameters)
                {
                    getCommand.Parameters.AddWithValue(parameter.Name, parameter.Value);
                }

                this.dbConnection.Open();

                try
                {
                    return (TProperty)Convert.ChangeType(getCommand.ExecuteScalar(), typeof(TProperty));
                }
                finally
                {
                    this.dbConnection.Close();
                }
            }
        }

        public TProperty[] GetScalarSet<TProperty>(string procedure, params DbParameter[] parameters)
            where TProperty : IConvertible
        {
            using (SqlCommand getCommand = new SqlCommand(procedure, this.dbConnection))
            {
                getCommand.CommandType = CommandType.StoredProcedure;

                foreach (DbParameter parameter in parameters)
                {
                    getCommand.Parameters.AddWithValue(parameter.Name, parameter.Value);
                }

                using (DataTable scalarTable = new DataTable())
                {
                    using (SqlDataAdapter getAdapter = new SqlDataAdapter(getCommand))
                    {
                        getAdapter.Fill(scalarTable);
                    }

                    Type conversionType = typeof(TProperty);
                    TProperty[] scalars = new TProperty[scalarTable.Rows.Count];

                    for (int i = 0; i < scalars.Length; ++i)
                    {
                        scalars[i] = (TProperty)Convert.ChangeType(scalarTable.Rows[i][0], conversionType);
                    }

                    return scalars;
                }
            }
        }

        public TEntity GetSingle<TEntity>(params DbParameter[] parameters)
            where TEntity : IDbEntity, new()
        {
            return this.Get<TEntity>(parameters).FirstOrDefault();
        }

        public TEntity[] Get<TEntity>(params DbParameter[] parameters)
            where TEntity : IDbEntity, new()
        {
            const DbOperation operation = DbOperation.Get;
            Type entityType = typeof(TEntity);
            DbTypeInfo entityDbTypeInfo = this.GetDbTypeInfo<TEntity>();

            if (!entityDbTypeInfo.DefaultEntity.DbOperations.ContainsKey(operation))
            {
                throw new InvalidOperationException(string.Format("Operation {0} is not supported by type {1}.", operation, entityType.Name));
            }

            DbOperationInfo operationInfo = entityDbTypeInfo.DefaultEntity.DbOperations[operation];

            using (DataTable entityTable = new DataTable())
            {
                using (SqlCommand getCommand = new SqlCommand(operationInfo.Procedure, this.dbConnection))
                {
                    getCommand.CommandType = CommandType.StoredProcedure;

                    foreach (DbParameter parameter in parameters)
                    {
                        getCommand.Parameters.AddWithValue(parameter.Name, parameter.Value);
                    }

                    using (SqlDataAdapter getAdapter = new SqlDataAdapter(getCommand))
                    {
                        getAdapter.Fill(entityTable);
                    }
                }

                TEntity[] results = new TEntity[entityTable.Rows.Count];

                for (int i = 0; i < results.Length; ++i)
                {
                    TEntity entity = new TEntity();
                    entity.PopulateEntity(this, entityTable.Rows[i]);
                    results[i] = entity;
                }

                return results;
            }
        }

        public void Put<TEntity>(TEntity entity)
            where TEntity : IDbEntity, new()
        {
            this.Put<TEntity>(new TEntity[] { entity });
        }

        public void Put<TEntity>(TEntity[] entities)
            where TEntity : IDbEntity, new()
        {
            const DbOperation operation = DbOperation.Put;
            Type entityType = typeof(TEntity);
            DbTypeInfo entityDbTypeInfo = this.GetDbTypeInfo<TEntity>();

            if (!entityDbTypeInfo.DefaultEntity.DbOperations.ContainsKey(operation))
            {
                throw new InvalidOperationException(string.Format("Operation {0} is not supported by type {1}.", operation, entityType.Name));
            }

            DbOperationInfo operationInfo = entityDbTypeInfo.DefaultEntity.DbOperations[operation];

            using (DataTable entityTable = entityDbTypeInfo.SchemaInfo[operationInfo.ParameterType].Clone())
            {
                for (int i = 0; i < entities.Length; ++i)
                {
                    DataRow entityRow = entityTable.NewRow();
                    entityRow[DbConnection.entityIdFieldName] = i;
                    entities[i].PopulateDataRow(this, entityRow);
                    entityTable.Rows.Add(entityRow);
                }

                using (DataTable entityUpdateTable = new DataTable())
                {
                    using (SqlCommand putCommand = new SqlCommand(operationInfo.Procedure, this.dbConnection))
                    {
                        SqlParameter entityParameter = new SqlParameter()
                        {
                            ParameterName = operationInfo.ParameterName,
                            SqlDbType = SqlDbType.Structured,
                            Value = entityTable
                        };

                        putCommand.Parameters.Add(entityParameter);
                        putCommand.CommandType = CommandType.StoredProcedure;

                        using (SqlDataAdapter putAdapter = new SqlDataAdapter(putCommand))
                        {
                            putAdapter.Fill(entityUpdateTable);
                        }
                    }

                    foreach (DataRow entityUpdateRow in entityUpdateTable.Rows)
                    {
                        int entityUpdateId = (int)entityUpdateRow[DbConnection.entityIdFieldName];
                        entities[entityUpdateId].PopulateEntity(this, entityUpdateRow);
                    }
                }
            }
        }

        public void Delete<TEntity>(TEntity entity)
            where TEntity : IDbEntity, new()
        {
            this.Delete<TEntity>(new TEntity[] { entity });
        }

        public void Delete<TEntity>(TEntity[] entities)
            where TEntity : IDbEntity, new()
        {
            const DbOperation operation = DbOperation.Delete;

            Type entityType = typeof(TEntity);
            DbTypeInfo entityDbTypeInfo = this.GetDbTypeInfo<TEntity>();

            if (!entityDbTypeInfo.DefaultEntity.DbOperations.ContainsKey(operation))
            {
                throw new InvalidOperationException(string.Format("Operation {0} is not supported by type {1}.", operation, entityType.Name));
            }

            DbOperationInfo operationInfo = entityDbTypeInfo.DefaultEntity.DbOperations[operation];

            using (DataTable entityTable = entityDbTypeInfo.SchemaInfo[operationInfo.ParameterType].Clone())
            {
                for (int i = 0; i < entities.Length; ++i)
                {
                    DataRow entityRow = entityTable.NewRow();
                    entities[i].PopulateDataRow(this, entityRow);
                    entityTable.Rows.Add(entityRow);
                }

                using (SqlCommand deleteCommand = new SqlCommand(operationInfo.Procedure, this.dbConnection))
                {
                    SqlParameter entityParameter = new SqlParameter()
                    {
                        ParameterName = operationInfo.ParameterName,
                        SqlDbType = SqlDbType.Structured,
                        Value = entityTable
                    };

                    deleteCommand.Parameters.Add(entityParameter);
                    deleteCommand.CommandType = CommandType.StoredProcedure;

                    deleteCommand.ExecuteNonQuery();
                }
            }
        }

        private DbTypeInfo GetDbTypeInfo<TEntity>()
            where TEntity : IDbEntity, new()
        {
            Type entityType = typeof(TEntity);
            DbTypeInfo entityDbTypeInfo;

            if (!DbConnection.dbTypeInfo.TryGetValue(entityType, out entityDbTypeInfo))
            {
                IDbEntity defaultEntity = new TEntity();
                SchemaCollection entitySchemaCollection = new SchemaCollection();

                foreach (DbOperationInfo operation in defaultEntity.DbOperations.Values)
                {
                    if (operation.ParameterType != null && !entitySchemaCollection.Contains(operation.ParameterType))
                    {
                        DataTable schemaTable = this.GetSchema(operation.ParameterType);
                        entitySchemaCollection.Add(schemaTable);
                    }
                }

                entityDbTypeInfo = new DbTypeInfo()
                {
                    DefaultEntity = defaultEntity,
                    SchemaInfo = entitySchemaCollection
                };

                DbConnection.dbTypeInfo[entityType] = entityDbTypeInfo;
            }

            return entityDbTypeInfo;
        }

        private DataTable GetSchema(string typeName)
        {
            DataTable schemaTable = new DataTable(typeName);
            string getSchemaCommandText = string.Format(DbConnection.getSchemaCommandFormat, typeName);

            using (SqlCommand getSchemaCommand = new SqlCommand(getSchemaCommandText, this.dbConnection))
            using (SqlDataAdapter getSchemaAdapter = new SqlDataAdapter(getSchemaCommand))
            {
                getSchemaAdapter.FillSchema(schemaTable, SchemaType.Source);
            }

            return schemaTable;
        }

        private struct DbTypeInfo
        {
            public IDbEntity DefaultEntity
            {
                get;
                set;
            }

            public SchemaCollection SchemaInfo
            {
                get;
                set;
            }
        }

        private sealed class SchemaCollection : KeyedCollection<string, DataTable>
        {
            protected override string GetKeyForItem(DataTable item)
            {
                return item.TableName;
            }
        }
    }

    public interface IDbConnection : IDisposable
    {
        bool IsReservedColumn(DataColumn column);
        TProperty GetScalar<TProperty>(string procedure, DbParameter[] parameters) where TProperty : IConvertible;
        TProperty[] GetScalarSet<TProperty>(string procedure, DbParameter[] parameters) where TProperty : IConvertible;
        TEntity GetSingle<TEntity>(DbParameter[] parameters) where TEntity : IDbEntity, new();
        TEntity[] Get<TEntity>(DbParameter[] parameters) where TEntity : IDbEntity, new();
        void Put<TEntity>(TEntity entity) where TEntity : IDbEntity, new();
        void Put<TEntity>(TEntity[] entities) where TEntity : IDbEntity, new();
        void Delete<TEntity>(TEntity entity) where TEntity : IDbEntity, new();
        void Delete<TEntity>(TEntity[] entities) where TEntity : IDbEntity, new();
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;

namespace SpendingReport.DataContext
{
    [Serializable]
    public abstract class DbEntity<TEntity> : IDbEntity
        where TEntity : DbEntity<TEntity>, new()
    {
        private static readonly Dictionary<string, DbEntityProperty> properties = typeof(TEntity).GetProperties().Where(property => property.CanWrite).ToDictionary(property => property.Name, property => new DbEntityProperty(property));

        private readonly Dictionary<string, object> delayLoadedValues = new Dictionary<string, object>();

        public abstract IReadOnlyDictionary<DbOperation, DbOperationInfo> DbOperations { get; }

        public void Put(TEntity other)
        {
            foreach (DbEntityProperty property in DbEntity<TEntity>.properties.Values)
            {
                if (property.IsDataMember && property.HasPublicSet)
                {
                    property.PropertyInfo.SetValue(this, property.PropertyInfo.GetValue(other));
                }
            }
        }

        protected TEntity CloneInternal()
        {
            TEntity clonedEntity = new TEntity();

            foreach (DbEntityProperty property in DbEntity<TEntity>.properties.Values)
            {
                if (property.HasPublicSet)
                {
                    property.PropertyInfo.SetValue(clonedEntity, property.PropertyInfo.GetValue(this));
                }
            }

            return clonedEntity;
        }

        void IDbEntity.PopulateEntity(IDbConnection connection, DataRow row)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (!connection.IsReservedColumn(column))
                {
                    PropertyInfo propertyInfo = DbEntity<TEntity>.properties[column.ColumnName].PropertyInfo;
                    propertyInfo.SetValue(this, Convert.ChangeType(row[column], propertyInfo.PropertyType));
                }
            }
        }

        void IDbEntity.PopulateDataRow(IDbConnection connection, DataRow row)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (!connection.IsReservedColumn(column))
                {
                    PropertyInfo propertyInfo = DbEntity<TEntity>.properties[column.ColumnName].PropertyInfo;
                    row[column] = Convert.ChangeType(propertyInfo.GetValue(this), column.DataType);
                }
            }
        }

        protected TProperty DelayLoadScalar<TProperty>(string cacheKey, string getProcedure, params DbParameter[] parameters)
            where TProperty : IConvertible
        {
            TProperty scalar;

            if (!this.TryGetDelayLoadedValue<TProperty>(cacheKey, out scalar))
            {
                using (IDbConnection dbConnection = DbConnectionFactory.CreateConnection())
                {
                    scalar = dbConnection.GetScalar<TProperty>(getProcedure, parameters);
                    this.CacheDelayLoadedValue(cacheKey, scalar);
                }
            }

            return scalar;
        }

        protected TProperty[] DelayLoadScalarSet<TProperty>(string cacheKey, string getProcedure, params DbParameter[] parameters)
            where TProperty : IConvertible
        {
            TProperty[] scalars;

            if (!this.TryGetDelayLoadedValue<TProperty[]>(cacheKey, out scalars))
            {
                using (IDbConnection dbConnection = DbConnectionFactory.CreateConnection())
                {
                    scalars = dbConnection.GetScalarSet<TProperty>(getProcedure, parameters);
                    this.CacheDelayLoadedValue(cacheKey, scalars);
                }
            }

            return scalars;
        }

        protected TProperty DelayLoadEntity<TProperty>(string cacheKey, params DbParameter[] parameters)
            where TProperty : IDbEntity, new()
        {
            TProperty entity;

            if (!this.TryGetDelayLoadedValue<TProperty>(cacheKey, out entity))
            {
                using (IDbConnection dbConnection = DbConnectionFactory.CreateConnection())
                {
                    entity = dbConnection.GetSingle<TProperty>(parameters);
                    this.CacheDelayLoadedValue(cacheKey, entity);
                }
            }

            return entity;
        }

        protected TProperty[] DelayLoadEntitySet<TProperty>(string cacheKey, params DbParameter[] parameters)
            where TProperty : IDbEntity, new()
        {
            TProperty[] entities;

            if (!this.TryGetDelayLoadedValue<TProperty[]>(cacheKey, out entities))
            {
                using (IDbConnection dbConnection = DbConnectionFactory.CreateConnection())
                {
                    entities = dbConnection.Get<TProperty>(parameters);
                    this.CacheDelayLoadedValue(cacheKey, entities);
                }
            }

            return entities;
        }

        private bool TryGetDelayLoadedValue<TProperty>(string cacheKey, out TProperty delayLoadedValue)
        {
            object cachedObject;
            bool propertyCached = this.delayLoadedValues.TryGetValue(cacheKey, out cachedObject);

            if (propertyCached)
            {
                delayLoadedValue = (TProperty)cachedObject;
            }
            else
            {
                delayLoadedValue = default(TProperty);
            }

            return propertyCached;
        }

        private void CacheDelayLoadedValue(string propertyName, object delayLoadedValue)
        {
            lock (this.delayLoadedValues)
            {
                if (!this.delayLoadedValues.ContainsKey(propertyName))
                {
                    this.delayLoadedValues.Add(propertyName, delayLoadedValue);
                }
            }
        }

        private class DbEntityProperty
        {
            public PropertyInfo PropertyInfo
            {
                get;
                private set;
            }

            public bool HasPublicSet
            {
                get;
                private set;
            }

            public bool IsDataMember
            {
                get;
                private set;
            }

            public DbEntityProperty(PropertyInfo propertyInfo)
            {
                this.PropertyInfo = propertyInfo;
                this.HasPublicSet = (propertyInfo.GetAccessors().Length > 1);
                this.IsDataMember = (propertyInfo.GetCustomAttribute<DataMemberAttribute>() != null);
            }
        }
    }

    public struct DbOperationInfo
    {
        public string Procedure;
        public string ParameterName;
        public string ParameterType;
    }

    public enum DbOperation
    {
        Get,
        Put,
        Delete
    }

    // TODO: I'd prefer this interface to be internal, but doing that and having the DbOperations property
    // be abstract causes compiler issues. For now, I'm ok with this since this is all just one project that
    // won't be inherited from anyway, but ideally I'd put the data context in its own project since it's generic
    // enough to work with any project that interacts with SQL.
    public interface IDbEntity
    {
        IReadOnlyDictionary<DbOperation, DbOperationInfo> DbOperations { get; }
        void PopulateEntity(IDbConnection connection, DataRow dataRow);
        void PopulateDataRow(IDbConnection connection, DataRow dataRow);
    }
}
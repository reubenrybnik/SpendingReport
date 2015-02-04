using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;

namespace SpendingReport.DataContext
{
    [Serializable]
    public abstract class DbEntity<TEntity> : IDbEntity
        where TEntity : DbEntity<TEntity>, new()
    {
        private static readonly Dictionary<string, PropertyInfo> properties = typeof(TEntity).GetProperties().Where(property => property.CanWrite).ToDictionary(property => property.Name);

        private readonly Dictionary<string, object> delayLoadedValues = new Dictionary<string, object>();

        public abstract IReadOnlyDictionary<DbOperation, DbOperationInfo> DbOperations { get; }

        protected TEntity CloneInternal()
        {
            TEntity clonedEntity = new TEntity();

            foreach (PropertyInfo property in DbEntity<TEntity>.properties.Values)
            {
                // if write accessor is public
                if (property.GetAccessors().Length > 1)
                {
                    property.SetValue(clonedEntity, property.GetValue(this));
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
                    DbEntity<TEntity>.properties[column.ColumnName].SetValue(this, row[column]);
                }
            }
        }

        void IDbEntity.PopulateDataRow(IDbConnection connection, DataRow row)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (!connection.IsReservedColumn(column))
                {
                    row[column] = DbEntity<TEntity>.properties[column.ColumnName].GetValue(this);
                }
            }
        }

        protected TProperty DelayLoadScalar<TProperty>(string cacheKey, string getProcedure, params DbParameter[] parameters)
        {
            TProperty scalar;

            if (!this.TryGetDelayLoadedValue<TProperty>(cacheKey, out scalar))
            {

                this.CacheDelayLoadedValue(cacheKey, scalar);
            }

            return scalar;
        }

        protected TProperty[] DelayLoadScalarSet<TProperty>(string cacheKey, string getProcedure, params DbParameter[] parameters)
        {
            TProperty[] scalars;

            if (!this.TryGetDelayLoadedValue<TProperty[]>(cacheKey, out scalars))
            {

                this.CacheDelayLoadedValue(cacheKey, scalars);
            }

            return scalars;
        }

        protected TProperty DelayLoadEntity<TProperty>(string cacheKey, params DbParameter[] parameters)
            where TProperty : IDbEntity, new()
        {
            TProperty entity;

            if (!this.TryGetDelayLoadedValue<TProperty>(cacheKey, out entity))
            {
                
                this.CacheDelayLoadedValue(cacheKey, entity);
            }

            return entity;
        }

        protected TProperty[] DelayLoadEntitySet<TProperty>(string cacheKey, params DbParameter[] parameters)
            where TProperty : IDbEntity, new()
        {
            TProperty[] entities;

            if (!this.TryGetDelayLoadedValue<TProperty[]>(cacheKey, out entities))
            {

                this.CacheDelayLoadedValue(cacheKey, entities);
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

    public interface IDbEntity
    {
        IReadOnlyDictionary<DbOperation, DbOperationInfo> DbOperations { get; }
        void PopulateEntity(IDbConnection connection, DataRow dataRow);
        void PopulateDataRow(IDbConnection connection, DataRow dataRow);
    }
}
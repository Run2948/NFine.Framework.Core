﻿using Microsoft.EntityFrameworkCore;
using NFine.Code;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NFine.Data
{
    /// <summary>
    /// 仓储实现
    /// </summary>
    public class RepositoryBase : IRepositoryBase
    {
        private readonly NFineDbContext _dbContext; //= new NFineDbContext();
        private DbTransaction Transaction { get; set; }

        public RepositoryBase(NFineDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IRepositoryBase BeginTrans()
        {
            DbConnection dbConnection = _dbContext.Database.GetDbConnection();
            if (dbConnection.State == ConnectionState.Closed)
            {
                dbConnection.Open();
            }
            Transaction = dbConnection.BeginTransaction();
            _dbContext.Database.UseTransaction(Transaction);
            return this;
        }

        public int Commit()
        {
            try
            {
                var returnValue = _dbContext.SaveChanges();

                if (Transaction != null)
                {
                    Transaction.Commit();
                }
                return returnValue;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (Transaction != null)
                {
                    this.Transaction.Rollback();
                }
                throw;
            }
            finally
            {
                this.Dispose();
            }
        }

        public void Dispose()
        {
            if (Transaction != null)
            {
                this.Transaction.Dispose();
            }
            _dbContext.Dispose();
        }

        public int Insert<TEntity>(TEntity entity) where TEntity : class
        {
            _dbContext.Entry<TEntity>(entity).State = EntityState.Added;
            return Transaction == null ? this.Commit() : 0;
        }

        public int Insert<TEntity>(List<TEntity> entitys) where TEntity : class
        {
            foreach (var entity in entitys)
            {
                _dbContext.Entry<TEntity>(entity).State = EntityState.Added;
            }
            return Transaction == null ? this.Commit() : 0;
        }

        public int Update<TEntity>(TEntity entity) where TEntity : class
        {
            _dbContext.Set<TEntity>().Attach(entity);
            PropertyInfo[] props = entity.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                if (prop.GetValue(entity, null) != null)
                {
                    if (prop.GetValue(entity, null).ToString() == "&nbsp;")
                        _dbContext.Entry(entity).Property(prop.Name).CurrentValue = null;
                    if (!prop.Name.Equals("F_Id"))
                        _dbContext.Entry(entity).Property(prop.Name).IsModified = true;
                }
            }
            return Transaction == null ? this.Commit() : 0;
        }

        public int Delete<TEntity>(TEntity entity) where TEntity : class
        {
            _dbContext.Set<TEntity>().Attach(entity);
            PropertyInfo prop = entity.GetType().GetProperty("F_DeleteMark");
            if (prop != null)
            {
                prop.SetValue(entity, true);
                _dbContext.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                _dbContext.Entry<TEntity>(entity).State = EntityState.Deleted;
            }
            return Transaction == null ? this.Commit() : 0;
        }

        public int Delete<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            var entitys = _dbContext.Set<TEntity>().Where(predicate)?.ToList();
            entitys?.ForEach(m =>
            {
                PropertyInfo prop = m.GetType().GetProperty("F_DeleteMark");
                if (prop != null)
                {
                    prop.SetValue(m, true);
                    _dbContext.Entry<TEntity>(m).State = EntityState.Modified;
                }
                else
                {
                    _dbContext.Entry<TEntity>(m).State = EntityState.Deleted;
                }
            });
            return Transaction == null ? this.Commit() : 0;
        }

        public TEntity FindEntity<TEntity>(object keyValue) where TEntity : class
        {
            return _dbContext.Set<TEntity>().Find(keyValue);
        }

        public TEntity FindEntity<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return _dbContext.Set<TEntity>().FirstOrDefault(predicate);
        }

        public IQueryable<TEntity> IQueryable<TEntity>() where TEntity : class
        {
            return _dbContext.Set<TEntity>();
        }

        public IQueryable<TEntity> IQueryable<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return _dbContext.Set<TEntity>().Where(predicate);
        }
        public List<TEntity> FindList<TEntity>(string strSql) where TEntity : class
        {
            return _dbContext.Set<TEntity>().FromSqlRaw(strSql).ToList<TEntity>();
        }

        public List<TEntity> FindList<TEntity>(string strSql, DbParameter[] dbParameter) where TEntity : class
        {
            return _dbContext.Set<TEntity>().FromSqlRaw(strSql).ToList<TEntity>();
        }

        public List<TEntity> FindList<TEntity>(Pagination pagination) where TEntity : class, new()
        {
            bool isAsc = pagination.sord.ToLower() == "asc" ? true : false;
            string[] _order = pagination.sidx.Split(',');
            MethodCallExpression resultExp = null;
            var tempData = _dbContext.Set<TEntity>().AsQueryable();
            foreach (string item in _order)
            {
                string _orderPart = item;
                _orderPart = Regex.Replace(_orderPart, @"\s+", " ");
                string[] _orderArry = _orderPart.Split(' ');
                string _orderField = _orderArry[0];
                bool sort = isAsc;
                if (_orderArry.Length == 2)
                {
                    isAsc = _orderArry[1].ToUpper() == "ASC" ? true : false;
                }
                var parameter = Expression.Parameter(typeof(TEntity), "t");
                var property = typeof(TEntity).GetProperty(_orderField);
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExp = Expression.Lambda(propertyAccess, parameter);
                resultExp = Expression.Call(typeof(Queryable), isAsc ? "OrderBy" : "OrderByDescending", new Type[] { typeof(TEntity), property.PropertyType }, tempData.Expression, Expression.Quote(orderByExp));
            }
            tempData = tempData.Provider.CreateQuery<TEntity>(resultExp);
            pagination.records = tempData.Count();
            tempData = tempData.Skip<TEntity>(pagination.rows * (pagination.page - 1)).Take<TEntity>(pagination.rows).AsQueryable();
            return tempData.ToList();
        }

        public List<TEntity> FindList<TEntity>(Expression<Func<TEntity, bool>> predicate, Pagination pagination) where TEntity : class, new()
        {
            bool isAsc = pagination.sord.ToLower() == "asc" ? true : false;
            string[] _order = pagination.sidx.Split(',');
            MethodCallExpression resultExp = null;
            var tempData = _dbContext.Set<TEntity>().Where(predicate);
            foreach (string item in _order)
            {
                string _orderPart = item;
                _orderPart = Regex.Replace(_orderPart, @"\s+", " ");
                string[] _orderArry = _orderPart.Split(' ');
                string _orderField = _orderArry[0];
                bool sort = isAsc;
                if (_orderArry.Length == 2)
                {
                    isAsc = _orderArry[1].ToUpper() == "ASC" ? true : false;
                }
                var parameter = Expression.Parameter(typeof(TEntity), "t");
                var property = typeof(TEntity).GetProperty(_orderField);
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExp = Expression.Lambda(propertyAccess, parameter);
                resultExp = Expression.Call(typeof(Queryable), isAsc ? "OrderBy" : "OrderByDescending", new Type[] { typeof(TEntity), property.PropertyType }, tempData.Expression, Expression.Quote(orderByExp));
            }
            tempData = tempData.Provider.CreateQuery<TEntity>(resultExp);
            pagination.records = tempData.Count();
            tempData = tempData.Skip<TEntity>(pagination.rows * (pagination.page - 1)).Take<TEntity>(pagination.rows).AsQueryable();
            return tempData.ToList();
        }
    }
}

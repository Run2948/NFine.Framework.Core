using Microsoft.EntityFrameworkCore;
using NFine.Code;
using System;
using System.Collections.Generic;
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
    /// <typeparam name="TEntity"></typeparam>
    public class RepositoryBase<TEntity> : IRepositoryBase<TEntity> where TEntity : class, new()
    {
        public RepositoryBase(NFineDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public NFineDbContext dbContext = null;//= new NFineDbContext();

        public int Insert(TEntity entity)
        {
            dbContext.Entry<TEntity>(entity).State = EntityState.Added;
            return dbContext.SaveChanges();
        }

        public int Insert(List<TEntity> entitys)
        {
            foreach (var entity in entitys)
            {
                dbContext.Entry<TEntity>(entity).State = EntityState.Added;
            }
            return dbContext.SaveChanges();
        }

        public int Update(TEntity entity)
        {
            dbContext.Set<TEntity>().Attach(entity);

            PropertyInfo[] props = entity.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                if (prop.GetValue(entity, null) != null)
                {
                    if (prop.GetValue(entity, null).ToString() == "&nbsp;")
                        dbContext.Entry(entity).Property(prop.Name).CurrentValue = null;
                    if (!prop.Name.Equals("F_Id"))
                        dbContext.Entry(entity).Property(prop.Name).IsModified = true;
                }
            }
            return dbContext.SaveChanges();
        }

        public int Delete(TEntity entity)
        {
            dbContext.Set<TEntity>().Attach(entity);
            PropertyInfo prop = entity.GetType().GetProperty("F_DeleteMark");
            if (prop != null)
            {
                prop.SetValue(entity, true);
                dbContext.Entry<TEntity>(entity).State = EntityState.Modified;
            }
            else
            {
                dbContext.Entry<TEntity>(entity).State = EntityState.Deleted;
            }
            return dbContext.SaveChanges();
        }

        public int Delete(Expression<Func<TEntity, bool>> predicate)
        {
            var entitys = dbContext.Set<TEntity>().Where(predicate)?.ToList();
            entitys?.ForEach(m =>
            {
                PropertyInfo prop = m.GetType().GetProperty("F_DeleteMark");
                if (prop != null)
                {
                    prop.SetValue(m, true);
                    dbContext.Entry<TEntity>(m).State = EntityState.Modified;
                }
                else
                {
                    dbContext.Entry<TEntity>(m).State = EntityState.Deleted;
                }
            });
            return dbContext.SaveChanges();
        }

        public TEntity FindEntity(object keyValue)
        {
            return dbContext.Set<TEntity>().Find(keyValue);
        }

        public TEntity FindEntity(Expression<Func<TEntity, bool>> predicate)
        {
            return dbContext.Set<TEntity>().FirstOrDefault(predicate);
        }

        public IQueryable<TEntity> IQueryable()
        {
            return dbContext.Set<TEntity>();
        }

        public IQueryable<TEntity> IQueryable(Expression<Func<TEntity, bool>> predicate)
        {
            return dbContext.Set<TEntity>().Where(predicate);
        }

        public List<TEntity> FindList(string strSql)
        {
            return dbContext.Set<TEntity>().FromSqlRaw(strSql).ToList<TEntity>();
        }

        public List<TEntity> FindList(string strSql, DbParameter[] dbParameter)
        {
            return dbContext.Set<TEntity>().FromSqlRaw(strSql, dbParameter).ToList<TEntity>();
        }

        public List<TEntity> FindList(Pagination pagination)
        {
            bool isAsc = pagination.sord.ToLower() == "asc" ? true : false;
            string[] _order = pagination.sidx.Split(',');
            MethodCallExpression resultExp = null;
            var tempData = dbContext.Set<TEntity>().AsQueryable();
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

        public List<TEntity> FindList(Expression<Func<TEntity, bool>> predicate, Pagination pagination)
        {
            bool isAsc = pagination.sord.ToLower() == "asc" ? true : false;
            string[] _order = pagination.sidx.Split(',');
            MethodCallExpression resultExp = null;
            var tempData = dbContext.Set<TEntity>().Where(predicate);
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

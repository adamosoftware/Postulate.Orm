using Dapper;
using Postulate.Orm.Attributes;
using Postulate.Orm.Enums;
using Postulate.Orm.Extensions;
using Postulate.Orm.Interfaces;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Postulate.Orm.Abstract
{
    public abstract partial class SqlDb<TKey> : IDb
    {
        public void Update<TRecord>(TRecord record, params Expression<Func<TRecord, object>>[] setColumns) where TRecord : Record<TKey>, new()
        {
            using (IDbConnection cn = GetConnection())
            {
                cn.Open();
                Update(cn, record, setColumns);
            }
        }

        /// <summary>
        /// Updates specific properties of a record
        /// </summary>
        public void Update<TRecord>(IDbConnection connection, TRecord record, params Expression<Func<TRecord, object>>[] setColumns) where TRecord : Record<TKey>, new()
        {
            Type modelType = typeof(TRecord);
            IdentityColumnAttribute idAttr;
            string identityCol = (modelType.HasAttribute(out idAttr)) ? idAttr.ColumnName : IdentityColumnName;
            bool useAltIdentity = (!identityCol.Equals(IdentityColumnName));
            PropertyInfo piIdentity = null;
            if (useAltIdentity) piIdentity = modelType.GetProperty(identityCol);

            DynamicParameters dp = new DynamicParameters();
            dp.Add(identityCol, (!useAltIdentity) ? record.Id : piIdentity.GetValue(record));

            List<string> columnNames = new List<string>();
            string setClause = string.Join(", ", setColumns.Select(expr =>
            {
                string propName = PropertyNameFromLambda(expr);
                columnNames.Add(propName);
                PropertyInfo pi = typeof(TRecord).GetProperty(propName);
                dp.Add(propName, expr.Compile().Invoke(record));
                return $"{Syntax.ApplyDelimiter(pi.SqlColumnName())}=@{propName}";
            }).Concat(
                modelType.GetProperties().Where(pi =>
                    pi.HasAttribute<ColumnAccessAttribute>(a => a.Access == Access.UpdateOnly) &&
                    pi.IsSupportedType(_syntax))
                        .Select(pi =>
                        {
                            if (columnNames.Contains(pi.SqlColumnName())) throw new InvalidOperationException($"Can't set column {pi.SqlColumnName()} with the Update method because it has a ColumnAccess(UpdateOnly) attribute.");
                            return $"{Syntax.ApplyDelimiter(pi.SqlColumnName())}=@{pi.SqlColumnName()}";
                        })));

            string cmd = $"UPDATE {GetTableName<TRecord>()} SET {setClause} WHERE {Syntax.ApplyDelimiter(identityCol)}=@{identityCol}";

            SaveInner(connection, record, SaveAction.Update, (r, t) =>
            {
                connection.Execute(cmd, r);
            }, null);
        }

        protected string PropertyNameFromLambda(Expression expression)
        {
            // thanks to http://odetocode.com/blogs/scott/archive/2012/11/26/why-all-the-lambdas.aspx
            // thanks to http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression

            LambdaExpression le = expression as LambdaExpression;
            if (le == null) throw new ArgumentException("expression");

            MemberExpression me = null;
            if (le.Body.NodeType == ExpressionType.Convert)
            {
                me = ((UnaryExpression)le.Body).Operand as MemberExpression;
            }
            else if (le.Body.NodeType == ExpressionType.MemberAccess)
            {
                me = le.Body as MemberExpression;
            }

            if (me == null) throw new ArgumentException("expression");

            return me.Member.Name;
        }
    }
}
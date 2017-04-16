using Postulate.Attributes;
using Postulate.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq;
using Postulate.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Postulate.Abstract
{
    public abstract class SqlDb
    {
        public const string IdentityColumnName = "Id";

        protected Dictionary<string, string> _insertCommands = new Dictionary<string, string>();

        public string UserName { get; set; }

        public TRecord Find<TRecord, TKey>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            var row = ExecuteFind<TRecord, TKey>(connection, id);
            return FindInner<TRecord, TKey>(connection, row);
        }        

        public TRecord FindWhere<TRecord, TKey>(IDbConnection connection, string critieria) where TRecord : Record<TKey>
        {
            var row = ExecuteFindWhere<TRecord, TKey>(connection, critieria);
            return FindInner<TRecord, TKey>(connection, row);
        }        

        public void Delete<TRecord, TKey>(IDbConnection connection, TKey id) where TRecord : Record<TKey>
        {
            TRecord row = Find<TRecord, TKey>(connection, id);
            string message;
            if (row.AllowDelete(connection, UserName, out message))
            {
                ExecuteDelete(connection, id);
                row.AfterDelete(connection);
            }
            else
            {
                throw new PermissionDeniedException(message);
            }
        }        
        
        public void Save<TRecord, TKey>(IDbConnection connection, TRecord row) where TRecord : Record<TKey>
        {
            SaveAction action;
            Save<TRecord, TKey>(connection, row, out action);
        }

        public void Save<TRecord, TKey>(IDbConnection connection, TRecord row, out SaveAction action) where TRecord : Record<TKey>
        {
            action = SaveAction.NotSet;
            string message;
            if (row.IsValid(connection, out message))
            {
                if (row.AllowSave(connection, UserName, out message))
                {
                    action = (row.IsNewRow()) ? SaveAction.Insert : SaveAction.Update;
                    row.BeforeSave(connection, action);
                    if (row.IsNewRow())
                    {
                        row.Id = ExecuteInsert<TRecord, TKey>(connection, row);
                    }
                    else
                    {
                        ExecuteUpdate<TRecord, TKey>(connection, row);
                    }
                    row.AfterSave(connection, action);
                }
                else
                {
                    throw new PermissionDeniedException(message);
                }
            }
            else
            {
                throw new ValidationException(message);
            }
        }

        protected abstract TKey ExecuteInsert<TRecord, TKey>(IDbConnection connection, TRecord row) where TRecord : Record<TKey>;
        protected abstract void ExecuteUpdate<TRecord, TKey>(IDbConnection connection, TRecord row) where TRecord : Record<TKey>;
        protected abstract TRecord ExecuteFind<TRecord, TKey>(IDbConnection connection, TKey id) where TRecord : Record<TKey>;
        protected abstract TRecord ExecuteFindWhere<TRecord, TKey>(IDbConnection connection, string criteria) where TRecord : Record<TKey>;
        protected abstract void ExecuteDelete<TKey>(IDbConnection connection, TKey id);

        protected string GetTableName<TRecord, TKey>() where TRecord : Record<TKey>
        {
            Type modelType = typeof(TRecord);            
            string result = modelType.Name;

            TableAttribute tblAttr;
            if (modelType.HasAttribute(out tblAttr))
            {
                result = tblAttr.Name;
                if (!string.IsNullOrEmpty(tblAttr.Schema))
                {
                    result = tblAttr.Schema + "." + tblAttr.Name;
                }
            }

            return DelimitName(result);
        }
            
        protected string GetFindStatement<TRecord, TKey>() where TRecord : Record<TKey>
        {
            return 
                $@"SELECT 
                    {string.Join(", ", GetColumnNames<TRecord, TKey>().Select(name => DelimitName(name)))} 
                FROM 
                    {GetTableName<TRecord, TKey>()} 
                WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        protected string GetInsertStatement<TRecord, TKey>() where TRecord : Record<TKey>
        {
            var columns = GetColumnNames<TRecord, TKey>(pi => pi.HasColumnAccess(Access.InsertOnly));

            return 
                $@"INSERT {GetTableName<TRecord, TKey>()} (
                    {string.Join(", ", columns.Select(s => DelimitName(s)))}
                ) OUTPUT [inserted].[{typeof(TRecord).IdentityColumnName()}] VALUES (
                    {string.Join(", ", columns.Select(s => $"@{s}"))}
                )";
        }

        protected string GetUpdateStatement<TRecord, TKey>() where TRecord : Record<TKey>
        {
            var columns = GetColumnNames<TRecord, TKey>(pi => pi.HasColumnAccess(Access.UpdateOnly));

            return 
                $@"UPDATE {GetTableName<TRecord, TKey>()} SET
                    {string.Join(", ", columns.Select(s => $"{DelimitName(s)} = @{s}"))}
                WHERE 
                    [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        protected string GetDeleteStatement<TRecord, TKey>() where TRecord : Record<TKey>
        {
            return $"DELETE {GetTableName<TRecord, TKey>()} WHERE [{typeof(TRecord).IdentityColumnName()}]=@id";
        }

        protected IEnumerable<PropertyInfo> GetColumns<TRecord, TKey>(Func<PropertyInfo, bool> predicate = null) where TRecord : Record<TKey>
        {            
            return typeof(TRecord).GetProperties().Where(pi =>
                !pi.Name.Equals(IdentityColumnName) && (!pi.HasAttribute<ColumnAccessAttribute>() || (predicate?.Invoke(pi) ?? true)));
        }

        protected IEnumerable<string> GetColumnNames<TRecord, TKey>(Func<PropertyInfo, bool> predicate = null) where TRecord : Record<TKey>
        {
            return GetColumns<TRecord, TKey>(predicate).Select(pi =>
            {
                ColumnAttribute colAttr;
                return (pi.HasAttribute(out colAttr)) ? colAttr.Name : pi.Name;                
            });
        }        

        protected abstract string DelimitName(string name);

        private TRecord FindInner<TRecord, TKey>(IDbConnection connection, TRecord row) where TRecord : Record<TKey>
        {
            string message;
            if (row.AllowFind(connection, UserName, out message))
            {
                return row;
            }
            else
            {
                throw new PermissionDeniedException(message);
            }
        }
    }
}

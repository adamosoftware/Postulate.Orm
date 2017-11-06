using Dapper;
using Postulate.Orm.Abstract;
using Postulate.Orm.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Postulate.Orm.Merge.Actions
{
    public class DropTable : MergeAction
    {
        private readonly TableInfo _tableInfo;

        public DropTable(SqlScriptGenerator scriptGen, TableInfo tableInfo) : base(scriptGen, ObjectType.Table, ActionType.Drop, $"Drop table {tableInfo.ToString()}")
        {
            _tableInfo = tableInfo;
        }

        public DropTable(SqlScriptGenerator scriptGen, Type modelType, IDbConnection connection = null) : this(scriptGen, TableInfo.FromModelType(modelType, connection: connection))
        {
        }

        public override IEnumerable<string> SqlCommands(IDbConnection connection)
        {
            foreach (var fk in GetDependentForeignKeys(connection)) yield return GetDropForeignKeyStatement(fk);

            yield return GetDropTableStatement();
        }

        public virtual IEnumerable<ForeignKeyInfo> GetDependentForeignKeys(IDbConnection connection)
        {
            if (_tableInfo.ObjectId == 0) throw new Exception(
                $@"Dependent foreign key info could not be found for {_tableInfo} because the ObjectId was not set.
                Use the IDbConnection argument when creating a TableInfo object to make sure the ObjectId is set.");

            return connection.Query<ForeignKeyData>(
                @"SELECT
                    [fk].[name] AS [ConstraintName],
                    SCHEMA_NAME([parent].[schema_id]) AS [ReferencedSchema],
                    [parent].[name] AS [ReferencedTable],
                    [refdcol].[name] AS [ReferencedColumn],
                    SCHEMA_NAME([child].[schema_id]) AS [ReferencingSchema],
                    [child].[name] AS [ReferencingTable],
                    [rfincol].[name] AS [ReferencingColumn]
                FROM
                    [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [child] ON [fk].[parent_object_id]=[child].[object_id]
                    INNER JOIN [sys].[tables] [parent] ON [fk].[referenced_object_id]=[parent].[object_id]
                    INNER JOIN [sys].[foreign_key_columns] [fkcol] ON
                        [fk].[parent_object_id]=[fkcol].[parent_object_id] AND
                        [fk].[object_id]=[fkcol].[constraint_object_id]
                    INNER JOIN [sys].[columns] [refdcol] ON
                        [fkcol].[referenced_column_id]=[refdcol].[column_id] AND
                        [fkcol].[referenced_object_id]=[refdcol].[object_id]
                    INNER JOIN [sys].[columns] [rfincol] ON
                        [fkcol].[parent_column_id]=[rfincol].[column_id] AND
                        [fkcol].[parent_object_id]=[rfincol].[object_id]
				WHERE
                    [fk].[referenced_object_id]=@objID", new { objID = _tableInfo.ObjectId })
                .Select(fk => new ForeignKeyInfo()
                {
                    ConstraintName = fk.ConstraintName,
                    Child = new ColumnInfo() { Schema = fk.ReferencingSchema, TableName = fk.ReferencingTable, ColumnName = fk.ReferencingColumn },
                    Parent = new ColumnInfo() { Schema = fk.ReferencedSchema, TableName = fk.ReferencedTable, ColumnName = fk.ReferencedColumn }
                });
        }

        public virtual string GetDropForeignKeyStatement(ForeignKeyInfo foreignKeyInfo)
        {
            return $"ALTER TABLE [{foreignKeyInfo.Child.Schema}].[{foreignKeyInfo.Child.TableName}] DROP CONSTRAINT [{foreignKeyInfo.ConstraintName}]"; ;
        }

        public virtual string GetDropTableStatement()
        {
            return $"DROP TABLE [{_tableInfo.Schema}].[{_tableInfo.Name}]";
        }
    }
}
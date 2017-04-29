# Postulate ORM

Postulate is a lightweight code-first ORM for SQL Server made with Dapper. You can also target any data source that supports `IDbConnection`, but the default implementation is intended for SQL Server. This repo an overhaul of the [prior version](https://github.com/adamosoftware/Postulate08) with a number of breaking changes -- the new syntax is simpler, and model classes no longer require a RowManager.

As of now (4/29/17), several important things aren't working yet, and there's no nuget package available yet.

This is Postulate in a nutshell:

- [SqlServerDb&lt;TKey&gt;](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/SqlServerDb.cs) is the "root object" you inherit from that represents access to a SQL Server database as a whole. It offers CRUD methods such as [Find](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/SqlServerDb.cs#L29), [Save](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/SqlServerDb.cs#L83), and [Delete](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/SqlServerDb.cs#L56). Supported key types are `int`, `long`, and `Guid`.

- The only requirement for model classes is that they must inherit from [Record&lt;TKey&gt;](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/Abstract/Record.cs).

- Use the [SchemaMerge](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/Merge/SchemaMerge.cs) class to migrate models to your database. It offers methods for comparing and synchronizing models and the physical database.

- Most methods have at least two overloads -- one that accepts an `IDbConnection` already open, and one that opens and closes a connection within the scope of the method.



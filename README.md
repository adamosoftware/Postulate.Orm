# Postulate ORM

Nuget package **Postulate.Orm**

Postulate is a lightweight code-first ORM for SQL Server made with Dapper that uses inline SQL. You can target any data source that supports `IDbConnection`, but the default implementation is for SQL Server, and the Schema Merge feature works only with SQL Server. This repo an overhaul of the [prior version](https://github.com/adamosoftware/Postulate08) with a number of breaking changes -- the new syntax is simpler, and model classes no longer require a RowManager. The Query class has been moved to [Postulate.Sql](https://github.com/adamosoftware/Postulate.Sql).

This is Postulate in a nutshell:

- [SqlServerDb&lt;TKey&gt;](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/SqlServerDb.cs) is the "root object" you inherit from that represents access to a SQL Server database as a whole. It offers CRUD methods such as [Find](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/SqlServerDb.cs#L33), [Save](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/SqlServerDb.cs#L78), and [Delete](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/SqlServerDb.cs#L60). Supported key types are `int`, `long`, and `Guid`.

- The only requirement for model classes is that they must inherit from [Record&lt;TKey&gt;](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/Abstract/Record.cs). The `Record<TKey>` class has many overrides for checking permissions, handling events, and performing validation. A variety of attributes let you define foreign keys, primary keys, as well as fine-tune behaviors in multi-tenant systems such as limiting update capability on partition fields. See [Designing Model Classes](https://github.com/adamosoftware/Postulate.Orm/wiki/Designing-Model-Classes) for more information.

- Use the [SchemaMerge](https://github.com/adamosoftware/PostulateORM/blob/master/PostulateV1/Merge/SchemaMerge.cs) class to migrate models to your database. It offers methods for comparing and synchronizing models and the physical database, and works only with SQL Server. See [Schema Merging](https://github.com/adamosoftware/Postulate.Orm/wiki/Schema-Merge) for more information.

- Most methods have at least two overloads -- one that accepts an `IDbConnection` already open, and one that opens and closes a connection within the scope of the method.

## Code Examples

The following examples assume a `SqlServerDb<int>` variable called `MyDb` and this model class:

    public class Customer : Record<int>
    {
      [ForeignKey(typeof(Organization))]
      public int OrganizationId { get; set; }
      [MaxLength(50)]
      [Required]
      public string FirstName { get; set; }
      [MaxLength(50)]
      [Required]
      public string LastName { get; set; }
      public string Address { get; set; }
      public string City { get; set; }
      [MaxLength(2)]
      public string State { get; set; }
      [MaxLength(10)]
      public string ZipCode { get; set; }
    }

Find a Customer record from a key value.

    var customer = new MyDb().Find<Customer>(id);
    
Create and save a new Customer. "Save" here means an insert or update is performed automatically according to the state of the record:

    var customer = new Customer() { FirstName = "Adam", LastName = "O'Neil" };
    new MyDb().Save<Customer>(customer);

Find a Customer based on a WHERE clause:

    var customer = new MyDb().FindWhere<Customer>(
      "[LastName]=@lastName AND [FirstName]=@firstName", 
      new { lastName = "O'Neil", firstName = "Adam" });
      
Update select properties of a Customer without updating the whole record:

    customer.Address = "3232 Whatever St";
    customer.City = "Binghamton Heights";
    customer.State = "XR";
    customer.ZipCode = "12345";
    new MyDb().Update<Customer>(customer, r => r.Address, r => r.City, r => r.State, r => r.ZipCode);


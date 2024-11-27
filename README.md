# Playing with Dapper

- This repository contains a few projects for working with Dapper on PostgreSQL and SQL Server

## Projects in the solution

#### `DapperWebApi`

- Simple Web API for a simplified hotel booking system with a minimal business scope
- Unit of Work design pattern is implemented to handle transactions across multiple repositories
- PostgreSQL and DbUp are used for managing database migrations

#### `OutboxProcessorWorker`

- The concept originates from Milan: Outbox message processor designed for high-performance, handling of billions of messages daily
- I adapted the Postgres database solution for use on SQL server
- For the update, with the MERGE INTO statement, I encountered an issue of SQL Server's 2100 parameter limit
- While solving that, I realized by using a User-Defined Table Type and Stored Procedure could improve update performance

#### `ConcurrencyControlApp`

- A console application with examples demonstrating **optimistic concurrency** in SQL Server and Postgres
- There are examples that use Dapper with manually written SQL commands, as well as examples that use Entity Framework
- Both databases have built-in solutions, but they work differently
- SQL Server uses a type of `ROWVERSION` column for the table, while Postgres has a built-in system column called `xmin`
- Both works well and require no additional management effort
- You could create a unified solution for SQL Server and Postgres by using a column like UpdatedAt as a DateTime or Version as a Guid. However, in this case, you would need to manage the version manually. Fortunately, EF can assist with this, as [shown in this example](ConcurrencyControlApp/Common/CustomConcurrencyToken.cs).
- The Unit of Work design pattern is implemented in a generic way that can be used for both SQL Server and Postgres with Dapper

## Resources

#### 🧰 `Dapper`

- [Learn Dapper](https://www.learndapper.com) 📓*Official*
- [Dapper.SqlBuilder](https://github.com/DapperLib/Dapper/tree/main/Dapper.SqlBuilder) 👤*Simple SQL formatter*
- [Getting started with Dapper](https://youtu.be/F1ONxvjdLlc) 📽️*24 min - Nick Chapsas*
- [Dapper and SQL Server Database relationships](https://youtu.be/OPedaRBwNUA) 📽️*1h:15min - Patrick God*
- [Building a Dapper generic CRUD repository from scratch](https://youtu.be/9YGByZqzOaY) 📽️*1h:16min - Remigiusz Zalewski*
  - [Generating SQL queries with a Source Generator](https://github.com/19balazs86/PlayingWithSourceGenerator/blob/master/SourceGeneratorLib/SqlSourceGenerator.cs) 👤*Just for fun, not fully finished*

#### 🆙 `DbUp`

- [Documentation](https://dbup.github.io) 📓*Official*
- Manage database migrations: 📽️ [Amichai Mantinband](https://youtu.be/pgCJYNyayeM) | [Nick Chapsas](https://youtu.be/fdbW9eC3rN4) | [Dev Leader](https://youtu.be/FuXx-N2-zoM)

#### 🧑 `Milan's newsletter`

- [Mastering Dapper relationship mappings](https://www.milanjovanovic.tech/blog/mastering-dapper-relationship-mappings)
- [Scaling the outbox pattern using Postgres](https://www.milanjovanovic.tech/blog/scaling-the-outbox-pattern) | [Source](https://github.com/m-jovanovic/outbox-scaling)
- [Fast SQL Bulk Inserts](https://www.milanjovanovic.tech/blog/fast-sql-bulk-inserts-with-csharp-and-ef-core) (Dapper, EF, EF Bulk Extensions, SQL Bulk Copy)

#### 🦺 `Concurrency`

- [EF handling concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=fluent-api)
- [EF Postgres concurrency token](https://www.npgsql.org/efcore/modeling/concurrency.html?tabs=fluent-api)
- [EF Pessimistic Locking](https://code-maze.com/dotnet-optimistic-locking-vs-pessimistic-locking) *(RowVersion, ConcurrencyToken)* 📓*Code-Maze*
- [Resolve deadlocks when swapping numbers using RowVersion](https://youtu.be/EzXKlTyk2KU) 📽️*28 min - Zoran Horvat*

#### ✨ `Miscellaneous`

- [Delta](https://github.com/SimonCropp/Delta) 👤*Simon Cropp*
  - Implementing a 304 Not Modified response by leveraging SqlServer RowVersion to generate an ETag
  - [Improve DB performance by using Delta](https://youtu.be/sLoTReccvPw) 📽️*9 min - Nick Chapsas*
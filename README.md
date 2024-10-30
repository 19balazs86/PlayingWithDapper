# Playing with Dapper

- This repository contains 2 projects for working with Dapper on PostgreSQL and SQL Server

## Projects in the solution

#### `DapperWebApi`

- Simple Web API for a simplified hotel booking system with a minimal business scope
- Unit of Work design pattern is implemented to handle transactions across multiple repositories
- PostgreSQL and DbUp are used for managing database migrations

#### `OutboxProcessorWorker`

- The concept originates from Milan: Outbox message processor designed for high-performance, handling of billions of messages daily
- I adapted the Postgres database solution for use on SQL server
- For the update, with the MERGE INTO statement, I encountered the issue of SQL Server's 2100 parameter limit
- While solving that issue, I realized I could improve update performance by using a User-Defined Table Type and Stored Procedure
- Eventually, I implemented the solution for both PostgreSQL and SQL Server using Dapper

## Resources

#### 🧰 `Dapper`

- [Learn Dapper](https://www.learndapper.com) 📓*Official*
- [Dapper.SqlBuilder](https://github.com/DapperLib/Dapper/tree/main/Dapper.SqlBuilder) 👤*Simple sql formatter*
- [Getting started with Dapper](https://youtu.be/F1ONxvjdLlc) 📽️*24 min - Nick Chapsas*
- [Dapper and SQL Server Database relationships](https://youtu.be/OPedaRBwNUA) 📽️*1h:15min - Patrick God*
- [Building a Dapper generic CRUD repository from scratch](https://youtu.be/9YGByZqzOaY) 📽️*1h:16min - Remigiusz Zalewski*
  - [Generating SQL queries with a Source Generator](https://github.com/19balazs86/PlayingWithSourceGenerator/blob/master/SourceGeneratorLib/SqlSourceGenerator.cs) 👤*Just for fun, not fully finished*

#### 🆙 `DbUp`

- [Documentation](https://dbup.github.io) 📓*Official*
- Manage database migrations - 📽️ [Amichai Mantinband](https://youtu.be/pgCJYNyayeM) | [Nick Chapsas](https://youtu.be/fdbW9eC3rN4) | [Dev Leader](https://youtu.be/FuXx-N2-zoM)

#### 🧑 `Milan's newsletter`

- [Scaling the outbox pattern using Postgres](https://www.milanjovanovic.tech/blog/scaling-the-outbox-pattern) | [Source](https://github.com/m-jovanovic/outbox-scaling)
- [Fast SQL Bulk Inserts](https://www.milanjovanovic.tech/blog/fast-sql-bulk-inserts-with-csharp-and-ef-core) (Dapper, EF, EF Bulk Extensions, SQL Bulk Copy)
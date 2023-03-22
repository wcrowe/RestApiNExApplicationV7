using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestApiNLxV7.Data.Context;
using RestApiNLxV7.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace RestApiNLxV7.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("InitialCreate")]
    public class InitialCreate : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {

            //create a table
            migrationBuilder.Sql(@"
                IF (NOT EXISTS(SELECT * 
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_SCHEMA = 'dbo'
                        AND TABLE_NAME = 'Accounts'))
                    BEGIN
                        CREATE TABLE [dbo].[Accounts](
	                        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	                        [Name] [nvarchar](30) NOT NULL,
	                        [Description] [nvarchar](255) NULL
                         )
                    END
                    ");

            //add some records
            migrationBuilder.Sql(@"
                IF (NOT EXISTS(SELECT * FROM Accounts))
                    BEGIN
                        INSERT INTO [dbo].[Accounts] ([Name] ,[Description]) VALUES('Nikola Tesla1','one of the greatest ');
                        INSERT INTO [dbo].[Accounts] ([Name] ,[Description]) VALUES('Nikola Tesla2','one of the greatest ');
                    END
                    ");


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

        }

    }
}

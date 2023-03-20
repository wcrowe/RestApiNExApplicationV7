using System;
using System.Collections.Generic;
using System.Text;

namespace RestApiNDxApiV6.Entity.Queries
{
    public class AccountQuery : IQuery  //<Account>
    {
        readonly string selectAll = "SELECT * FROM Accounts";
        readonly string selectOne = "SELECT * FROM Accounts WHERE Id = @Id";
        readonly string insert = "INSERT INTO Accounts (Name, Email, Description, IsTrial, IsActive, SetActive, Created, Modified )"
                             + " VALUES(@Name, @Email, @Description, @IsTrial, @IsActive, @SetActive, GETUTCDATE(), GETUTCDATE());"
                             + "SELECT CAST(SCOPE_IDENTITY() as int)";
        readonly string update = "IF NOT EXISTS (SELECT * FROM Accounts WHERE Id = @Id) SELECT 0 "
                               + "ELSE IF NOT EXISTS (SELECT * FROM Accounts WHERE RowVersion = @RowVersion) SELECT -1 ELSE SELECT 1;"
                               + "UPDATE Accounts SET Name=@Name, Email=@Email, Description=@Description, IsTrial=@IsTrial, IsActive=@IsActive,SetActive=@SetActive, Modified=GETUTCDATE()"
                               + " WHERE Id = @Id AND RowVersion = @RowVersion";
        readonly string delete = "IF NOT EXISTS (SELECT * FROM Accounts WHERE Id = @Id) SELECT 0 "
                               + "ELSE SELECT 1;"
                                + "DELETE FROM Accounts"
                                + " WHERE Id = @Id";
        string IQuery.SelectAll => selectAll;
        string IQuery.SelectOne => selectOne;
        string IQuery.Insert => insert;
        string IQuery.Update => update;
        string IQuery.Delete => delete;
    }
}

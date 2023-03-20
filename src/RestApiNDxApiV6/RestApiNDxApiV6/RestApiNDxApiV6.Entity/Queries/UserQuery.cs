using System;
using System.Collections.Generic;
using System.Text;

namespace RestApiNDxApiV6.Entity.Queries
{
    public class UserQuery : IQuery  //<User>
    {
        readonly string selectAll = "SELECT * FROM Users";
        readonly string selectOne = "SELECT * FROM Users WHERE Id = @Id";
        readonly string insert = "INSERT INTO Users (FirstName, LastName, UserName, Email, Description, IsAdminRole, Roles, IsActive, Password, AccountId, RefreshToken, RefreshTokenExpiryDateTime, Created, Modified )"
                             + " VALUES(@FirstName, @LastName, @UserName, @Email, @Description, @IsAdminRole, @Roles, @IsActive, @Password, @AccountId, @RefreshToken, @RefreshTokenExpiryDateTime, GETUTCDATE(), GETUTCDATE())"
                             + "SELECT CAST(SCOPE_IDENTITY() as int); ";
        readonly string update = "IF NOT EXISTS (SELECT * FROM Users WHERE Id = @Id) SELECT 0 "
                               + "ELSE IF NOT EXISTS (SELECT * FROM Users WHERE RowVersion = @RowVersion) SELECT -1 ELSE SELECT 1;"
                               + "UPDATE Users SET FirstName=@FirstName, LastName=@LastName, UserName=@UserName, Email = @Email, Description=@Description, IsAdminRole=@IsAdminRole, Roles = @Roles, IsActive=@IsActive,Password=@Password,AccountId =@AccountId, RefreshToken=@RefreshToken, RefreshTokenExpiryDateTime=@RefreshTokenExpiryDateTime, Modified=GETUTCDATE() "
                               + " WHERE Id = @Id AND RowVersion = @RowVersion";
        readonly string delete = "IF NOT EXISTS (SELECT * FROM Users WHERE Id = @Id) SELECT 0 "
                               + "ELSE SELECT 1;"
                               + "DELETE FROM Users"
                               + " WHERE Id = @Id";
        string IQuery.SelectAll => selectAll;
        string IQuery.SelectOne => selectOne;
        string IQuery.Insert => insert;
        string IQuery.Update => update;
        string IQuery.Delete => delete;
    }
}

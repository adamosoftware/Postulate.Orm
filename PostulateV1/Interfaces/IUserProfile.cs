using System;
using System.Data;

namespace Postulate.Orm.Interfaces
{
    public interface IUserProfile
    {
        string UserName { get; set; }

        DateTime GetLocalTime(IDbConnection connection);
    }
}
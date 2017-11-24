using System;

namespace Postulate.Orm.Interfaces
{
    public interface IUserProfile
    {
        string UserName { get; set; }

        DateTime GetLocalTime();
    }
}
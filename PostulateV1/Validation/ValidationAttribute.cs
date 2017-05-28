using System;
using System.Data;
using System.Reflection;

namespace Postulate.Orm.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public abstract class ValidationAttribute : Attribute
    {
        protected string _message;

        public ValidationAttribute(string message)
        {
            _message = message;
        }

        public string ErrorMessage { get { return _message; } }

        public abstract bool IsValid(PropertyInfo property, object value, IDbConnection connection = null);
    }
}
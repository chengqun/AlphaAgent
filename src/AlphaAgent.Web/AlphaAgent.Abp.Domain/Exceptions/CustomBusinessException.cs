using System;
using Volo.Abp; 

namespace AlphaAgent.Abp.Domain.Exceptions
{
    public class CustomBusinessException : BusinessException
    {
        public CustomBusinessException(
            string code = null,
            string message = null,
            string details = null,
            Exception innerException = null)
            : base(code, message, details, innerException)
        {
        }
    }
}
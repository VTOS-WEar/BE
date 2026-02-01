using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOS.Application.Abstractions
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
    }
}

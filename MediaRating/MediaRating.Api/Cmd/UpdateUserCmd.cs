using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.Api.Cmd
{
    public record UpdateUserCmd(string? Username, string? Password);
}

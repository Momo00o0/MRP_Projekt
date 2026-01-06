using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.DTOs
{
    public record MediaUpdateDto(
    string Title,
    string Description,
    int ReleaseYear,
    int AgeRestriction,
    MediaKind Kind
);
}

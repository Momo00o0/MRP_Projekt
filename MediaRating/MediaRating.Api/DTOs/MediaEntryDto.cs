using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.Api.DTOs
{
    public record MediaEntryDto(
        
        string Title,
        string Description,
        int ReleaseYear,
        List<string> Genres,
        int AgeRestriction,
        Guid UserGuid,
        MediaKind Kind
        );
    //A
    
}

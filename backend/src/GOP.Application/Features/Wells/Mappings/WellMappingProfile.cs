using AutoMapper;
using GOP.Application.Features.Wells.Queries.GetWellById;
using GOP.Application.Features.Wells.Queries.GetWellsList;
using GOP.Domain.Entities;

namespace GOP.Application.Features.Wells.Mappings;

internal sealed class WellMappingProfile : Profile
{
    public WellMappingProfile()
    {
        CreateMap<Well, WellListItemDto>()
            .ForCtorParam(nameof(WellListItemDto.Clasificacion), opt => opt.MapFrom(w => w.Clasificacion.ToString()))
            .ForCtorParam(nameof(WellListItemDto.Estado), opt => opt.MapFrom(w => w.Estado.ToString()))
            // Contrato y Campo se resuelven en el handler via ProjectTo o manual mapping
            .ForCtorParam(nameof(WellListItemDto.Contrato), opt => opt.MapFrom(w => string.Empty))
            .ForCtorParam(nameof(WellListItemDto.Campo), opt => opt.MapFrom(w => string.Empty));

        CreateMap<Well, WellDetailDto>()
            .ForCtorParam(nameof(WellDetailDto.TipoTrayectoria), opt => opt.MapFrom(w => w.TipoTrayectoria.ToString()))
            .ForCtorParam(nameof(WellDetailDto.Clasificacion), opt => opt.MapFrom(w => w.Clasificacion.ToString()))
            .ForCtorParam(nameof(WellDetailDto.TipoUbicacion), opt => opt.MapFrom(w => w.TipoUbicacion.ToString()))
            .ForCtorParam(nameof(WellDetailDto.TipoAngulo), opt => opt.MapFrom(w => w.TipoAngulo.ToString()))
            .ForCtorParam(nameof(WellDetailDto.TipoObjetivo), opt => opt.MapFrom(w => w.TipoObjetivo.ToString()))
            .ForCtorParam(nameof(WellDetailDto.TipoTerminacion), opt => opt.MapFrom(w => w.TipoTerminacion.ToString()))
            .ForCtorParam(nameof(WellDetailDto.Estado), opt => opt.MapFrom(w => w.Estado.ToString()))
            // Datos de ubicación desde owned entity
            .ForCtorParam(nameof(WellDetailDto.DepartamentoId), opt => opt.MapFrom(w => w.Location.DepartamentoId))
            .ForCtorParam(nameof(WellDetailDto.MunicipioId), opt => opt.MapFrom(w => w.Location.MunicipioId))
            .ForCtorParam(nameof(WellDetailDto.ClusterId), opt => opt.MapFrom(w => w.Location.ClusterId))
            .ForCtorParam(nameof(WellDetailDto.CodigoDaneDpto), opt => opt.MapFrom(w => w.Location.CodigoDaneDpto))
            .ForCtorParam(nameof(WellDetailDto.CodigoDaneMpio), opt => opt.MapFrom(w => w.Location.CodigoDaneMpio))
            // Nombres resueltos manualmente en el handler
            .ForCtorParam(nameof(WellDetailDto.Contrato), opt => opt.MapFrom(w => string.Empty))
            .ForCtorParam(nameof(WellDetailDto.Campo), opt => opt.MapFrom(w => string.Empty))
            .ForCtorParam(nameof(WellDetailDto.Departamento), opt => opt.MapFrom(w => string.Empty))
            .ForCtorParam(nameof(WellDetailDto.Municipio), opt => opt.MapFrom(w => string.Empty))
            .ForCtorParam(nameof(WellDetailDto.Cluster), opt => opt.MapFrom(w => (string?)null));
    }
}

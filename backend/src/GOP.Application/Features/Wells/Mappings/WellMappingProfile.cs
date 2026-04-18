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
            .ForCtorParam(nameof(WellListItemDto.Clasificacion),
                opt => opt.MapFrom(w => w.Clasificacion.ToString().ToUpperInvariant()))
            .ForCtorParam(nameof(WellListItemDto.SubClasificacion),
                opt => opt.MapFrom(w => w.SubClasificacion != null ? w.SubClasificacion.ToString() : null))
            .ForCtorParam(nameof(WellListItemDto.Estado),
                opt => opt.MapFrom(w => w.Estado.ToString().ToUpperInvariant()))
            .ForCtorParam(nameof(WellListItemDto.Uwi),
                opt => opt.MapFrom(w => w.Uwi))
            .ForCtorParam(nameof(WellListItemDto.Campo),
                opt => opt.MapFrom(w => w.Campo))
            .ForCtorParam(nameof(WellListItemDto.Contrato),
                opt => opt.MapFrom(w => w.Contrato));

        CreateMap<Well, WellDetailDto>()
            .ForCtorParam(nameof(WellDetailDto.TipoTrayectoria),
                opt => opt.MapFrom(w => w.TipoTrayectoria.ToString()))
            .ForCtorParam(nameof(WellDetailDto.Clasificacion),
                opt => opt.MapFrom(w => w.Clasificacion.ToString().ToUpperInvariant()))
            .ForCtorParam(nameof(WellDetailDto.SubClasificacion),
                opt => opt.MapFrom(w => w.SubClasificacion != null ? w.SubClasificacion.ToString() : null))
            .ForCtorParam(nameof(WellDetailDto.TipoUbicacion),
                opt => opt.MapFrom(w => w.TipoUbicacion.ToString().ToUpperInvariant()))
            .ForCtorParam(nameof(WellDetailDto.TipoAngulo),
                opt => opt.MapFrom(w => w.TipoAngulo.ToString()))
            .ForCtorParam(nameof(WellDetailDto.TipoObjetivo),
                opt => opt.MapFrom(w => w.TipoObjetivo.ToString()))
            .ForCtorParam(nameof(WellDetailDto.TipoTerminacion),
                opt => opt.MapFrom(w => w.TipoTerminacion.ToString()))
            .ForCtorParam(nameof(WellDetailDto.Estado),
                opt => opt.MapFrom(w => w.Estado.ToString().ToUpperInvariant()))
            .ForCtorParam(nameof(WellDetailDto.Uwi),
                opt => opt.MapFrom(w => w.Uwi))
            .ForCtorParam(nameof(WellDetailDto.Forma101Radicada),
                opt => opt.MapFrom(w => w.Forma101Radicada))
            // Ubicación aplanada
            .ForCtorParam(nameof(WellDetailDto.DepartamentoId),
                opt => opt.MapFrom(w => w.DepartamentoId))
            .ForCtorParam(nameof(WellDetailDto.Departamento),
                opt => opt.MapFrom(w => w.Departamento))
            .ForCtorParam(nameof(WellDetailDto.CodigoDaneDpto),
                opt => opt.MapFrom(w => w.CodigoDaneDpto))
            .ForCtorParam(nameof(WellDetailDto.MunicipioId),
                opt => opt.MapFrom(w => w.MunicipioId))
            .ForCtorParam(nameof(WellDetailDto.Municipio),
                opt => opt.MapFrom(w => w.Municipio))
            .ForCtorParam(nameof(WellDetailDto.CodigoDaneMpio),
                opt => opt.MapFrom(w => w.CodigoDaneMpio))
            .ForCtorParam(nameof(WellDetailDto.ClusterId),
                opt => opt.MapFrom(w => w.ClusterId))
            .ForCtorParam(nameof(WellDetailDto.Cluster),
                opt => opt.MapFrom(w => w.Cluster))
            .ForCtorParam(nameof(WellDetailDto.Contrato),
                opt => opt.MapFrom(w => w.Contrato))
            .ForCtorParam(nameof(WellDetailDto.Campo),
                opt => opt.MapFrom(w => w.Campo));
    }
}

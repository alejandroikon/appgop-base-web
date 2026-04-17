using AutoMapper;
using GOP.Domain.Entities;
using GOP.Application.Features.Wells.Queries.GetWellHistory;

namespace GOP.Application.Features.Wells.Mappings;

internal sealed class WellTransitionMappingProfile : Profile
{
    public WellTransitionMappingProfile()
    {
        CreateMap<WellTransitionHistory, TransitionHistoryItemDto>()
            .ConstructUsing(src => new TransitionHistoryItemDto(
                src.Id,
                src.FromState.ToString(),
                src.ToState.ToString(),
                src.Action.ToString(),
                src.Comment,
                src.PerformedByUserId,
                src.PerformedByName,
                src.PerformedByRole,
                src.CreatedAt));
    }
}

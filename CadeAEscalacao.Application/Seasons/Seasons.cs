using CadeAEscalacao.Application.Abstractions;
using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Groups;
using CadeAEscalacao.Domain.Seasons;

namespace CadeAEscalacao.Application.Seasons;

// Minimal season registration. The ranking is in Rankings/.

public sealed record SeasonResponse(Guid Id, Guid GroupId, string Name, DateOnly Start, DateOnly End)
{
    public static SeasonResponse From(Season season) =>
        new(season.Id.Value, season.GroupId.Value, season.Name, season.Start, season.End);
}

public sealed class CreateSeason(ISeasonRepository seasons, IGroupRepository groups, IUnitOfWork unitOfWork)
{
    public async Task<SeasonResponse> HandleAsync(
        GroupId groupId,
        string name,
        DateOnly start,
        int durationInMonths = Season.DefaultDurationInMonths,
        CancellationToken cancellationToken = default)
    {
        if (!await groups.ExistsAsync(groupId, cancellationToken))
        {
            throw new NotFoundException("Group", groupId);
        }

        var season = Season.Create(groupId, name, start, durationInMonths);

        // A match must belong to one season only, or it would count twice in attendances.
        if (await seasons.OverlapsAsync(groupId, season.Start, season.End, cancellationToken))
        {
            throw new ConflictException("The group already has a season in this period.");
        }

        seasons.Add(season);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SeasonResponse.From(season);
    }
}

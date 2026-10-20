using CadeAEscalacao.Application.Groups;
using CadeAEscalacao.Application.Lineups;
using CadeAEscalacao.Application.Lists;
using CadeAEscalacao.Application.Matches;
using CadeAEscalacao.Application.Players;
using CadeAEscalacao.Application.Rankings;
using CadeAEscalacao.Application.Seasons;
using Microsoft.Extensions.DependencyInjection;

namespace CadeAEscalacao.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the use cases: one class per use case with a <c>HandleAsync</c> method,
    /// injected straight into the endpoints (ADR-013).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreatePlayer>();
        services.AddScoped<GetPlayer>();

        services.AddScoped<CreateGroup>();
        services.AddScoped<ListMyGroups>();
        services.AddScoped<GetGroup>();
        services.AddScoped<AddGroupMember>();
        services.AddScoped<AddGroupMemberByEmail>();
        services.AddScoped<CreatePair>();
        services.AddScoped<ReorderCoachRotation>();

        services.AddScoped<CreateSeason>();
        services.AddScoped<ListGroupSeasons>();

        services.AddScoped<OpenWeeklyList>();
        services.AddScoped<GetWeeklyList>();
        services.AddScoped<ListGroupLists>();
        services.AddScoped<ChangeWeeklyList>();
        services.AddScoped<SignUpForList>();
        services.AddScoped<WithdrawFromList>();
        services.AddScoped<ChangeListPositions>();
        services.AddScoped<CloseWeeklyList>();
        services.AddScoped<CancelWeeklyList>();

        services.AddScoped<StartLineup>();
        services.AddScoped<GetLineup>();
        services.AddScoped<SetPartnerOfTheDay>();
        services.AddScoped<AssignTeams>();
        services.AddScoped<ChoosePartnerTeam>();

        services.AddScoped<RecordMatch>();
        services.AddScoped<GetMatch>();
        services.AddScoped<CorrectScore>();

        services.AddScoped<SeasonAttendance>();
        services.AddScoped<GetSeasonRanking>();

        return services;
    }
}

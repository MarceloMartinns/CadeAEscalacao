using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CadeAEscalacao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    match_day = table.Column<int>(type: "integer", nullable: false),
                    president_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    coach_rotation = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    bench_players_per_team = table.Column<int>(type: "integer", nullable: false),
                    players_per_team = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_groups_players_president_id",
                        column: x => x.president_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_members", x => new { x.group_id, x.player_id });
                    table.ForeignKey(
                        name: "fk_group_members_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_members_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_pairs",
                columns: table => new
                {
                    first_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    second_player_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_pairs", x => new { x.group_id, x.first_player_id });
                    table.ForeignKey(
                        name: "fk_group_pairs_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_pairs_players_first_player_id",
                        column: x => x.first_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_pairs_players_second_player_id",
                        column: x => x.second_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                    table.ForeignKey(
                        name: "fk_seasons_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "weekly_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_date = table.Column<DateOnly>(type: "date", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_lists_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lineups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weekly_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    players = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    team_a = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    team_b = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lineups", x => x.id);
                    table.ForeignKey(
                        name: "fk_lineups_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lineups_players_coach_id",
                        column: x => x.coach_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lineups_players_partner_id",
                        column: x => x.partner_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lineups_weekly_lists_weekly_list_id",
                        column: x => x.weekly_list_id,
                        principalTable: "weekly_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "weekly_list_entries",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weekly_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    positions = table.Column<int>(type: "integer", nullable: false),
                    has_reserved_spot = table.Column<bool>(type: "boolean", nullable: false),
                    sign_up_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_list_entries", x => new { x.weekly_list_id, x.player_id });
                    table.ForeignKey(
                        name: "fk_weekly_list_entries_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_weekly_list_entries_weekly_lists_weekly_list_id",
                        column: x => x.weekly_list_id,
                        principalTable: "weekly_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lineup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_date = table.Column<DateOnly>(type: "date", nullable: false),
                    goals_a = table.Column<int>(type: "integer", nullable: false),
                    goals_b = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    team_a = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    team_b = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matches", x => x.id);
                    table.ForeignKey(
                        name: "fk_matches_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_matches_lineups_lineup_id",
                        column: x => x.lineup_id,
                        principalTable: "lineups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_group_members_player_id",
                table: "group_members",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_pairs_first_player_id",
                table: "group_pairs",
                column: "first_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_pairs_group_id_second",
                table: "group_pairs",
                columns: new[] { "group_id", "second_player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_pairs_second_player_id",
                table: "group_pairs",
                column: "second_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_president_id",
                table: "groups",
                column: "president_id");

            migrationBuilder.CreateIndex(
                name: "ix_lineups_coach_id",
                table: "lineups",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "ix_lineups_group_id",
                table: "lineups",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_lineups_partner_id",
                table: "lineups",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_lineups_weekly_list_id",
                table: "lineups",
                column: "weekly_list_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_matches_group_id_match_date",
                table: "matches",
                columns: new[] { "group_id", "match_date" });

            migrationBuilder.CreateIndex(
                name: "ix_matches_lineup_id",
                table: "matches",
                column: "lineup_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seasons_group_id",
                table: "seasons",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_list_entries_player_id",
                table: "weekly_list_entries",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_lists_group_id_match_date",
                table: "weekly_lists",
                columns: new[] { "group_id", "match_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "group_pairs");

            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "weekly_list_entries");

            migrationBuilder.DropTable(
                name: "lineups");

            migrationBuilder.DropTable(
                name: "weekly_lists");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "players");
        }
    }
}

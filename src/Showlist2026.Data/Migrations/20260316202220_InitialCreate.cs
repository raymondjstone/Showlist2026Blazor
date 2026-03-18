using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Country",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenreText",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    genre = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenreText", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Language",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Language", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShowUpdated",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    showudatedid = table.Column<long>(type: "bigint", nullable: false),
                    xshowid = table.Column<long>(type: "bigint", nullable: false),
                    updatedTimeStamp = table.Column<long>(type: "bigint", nullable: false),
                    lastupdateprocessed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowUpdated", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timezone",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    timezone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UTCOffset = table.Column<double>(type: "float", nullable: false),
                    UTCDSTOffset = table.Column<double>(type: "float", nullable: false),
                    countrycode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timezone", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Touchfolder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Touchfolder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TVDirectories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DaysToScan = table.Column<int>(type: "int", nullable: false),
                    Filter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinFileSize = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TVDirectories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TVSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    URLTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TVSites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Type",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Type", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCountrySelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    include = table.Column<bool>(type: "bit", nullable: false),
                    countryId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCountrySelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCountrySelection_Country_countryId",
                        column: x => x.countryId,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGenreSelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    include = table.Column<bool>(type: "bit", nullable: false),
                    genretextId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGenreSelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGenreSelection_GenreText_genretextId",
                        column: x => x.genretextId,
                        principalTable: "GenreText",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLanguageSelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    include = table.Column<bool>(type: "bit", nullable: false),
                    languageId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLanguageSelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLanguageSelection_Language_languageId",
                        column: x => x.languageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Network",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    networkid = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    timezone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    countryId = table.Column<int>(type: "int", nullable: true),
                    tzId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Network", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Network_Country_countryId",
                        column: x => x.countryId,
                        principalTable: "Country",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Network_Timezone_tzId",
                        column: x => x.tzId,
                        principalTable: "Timezone",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WebNetwork",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    webid = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    timezone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    countryId = table.Column<int>(type: "int", nullable: true),
                    tzId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebNetwork", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebNetwork_Country_countryId",
                        column: x => x.countryId,
                        principalTable: "Country",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WebNetwork_Timezone_tzId",
                        column: x => x.tzId,
                        principalTable: "Timezone",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserTypeSelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    include = table.Column<bool>(type: "bit", nullable: false),
                    typeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTypeSelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTypeSelection_Type_typeId",
                        column: x => x.typeId,
                        principalTable: "Type",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNetworkSelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    include = table.Column<bool>(type: "bit", nullable: false),
                    networkId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNetworkSelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNetworkSelection_Network_networkId",
                        column: x => x.networkId,
                        principalTable: "Network",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Show",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    showid = table.Column<long>(type: "bigint", nullable: false),
                    page = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    scheduletime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    scheduledays = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    premiered = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imagemed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imageorig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    needsupdate = table.Column<bool>(type: "bit", nullable: false),
                    ShowUpdatedsId = table.Column<int>(type: "int", nullable: true),
                    NetworksId = table.Column<int>(type: "int", nullable: true),
                    WebNetworksId = table.Column<int>(type: "int", nullable: true),
                    TypesId = table.Column<int>(type: "int", nullable: true),
                    LanguagesId = table.Column<int>(type: "int", nullable: true),
                    FolderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tvrage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    thetvdb = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    imdb = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Show", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Show_Language_LanguagesId",
                        column: x => x.LanguagesId,
                        principalTable: "Language",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Show_Network_NetworksId",
                        column: x => x.NetworksId,
                        principalTable: "Network",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Show_ShowUpdated_ShowUpdatedsId",
                        column: x => x.ShowUpdatedsId,
                        principalTable: "ShowUpdated",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Show_Type_TypesId",
                        column: x => x.TypesId,
                        principalTable: "Type",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Show_WebNetwork_WebNetworksId",
                        column: x => x.WebNetworksId,
                        principalTable: "WebNetwork",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserWebNetworkSelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    include = table.Column<bool>(type: "bit", nullable: false),
                    webnetworkId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWebNetworkSelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWebNetworkSelection_WebNetwork_webnetworkId",
                        column: x => x.webnetworkId,
                        principalTable: "WebNetwork",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Episode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    episodeid = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    season = table.Column<long>(type: "bigint", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    airdate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    airtime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    runtime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imagemedium = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imageoriginal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    summary = table.Column<string>(type: "varchar(MAX)", nullable: true),
                    links = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AirDateOffset2 = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    showId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episode_Show_showId",
                        column: x => x.showId,
                        principalTable: "Show",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Genre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    showId = table.Column<int>(type: "int", nullable: false),
                    genretextId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genre", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Genre_GenreText_genretextId",
                        column: x => x.genretextId,
                        principalTable: "GenreText",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Genre_Show_showId",
                        column: x => x.showId,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserShowSelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    include = table.Column<bool>(type: "bit", nullable: false),
                    showId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserShowSelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserShowSelection_Show_showId",
                        column: x => x.showId,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Touchfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasRealFile = table.Column<bool>(type: "bit", nullable: false),
                    FileDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EpisodeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Touchfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Touchfile_Episode_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episode",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserWatchedSelection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    episodeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWatchedSelection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWatchedSelection_Episode_episodeId",
                        column: x => x.episodeId,
                        principalTable: "Episode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Episode_showId",
                table: "Episode",
                column: "showId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_genretextId",
                table: "Genre",
                column: "genretextId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_showId",
                table: "Genre",
                column: "showId");

            migrationBuilder.CreateIndex(
                name: "IX_Network_countryId",
                table: "Network",
                column: "countryId");

            migrationBuilder.CreateIndex(
                name: "IX_Network_tzId",
                table: "Network",
                column: "tzId");

            migrationBuilder.CreateIndex(
                name: "IX_Show_LanguagesId",
                table: "Show",
                column: "LanguagesId");

            migrationBuilder.CreateIndex(
                name: "IX_Show_NetworksId",
                table: "Show",
                column: "NetworksId");

            migrationBuilder.CreateIndex(
                name: "IX_Show_ShowUpdatedsId",
                table: "Show",
                column: "ShowUpdatedsId");

            migrationBuilder.CreateIndex(
                name: "IX_Show_TypesId",
                table: "Show",
                column: "TypesId");

            migrationBuilder.CreateIndex(
                name: "IX_Show_WebNetworksId",
                table: "Show",
                column: "WebNetworksId");

            migrationBuilder.CreateIndex(
                name: "IX_Touchfile_EpisodeId",
                table: "Touchfile",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCountrySelection_countryId",
                table: "UserCountrySelection",
                column: "countryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGenreSelection_genretextId",
                table: "UserGenreSelection",
                column: "genretextId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLanguageSelection_languageId",
                table: "UserLanguageSelection",
                column: "languageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkSelection_networkId",
                table: "UserNetworkSelection",
                column: "networkId");

            migrationBuilder.CreateIndex(
                name: "IX_UserShowSelection_showId",
                table: "UserShowSelection",
                column: "showId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTypeSelection_typeId",
                table: "UserTypeSelection",
                column: "typeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWatchedSelection_episodeId",
                table: "UserWatchedSelection",
                column: "episodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWebNetworkSelection_webnetworkId",
                table: "UserWebNetworkSelection",
                column: "webnetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_WebNetwork_countryId",
                table: "WebNetwork",
                column: "countryId");

            migrationBuilder.CreateIndex(
                name: "IX_WebNetwork_tzId",
                table: "WebNetwork",
                column: "tzId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Genre");

            migrationBuilder.DropTable(
                name: "Touchfile");

            migrationBuilder.DropTable(
                name: "Touchfolder");

            migrationBuilder.DropTable(
                name: "TVDirectories");

            migrationBuilder.DropTable(
                name: "TVSites");

            migrationBuilder.DropTable(
                name: "UserCountrySelection");

            migrationBuilder.DropTable(
                name: "UserGenreSelection");

            migrationBuilder.DropTable(
                name: "UserLanguageSelection");

            migrationBuilder.DropTable(
                name: "UserNetworkSelection");

            migrationBuilder.DropTable(
                name: "UserShowSelection");

            migrationBuilder.DropTable(
                name: "UserTypeSelection");

            migrationBuilder.DropTable(
                name: "UserWatchedSelection");

            migrationBuilder.DropTable(
                name: "UserWebNetworkSelection");

            migrationBuilder.DropTable(
                name: "GenreText");

            migrationBuilder.DropTable(
                name: "Episode");

            migrationBuilder.DropTable(
                name: "Show");

            migrationBuilder.DropTable(
                name: "Language");

            migrationBuilder.DropTable(
                name: "Network");

            migrationBuilder.DropTable(
                name: "ShowUpdated");

            migrationBuilder.DropTable(
                name: "Type");

            migrationBuilder.DropTable(
                name: "WebNetwork");

            migrationBuilder.DropTable(
                name: "Country");

            migrationBuilder.DropTable(
                name: "Timezone");
        }
    }
}

using System.Diagnostics.CodeAnalysis;

// Marks every EF Core migration (scaffolded T-SQL/DDL, only meaningfully exercised by
// Database.Migrate() against a real SQL Server - see src/Showlist2026.Tests/NOTES.md) as
// excluded from code coverage via the standard [ExcludeFromCodeCoverage] attribute.
//
// This is a hand-authored companion file, not a generated one - it never touches the actual
// migration/snapshot files. Each class here is `partial` and deliberately does NOT repeat the
// base class (C# only allows a base class to be specified in one partial declaration; the
// generated file already specifies it), so this just adds the attribute to an existing type.
// `dotnet ef migrations add/remove` can't ever clobber this file. The only maintenance cost:
// add one line here when a new migration is scaffolded.
//
// [ExcludeFromCodeCoverage] is honored, with no extra configuration, by every coverage engine
// used with this repo: coverlet (dotnet test), Microsoft's native collector (Visual Studio
// Enterprise's "Analyze Code Coverage"), and dotCover/ReSharper's --exclude-attributes filter.

namespace Showlist2026.Data.Migrations
{
    [ExcludeFromCodeCoverage] public partial class InitialCreate { }
    [ExcludeFromCodeCoverage] public partial class DropAbpTables { }
    [ExcludeFromCodeCoverage] public partial class AddNotesAndPriorityAndWatchedHistory { }
    [ExcludeFromCodeCoverage] public partial class AddPerformanceIndexes { }
    [ExcludeFromCodeCoverage] public partial class AddAppSettingTable { }
    [ExcludeFromCodeCoverage] public partial class AddUserGivenUpSelectionTable { }
    [ExcludeFromCodeCoverage] public partial class MoveWatchedGivenUpToEpisode { }
    [ExcludeFromCodeCoverage] public partial class MoveWantedPriorityToShow { }
    [ExcludeFromCodeCoverage] public partial class MoveWantedToFilterEntities { }
    [ExcludeFromCodeCoverage] public partial class AddApiKeyToTVSites { }
    [ExcludeFromCodeCoverage] public partial class AddRssApiKeyToTVSites { }
    [ExcludeFromCodeCoverage] public partial class AddShowFolderAliasesAndAliasable { }
    [ExcludeFromCodeCoverage] public partial class AddFriends { }
    [ExcludeFromCodeCoverage] public partial class AddSeasonOffsetToShowFolderAlias { }
    [ExcludeFromCodeCoverage] public partial class AddShowLinks { }

    [ExcludeFromCodeCoverage] partial class ShowlistDbContextModelSnapshot { }
}

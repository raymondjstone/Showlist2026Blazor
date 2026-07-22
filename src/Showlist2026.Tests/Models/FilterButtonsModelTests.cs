using Showlist2026.Models;
using Xunit;

namespace Showlist2026.Tests.Models;

public class FilterButtonsModelTests
{
    [Fact]
    public void NegativeKey_NeverShowsEitherButton()
    {
        var model = new FilterButtonsModel("show", null, -1);
        Assert.False(model.ShowPlus);
        Assert.False(model.ShowNegative);
    }

    [Fact]
    public void UndecidedStatus_ShowsBothButtons()
    {
        var model = new FilterButtonsModel("show", null, 1);
        Assert.True(model.ShowPlus);
        Assert.True(model.ShowNegative);
    }

    [Fact]
    public void IncludedStatus_ShowsOnlyNegativeButton()
    {
        var model = new FilterButtonsModel("show", true, 1);
        Assert.False(model.ShowPlus);
        Assert.True(model.ShowNegative);
    }

    [Fact]
    public void ExcludedStatus_ShowsOnlyPlusButton()
    {
        var model = new FilterButtonsModel("show", false, 1);
        Assert.True(model.ShowPlus);
        Assert.False(model.ShowNegative);
    }
}

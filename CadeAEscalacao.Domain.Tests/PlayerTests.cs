using CadeAEscalacao.Domain.Players;

namespace CadeAEscalacao.Domain.Tests;

public class PlayerTests
{
    [Test]
    public void Create_ValidName_TrimsName()
    {
        var player = Player.Create("  Marcelo  ");

        player.Name.ShouldBe("Marcelo");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_EmptyName_Throws(string name)
    {
        Should.Throw<ArgumentException>(() => Player.Create(name));
    }

    [Test]
    public void Create_TwoPlayers_GeneratesDifferentIds()
    {
        var first = Player.Create("Ana");
        var second = Player.Create("Bruno");

        first.Id.ShouldNotBe(second.Id);
    }

    [Test]
    public void Rename_ValidName_ChangesName()
    {
        var player = Player.Create("Marcelo");

        player.Rename("Marcelinho");

        player.Name.ShouldBe("Marcelinho");
    }
}

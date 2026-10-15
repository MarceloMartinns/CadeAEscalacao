namespace CadeAEscalacao.Domain.Players;

/// <summary>
/// A person who plays. Being a regular or a substitute depends on the group
/// (the same person may be a regular on Tuesday and a substitute in futsal),
/// so the <see cref="PlayerType"/> lives in <see cref="Groups.GroupMember"/>.
/// </summary>
public sealed class Player
{
    private Player(PlayerId id, string name)
    {
        Id = id;
        Name = name;
    }

    public PlayerId Id { get; }

    public string Name { get; private set; }

    public static Player Create(string name) => new(PlayerId.New(), NormalizeName(name));

    public void Rename(string name) => Name = NormalizeName(name);

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}

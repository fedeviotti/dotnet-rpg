using dotnet_rpg.Models;

namespace dotnet_rpg.Services.CharacterService;

public class CharacterService : ICharacterService
{
    private static readonly List<Character> Characters = new List<Character>
    {
        new Character(),
        new Character { Id = 1, Name = "Sam" }
    };
    
    public List<Character> GetAllCharacters()
    {
        return Characters;
    }

    public Character GetCharacterById(int id)
    {
        var character = Characters.FirstOrDefault(c => c.Id == id);
        if (character == null)
        {
            throw new Exception("Character not found");
        }

        return character;
    }

    public List<Character> AddCharacter(Character newCharacter)
    {
        Characters.Add(newCharacter);
        return Characters;
    }
}
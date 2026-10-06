using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public interface ILanguage
{
	IEnumerable<char> GetForbiddenStartOfLineCharacters();

	bool IsCharacterForbiddenAtStartOfLine(char character);

	IEnumerable<char> GetForbiddenEndOfLineCharacters();

	bool IsCharacterForbiddenAtEndOfLine(char character);

	string GetLanguageID();

	string GetDefaultFontName();

	Font GetDefaultFont();

	char GetLineSeperatorChar();

	bool DoesLanguageRequireSpaceForNewline();

	bool FontMapHasKey(string keyFontName);

	Font GetMappedFont(string keyFontName);
}

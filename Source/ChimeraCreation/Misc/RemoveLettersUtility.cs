using System.Collections.Generic;
using Verse;

namespace AnomalyAllies.Misc
{
    public static class RemoveLettersUtility
    {
        public static Letter RemoveLastLetter(this LetterStack letterStack, bool removeFromArchive = false)
        {
            List<Letter> letters = letterStack.ForceGetField<List<Letter>>("letters");
            Letter lastLetter = letters.PopFront();
            lastLetter.Removed();

            if (removeFromArchive)
                Find.Archive.Remove(lastLetter);

            return lastLetter;
        }
    }
}

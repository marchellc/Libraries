using System;

namespace Common.Logging
{
    public struct LogCharacter
    {
        public char Character;
        public ConsoleColor Color;

        public LogCharacter(char c, ConsoleColor color = ConsoleColor.White)
        {
            Character = c;
            Color = color;
        }
    }
}
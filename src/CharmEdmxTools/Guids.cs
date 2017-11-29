// Guids.cs
// MUST match guids.h

using System;

namespace CharmEdmxTools
{

    internal static class FileExtensions
    {
        public const string CSharp = ".cs";
        public const string VisualBasic = ".vb";
        public const string EntityDataModel = ".edmx";
        public const string Xml = ".xml";
        public const string Sql = ".sql";
    }
    static class GuidList
    {
        public const string guidCharmEdmxToolsPkgString = "1601c91e-ce52-4571-b547-b26295df8eb9";
        public const string guidCharmEdmxToolsCmdSetString = "28d08155-17d3-4ee4-b5ab-e2782a09d442";

        public static readonly Guid guidCharmEdmxToolsCmdSet = new Guid(guidCharmEdmxToolsCmdSetString);
    };
}
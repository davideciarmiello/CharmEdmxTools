// Guids.cs
// MUST match guids.h
using System;
using System.ComponentModel.Design;

namespace CharmEdmxTools
{
    static class GuidList
    {
        public const string guidCharmEdmxToolsPkgString = "7ced666a-8d1e-4d19-bc17-3114c8a84ab7";
        //public const string guidCharmEdmxToolsCmdSetString = "b7a267b8-0368-4504-9f9b-3003f9d637d3";

        public static readonly Guid guidCharmEdmxToolsCmdSet = new Guid("b7a267b8-0368-4504-9f9b-3003f9d637df");

        //public static readonly CommandID cmdidEdmxToolbarFixTopLevelMenu = new CommandID(guidDbContextPackageCmdSet, 0x1021);
        //public static readonly CommandID cmdidEdmxToolbarFix = new CommandID(guidDbContextPackageCmdSet, 0x0026);


        /*
    static class GuidList
    {
        public const string guidCharmEdmxToolsPkgString = "1601c91e-ce52-4571-b547-b26295df8eb9";
        public const string guidCharmEdmxToolsPkgString2017 = "1601c91e-ce52-4571-b547-b26295df8eb8";
        public const string guidCharmEdmxToolsCmdSetString = "28d08155-17d3-4ee4-b5ab-e2782a09d442";

        public static readonly Guid guidCharmEdmxToolsCmdSet = new Guid(guidCharmEdmxToolsCmdSetString);
    };*/

    };


    internal static class FileExtensions
    {
        public const string CSharp = ".cs";
        public const string VisualBasic = ".vb";
        public const string EntityDataModel = ".edmx";
        public const string Xml = ".xml";
        public const string Sql = ".sql";
    }
}
﻿// PkgCmdID.cs
// MUST match PkgCmdID.h

namespace CharmEdmxTools
{
    static class PkgCmdIDList
    {
        public const uint cmdidMyCommand = 0x100;


        public const uint cmdidEdmxExecAllFixs = 0x100;
        public const uint cmdidEdmxClearAllProperties = 0x300;

        public const uint cmdidEdmxToolbarFixUpper = 0x0026;
        public const uint cmdidEdmxToolbarFix = 0x0026;
        public static uint? TopLevelMenu = 0x1021;


        //public const uint cmdidEdmxOracleFix =        0x100;
        public const uint cmdidViewEntityDataModel = 0x100;
        public const uint cmdidViewEntityDataModelXml = 0x200;
        public const uint cmdidViewEntityModelDdl = 0x400;
        public const uint cmdidReverseEngineerCodeFirst = 0x001;
        public const uint cmdidCustomizeReverseEngineerTemplates = 0x005;

    };
}
using System;
using EngineA;

namespace DataFile
{
    public static class DB
    {
        public static Unit_Lib_Entry UnitLib = new Unit_Lib_Entry();
        public static Terrain terrain = new Terrain();
        public static Setup setup = new Setup();
        public static STATUS status = new STATUS();                    /* statuses defined in engine_tools.h */
        public static Scenario scen = new Scenario();
#if TODO_RR
        public static campaign camp = new campaign();
#endif
        public static Map map = new Map();
    }
}

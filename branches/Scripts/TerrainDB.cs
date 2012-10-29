using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using EngineA;


namespace DataFile
{
    public class Terrain_TypeDB
    {
        public string id;
        public string name;
        public int[] spt;
		[XmlIgnore]
        public int[,] mov;
        public int[][] Mov {
            get {
                int[][] arr = new int[TerrainDB.rows_mov][];
                for (int i = 0; i < TerrainDB.rows_mov; i++)
                {
                    arr[i] = new int[TerrainDB.col_mov];
                }

                for (int i = 0; i < TerrainDB.rows_mov; i++)
                {
                    for (int j = 0; j < TerrainDB.col_mov; j++)
                    {
                        arr[i][j] = mov[i,j];
                    }
                }
                return arr;
            }
			set {
                mov = new int[TerrainDB.rows_mov,TerrainDB.col_mov];
                for (int i = 0; i < TerrainDB.rows_mov; i++)
                {
                    for (int j = 0; j < TerrainDB.col_mov; j++)
                    {
                        mov[i, j]=value[i][j];
                    }
                }
            }
        }
        public int min_entr;
        public int max_entr;
        public int max_ini;
        public Terrain_flags[] flags;
		public string[] images_name;
    }

    public class TerrainDB
    {
        public Weather_Type[] weatherTypes;
        public int weatherTypeCount;
        public string fog;       /* mask used to create fog */
		public string danger;
        public string grid;
        public string selectP;    /* selecting frame picture */
        public string cross;     /* crosshair animation */
        public Terrain_TypeDB[] terrainTypes;
        public int terrainTypeCount;
        public static int rows_mov = Enum.GetNames(typeof (weather)).Length;
        public static int col_mov = DB.UnitLib.mov_type_count;


        public void terrainTOterrainDB(Terrain terr) {
            this.weatherTypeCount = terr.weatherTypeCount;
            this.weatherTypes = terr.weatherTypes;
			rows_mov = weatherTypeCount;
			if (terr.terrainIcons.fog!=null)
            	this.fog = terr.terrainIcons.fog.name;
			if (terr.terrainIcons.danger!=null)
				this.danger = terr.terrainIcons.danger.name;
			if (terr.terrainIcons.grid!=null)
            	this.grid = terr.terrainIcons.grid.name;
			if (terr.terrainIcons.selectP!=null)
            	this.selectP = terr.terrainIcons.selectP.name;
			if (terr.terrainIcons.cross!=null)
            	this.cross = terr.terrainIcons.cross.name;
            this.terrainTypeCount = terr.terrainTypeCount;
            this.terrainTypes = new Terrain_TypeDB[terrainTypeCount];
            for (int i = 0; i < terrainTypes.Length; i++) {
                terrainTypes[i] = new Terrain_TypeDB();
                terrainTypes[i].id = terr.terrainTypes[i].id;
                terrainTypes[i].name = terr.terrainTypes[i].name;
                terrainTypes[i].spt = terr.terrainTypes[i].spt;
				terrainTypes[i].mov = terr.terrainTypes[i].mov;
                terrainTypes[i].min_entr = terr.terrainTypes[i].min_entr;
                terrainTypes[i].max_entr = terr.terrainTypes[i].max_entr;
                terrainTypes[i].max_ini = terr.terrainTypes[i].max_ini;
                terrainTypes[i].flags = terr.terrainTypes[i].flags;
				terrainTypes[i].images_name = terr.terrainTypes[i].images;
				/*for (int j = 0; j < weatherTypeCount; j++)
                {
                    terrainTypes[i].images_name[j] = terr.terrainTypes[i].images[j].name;
                }*/
            }

                 
        }
    }
}

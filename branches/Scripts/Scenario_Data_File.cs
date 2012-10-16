using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Engine;

namespace DataFile
{
    [Serializable]
    public class Scenario_Data_File
    {

        public Scen_Info scen_info;
        public string nation_db_name;
        public string unit_db_name;
        [XmlIgnore]
        public List<string> unitsToAdd;
        public List<string> UnitsToAdd
        {
            get
            {
                return unitsToAdd;
            }
            set
            {
                unitsToAdd = value;
            }
        }
        public string map_fname;
        public int[] weather;
        [XmlIgnore]
        public List<Player> list_players;
        public List<Player> List_Players
        {
            get
            {
                return list_players;
            }
            set
            {
                list_players = value;
            }
        }
        [XmlIgnore]
        public List<Flag> list_flags;
        public List<Flag> List_flags
        {
            get
            {
                return list_flags;
            }
            set
            {
                list_flags = value;
            }
        }
        public VCOND_CHECK vcond_check_type = 0;
        public int vcond_count = 0;
        public VCond[] vconds;
        [XmlIgnore]
        public List<Unit> units;
        public List<Unit> Units {
            get
            {
                return units;
            }
            set
            {
                units = value;
            }
        }
        public Player player;
		public Map map;

        public void scenTOscen_data(string nationdbname, string unitdbname) {
            this.scen_info = Scenario.scen_info;
            this.nation_db_name = nationdbname;
            this.unit_db_name = unitdbname;
            this.unitsToAdd = Scenario.unitsToAdd;
            this.map_fname = Scenario.map_fname;
			if (string.IsNullOrEmpty(this.map_fname)){
				this.map = Scenario.mapScen;
			}
            this.weather = Scenario.weather;
            this.list_flags = Scenario.list_flags;
            this.list_players = Player.players;
            this.vcond_check_type = Scenario.vcond_check_type;
            this.vcond_count = Scenario.vcond_count;
            this.vconds = Scenario.vconds;
            this.player = Scenario.player;
            this.units = Scenario.units;
			 foreach (Unit ubase in this.units){
                ubase.name = ubase.DeleteOrdinal(ubase.name);
            }
			
        }
    }
}
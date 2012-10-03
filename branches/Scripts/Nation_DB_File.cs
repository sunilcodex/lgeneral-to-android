using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Engine;

namespace DataFile
{
    [Serializable]
    public class Nation_DB_File
    {

        public string id;
        public string name;
        public int flag_offset;

        public Nation[] nations;
        public int nation_count = 0;
        public string nation_flags_img;
        public int nation_flag_width = 0;
        public int nation_flag_height = 0;

        /// <summary>
        /// Convert the static attributes of Nation in a object to
        /// Serializer them
        /// </summary>
        public void nationTOnationDBatt()
        {
            this.nations = new Nation[Nation.nations.Length];
            for (int i = 0; i < Nation.nations.Length; i++) {
                this.nations[i] = new Nation();
                this.nations[i].Name = Nation.nations[i].Name;
                this.nations[i].ID = Nation.nations[i].ID;
                this.nations[i].Flag_offset = Nation.nations[i].Flag_offset;
            }
            this.nation_count = Nation.nation_count;
            string str = Nation.nation_flags.name.Replace("data\\gfx","Textures");
            str = str.Replace(".bmp","");
            this.nation_flags_img = str.Replace("\\","/");
            this.nation_flag_width = Nation.nation_flag_width;
            this.nation_flag_height = Nation.nation_flag_height;
        }

        public void nationDBTOnationatt()
        {
            Nation.nations = this.nations;
            Nation.nation_count = this.nation_count;
            Nation.nation_flags = SDL_Surface.LoadSurface(this.nation_flags_img, false);
            Nation.nation_flag_height = this.nation_flag_height;
            Nation.nation_flag_width = this.nation_flag_width;
        }
    }
    
}

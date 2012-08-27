
public class TextureTable {

	public static int GetMaxTextureOf(string name){
		if (name.Equals("airfield") || name.Equals("fog") 
			|| name.Equals("grid") || name.Equals("select_frame")){
			return 1;
		}
		else if (name.Equals("clear")){
			return 19;
		}
		else if (name.Equals("desert")){
			return 9;
		}
		else if (name.Equals("fields")){
			return 3;
		}
		else if (name.Equals("forest")){
			return 7;
		}
		else if (name.Equals("fort")){
			return 18;
		}
		else if (name.Equals("harbor") || name.Equals("rough")){
			return 6;
		}
		else if (name.Equals("mountain")){
			return 79;
		}
		else if (name.Equals("ocean")){
			return 29;
		}
		else if (name.Equals("road")){
			return 31;
		}
		else if (name.Equals("rough_desert")){
			return 5;
		}
		else if (name.Equals("swamp") || name.Equals("town")){
			return 4;
		}
		else if (name.Equals("river")){
			return 16;
		}
		else{
			return -1;
		}
		
	}
}

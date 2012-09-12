using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace Engine
{
	public interface IParseable<T>
	{
		T Parse (string str);
	}

	[Serializable]
    public class Matrix<T> : IXmlSerializable where T : IParseable<T>, new()
	{
		public int Width { get; private set; }

		public int Height { get; private set; }

		T[,] data;

		public Matrix ()
		{
			Width = 0;
			Height = 0;
		}

		public Matrix (int w, int h)
		{
			if (w < 0 || h <0)
				throw new Exception ("Width or Height less or equal than zero");
			Width = w;
			Height = h;
			data = new T[w, h];
		}

		public T this [int i, int j] {
			get 
			{ 
				if (i < 0 || j <0)
					throw new Exception ("Width or Height less or equal than zero");
				return data [i, j];
			}
			set 
			{ 
				if (i <0 || j <0)
					throw new Exception ("Width or Height less or equal than zero");
				data [i, j] = value;
			}
		}

		public System.Xml.Schema.XmlSchema GetSchema ()
		{
			return (null);
		}

		public void ReadXml (XmlReader reader)
		{
			reader.MoveToContent ();
			Boolean isEmptyElement = reader.IsEmptyElement; // (1)
			reader.ReadStartElement ();
			if (!isEmptyElement) { // (1)
				Width = int.Parse (reader.ReadElementString ("width"));
				Height = int.Parse (reader.ReadElementString ("height"));
				data = new T[Width, Height];
				for (int j = 0; j < Height; j++) {
					for (int i = 0; i < Width; i++) {
						string str = reader.ReadElementString ("Tile");
						data [i, j] = new T ().Parse (str);
					}
				}
			}
		}

		public void WriteXml (XmlWriter writer)
		{
			writer.WriteElementString ("width", Width.ToString ());
			writer.WriteElementString ("height", Height.ToString ());
			for (int j = 0; j < Height; j++) {
				for (int i = 0; i < Width; i++) {
					writer.WriteElementString ("Tile", data [i, j].ToString ());
				}
			}
		}
	}
}

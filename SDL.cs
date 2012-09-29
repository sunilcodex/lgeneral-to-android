/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 12/01/2009
 * Hora: 13:49
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */
using UnityEngine;
using System;

//TODO_RR 	using System.Drawing;
//TODO_RR using System.Drawing.Drawing2D;
//TODO_RR using System.Drawing.Imaging;

namespace Engine
{
    /// <summary>
    /// a dummy SDL_Surface.
    /// </summary>
    public class SDL_Surface
    {
        public int w, h;
        public Texture2D bitmap;
        //TODO_RR public System.Drawing.Graphics surf;
        public string name;
		public Material bitmapMaterial;

        public SDL_Surface()
        {
        }

        public Texture2D Bitmap
        {
            get { return bitmap; }
            set { bitmap = value; }
        }

#if TODO_RR
        public static Int32 SetPixel(SDL_Surface surf, int x, int y, int pixel)
        {
            surf.bitmap.SetPixel(x, y, Color.FromArgb(pixel));
            return surf.bitmap.GetPixel(x, y).ToArgb();
        }
#endif
#if TODO_RR
        public static Int32 GetPixel(SDL_Surface surf, int x, int y)
        {
            return surf.bitmap.GetPixel(x, y).ToArgb();
        }
#endif
        /*
        load a surface from file putting it in soft or hardware mem
        */

        public static SDL_Surface LoadSurface(string fname, bool applyTransparency)
        {
#if TODO_RR
			Falta aplicar la transparencia
#endif
			SDL_Surface sdl = new SDL_Surface();
            try
            {
				
                if (fname != null)
                {
					sdl.bitmap = Resources.Load(fname) as Texture2D;
                    sdl.name = fname;
					if (applyTransparency){
						
						sdl.bitmapMaterial = new Material(Shader.Find("Transparent/Diffuse"));
						sdl.bitmapMaterial.mainTexture = sdl.bitmap;
					}
					else{
						sdl.bitmapMaterial = new Material(Shader.Find("Diffuse"));
						sdl.bitmapMaterial.mainTexture = sdl.bitmap;
					}
                }
                return sdl;
            }
            catch (Exception e)
            {
				Debug.LogError(e.Message);
                return sdl;
                //throw e;
            }
        }

#if TODO_RR		
        public static SDL_Surface CreateSurface(int w, int h, bool f)
        {
            SDL_Surface sur = new SDL_Surface();
/*
            SDL_PixelFormat* spf = SDL_GetVideoSurface()->format;
            if ((sur = SDL_CreateRGBSurface(f, w, h, spf->BitsPerPixel, spf->Rmask, spf->Gmask, spf->Bmask, spf->Amask)) == 0)
            {
                fprintf(stderr, "ERR: ssur_create: not enough memory to create surface...\n");
                exit(1);
            }
*/
            /*    if (f & SDL_HWSURFACE && !(sur->flags & SDL_HWSURFACE))
                    fprintf(stderr, "unable to create surface (%ix%ix%i) in hardware memory...\n", w, h, spf->BitsPerPixel);*/
//            SDL_SetColorKey(sur, SDL_SRCCOLORKEY, 0x0);
//           SDL_SetAlpha(sur, 0, 0); /* no alpha */

            sur.Bitmap = new System.Drawing.Bitmap(w, h);
            sur.name ="Creado vacio";

            return sur;
        }
#endif
#if TODO_RR
        public static void full_copy_image(SDL_Surface dest, SDL_Surface src, int xpos, int ypos)
        {
            Graphics graphic = Graphics.FromImage(dest.bitmap);
            graphic.DrawImage(src.bitmap, 0, 0, new Rectangle(xpos, ypos, dest.w, dest.h), GraphicsUnit.Pixel);
        }

        //TODO_RR static ImageAttributes imageAttr = new ImageAttributes();
#endif
#if TODO_RR
        public static void set_alpha(int alpha)
        {
            // Initialize the color matrix.
            ColorMatrix myColorMatrix = new ColorMatrix();

            // Red
            myColorMatrix.Matrix00 = 1.00f;
            // Green
            myColorMatrix.Matrix11 = 1.00f;
            // Blue
            myColorMatrix.Matrix22 = 1.00f;
            // alpha
            myColorMatrix.Matrix33 = (float)(alpha) / 256.0f; ;
            // w
            myColorMatrix.Matrix44 = 1.00f;

            // set the color matrix.
            imageAttr.SetColorMatrix(myColorMatrix);
        }
#endif
#if TODO_RR
        public static void copy_image(SDL_Surface dest, int xdest, int ydest, int w, int h, SDL_Surface src, int xsrc, int ysrc)
        {
            Graphics graphic;
            if (dest.surf != null && dest.bitmap == null)
                graphic = dest.surf;
            else
                graphic = Graphics.FromImage(dest.bitmap);
            graphic.DrawImage(src.bitmap, xdest, ydest, new Rectangle(xsrc, ysrc, w, h), GraphicsUnit.Pixel);
        }
#endif
#if TODO_RR
        public static void copy_image(SDL_Surface dest, int xdest, int ydest,
                                       int w, int h, SDL_Surface src, int xsrc, int ysrc, int alpha)
        {
            Graphics graphic;
            if (dest.surf != null && dest.bitmap == null)
                graphic = dest.surf;
            else
                graphic = Graphics.FromImage(dest.bitmap);
            set_alpha(alpha);
            graphic.DrawImage(src.bitmap, new Rectangle(xdest, ydest, w, h), xsrc, ysrc, w, h, GraphicsUnit.Pixel, imageAttr);
        }
#endif

#if TODO_RR
        public static void copy_image(Graphics graphic, int xdest, int ydest, int w, int h, SDL_Surface src, int xsrc, int ysrc)
        {
            graphic.DrawImage(src.bitmap, xdest, ydest, new Rectangle(xsrc, ysrc, w, h), GraphicsUnit.Pixel);
        }
#endif
#if TODO_RR
        public static void copy_image180(SDL_Surface dest, int xdest, int ydest, int w, int h, SDL_Surface src, int xsrc, int ysrc)
        {
            Graphics graphic;
            if (dest.surf != null && dest.bitmap == null)
                graphic = dest.surf;
            else
                graphic = Graphics.FromImage(dest.bitmap);
            Image img = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(img);
            g.DrawImage(src.bitmap, 0, 0, new Rectangle(xsrc, ysrc, w, h), GraphicsUnit.Pixel);

            img.RotateFlip(RotateFlipType.RotateNoneFlipX);
            graphic.DrawImage(img, xdest, ydest, new Rectangle(0, 0, w, h), GraphicsUnit.Pixel);
        }
#endif
#if TODO_RR
        public static void SDL_SetColorKey(SDL_Surface dest, int color_key)
        {
            dest.bitmap.MakeTransparent(Color.FromArgb(color_key));
        }
#endif
    }

    public class Anim
    {
        public static Anim anim_create(SDL_Surface surf)
        {
            return new Anim();
        }
    }
}

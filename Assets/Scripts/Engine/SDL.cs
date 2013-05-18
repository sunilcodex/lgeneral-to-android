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
using System.Collections.Generic;

//TODO_RR 	using System.Drawing;
//TODO_RR using System.Drawing.Drawing2D;
//TODO_RR using System.Drawing.Imaging;

namespace EngineApp
{
    /// <summary>
    /// a dummy SDL_Surface.
    /// </summary>
    public class SDL_Surface
    {
        public int w, h;
        private Texture2D bitmap;
        //TODO_RR public System.Drawing.Graphics surf;
        public string name;
		private Material bitmapMaterial;
		public Material BitmapMaterial{
			get{return bitmapMaterial;}
			set{bitmapMaterial = value;}
		}

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

        public static Color GetPixel(SDL_Surface surf, int x, int y)
        {
            return surf.bitmap.GetPixel(x, y);
        }
		
		public static SDL_Surface Resize(SDL_Surface surf,float factor){
			SDL_Surface dest = new SDL_Surface();
			dest.name = surf.name;
			dest.w =(int) (surf.w*factor);
			dest.h =(int) (surf.h*factor);
			dest.Bitmap = new Texture2D (dest.w, dest.h, TextureFormat.RGBA32, false);
			float scale=(1/factor);
			for (int i=0; i<dest.w; i++) {
				for (int j=0; j<dest.h; j++) {
					int x = (int)(scale * i);
					int y = (int)(scale * j);
					dest.Bitmap.SetPixel (i, j, surf.Bitmap.GetPixel (x, y));
				}
			}
			dest.Bitmap.Apply ();
			dest.BitmapMaterial = new Material (Shader.Find ("Diffuse"));
			dest.BitmapMaterial.mainTexture = dest.Bitmap;
			return dest;
			
		}
		
        /*
        load a surface from file putting it in soft or hardware mem
        */
        public static SDL_Surface LoadSurface(string fname, bool applyTransparency)
        {
			SDL_Surface sdl = new SDL_Surface();
            try
            {
				
                if (fname != null)
                {
					sdl.bitmap = Resources.Load(fname) as Texture2D;
					sdl.w = sdl.bitmap.width;
					sdl.h = sdl.bitmap.height;
                    sdl.name = fname;
					sdl.w = sdl.bitmap.width;
					sdl.h = sdl.bitmap.height;
#if TODO_RR
					if (applyTransparency){
						Color[] c = sdl.bitmap.GetPixels();
						for (int i=0; i<c.Length; i++){
							if (c[i]==Color.black){
								c[i].b = c[i].b;
								c[i].r = c[i].r;
								c[i].g = c[i].g;
								c[i].a = 0;
								
							}
							else{
								c[i].b = c[i].b;
								c[i].r = c[i].r;
								c[i].g = c[i].g;
								c[i].a = 1;
							}
						}
						sdl.bitmap.SetPixels(c);
						sdl.bitmap.Apply();
						sdl.bitmapMaterial = new Material(Shader.Find("Transparent/Cutout/Diffuse"));
						sdl.bitmapMaterial.mainTexture = sdl.bitmap;
					}
					else{
						sdl.bitmapMaterial = new Material(Shader.Find("Diffuse"));
						sdl.bitmapMaterial.mainTexture = sdl.bitmap;
					}
#endif
					sdl.bitmapMaterial = new Material(Shader.Find("Diffuse"));
					sdl.bitmapMaterial.mainTexture = sdl.bitmap;
					
                }
                return sdl;
            }
            catch (Exception e)
            {
				Debug.LogError("error trying to load and create a Texture2D " + fname);
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
		
        public static void full_copy_image(SDL_Surface dest, SDL_Surface src)
        {
			if (src==null || dest==null)
				throw new Exception("the source or destination image is null");
			try{
				Texture2D nueva = new Texture2D(src.w,src.h,TextureFormat.RGB24,false);
				for (int i=0;i<src.w;i++){
					for (int j=0; j<src.h;j++){
						Color c = src.bitmap.GetPixel(i,j);
						nueva.SetPixel(i,j,c);
					}
				}
				nueva.Apply();
				dest = new SDL_Surface();
				dest.Bitmap = nueva;
				dest.BitmapMaterial = new Material(Shader.Find("Diffuse"));
				dest.BitmapMaterial.mainTexture = nueva;
				dest.w = src.w;
				dest.h = src.h;	
				
			}
			catch(Exception e){
				Debug.LogError("Problems with the copy of image: " + e.Message);
			}
            	
						
			
        }

        //TODO_RR static ImageAttributes imageAttr = new ImageAttributes();
		
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

        public static void copy_image(SDL_Surface dest, int w, int h, SDL_Surface src, int xsrc, int ysrc)
        {
			if (src==null || dest==null)
				throw new Exception("the source or destination image is null");
			try{
				Color[] pixels = src.bitmap.GetPixels(xsrc,ysrc,w,h);
				Texture2D tex = new Texture2D (w, h,TextureFormat.RGB24,false);
				tex.SetPixels (pixels);
				tex.Apply ();
				dest.Bitmap = tex;
				if (dest.BitmapMaterial!=null){
					dest.BitmapMaterial.mainTexture = tex;
				}
				else{
					dest.BitmapMaterial = new Material(Shader.Find("Diffuse"));
					dest.BitmapMaterial.mainTexture = tex;	
				}
				dest.w = w;
				dest.h = h;
			}
			catch(Exception e){
				Debug.LogError("Problems with the copy of image: " + e.Message);
			}
            
        }

        public static void copy_image180(SDL_Surface dest, SDL_Surface src)
        {
			if (src==null || dest==null)
				throw new Exception("the source or destination image is null");
			try{
				Texture2D nueva = new Texture2D(src.w,src.h,TextureFormat.RGB24,false);
				for (int i=0;i<src.w;i++){
					for (int j=0; j<src.h;j++){
						Color c = src.bitmap.GetPixel(i,j);
						nueva.SetPixel(src.w-i-1,j,c);
					}
				}
				nueva.Apply();
				dest.Bitmap = nueva;
				if (dest.BitmapMaterial!=null){
					dest.BitmapMaterial.mainTexture = nueva;
				}
				else{
					dest.BitmapMaterial = new Material(Shader.Find("Diffuse"));
					dest.BitmapMaterial.mainTexture = nueva;	
				}
				dest.w = src.w;
				dest.h = src.h;
				dest.name = src.name;
			}
			catch (Exception e){
				Debug.LogError("Problems with the rotate of image: " + e.Message);
			}
            
        }
		
		public static void copy_image(SDL_Surface dest, SDL_Surface src,int xdest, 
			int ydest,int wsrc, int hsrc, int xsrc, int ysrc){
			try{
				if (src==null || dest==null)
					throw new Exception("the source or destination image is null");
				Color[] c = src.Bitmap.GetPixels(xsrc,ysrc,wsrc,hsrc);
				dest.Bitmap.SetPixels(xdest,ydest,wsrc, hsrc,c);
				dest.Bitmap.Apply();
				if (dest.BitmapMaterial!=null){
					dest.BitmapMaterial.mainTexture = dest.Bitmap;
				}
				else{
					dest.BitmapMaterial = new Material(Shader.Find("Diffuse"));
					dest.BitmapMaterial.mainTexture = dest.Bitmap;	
				}
			}
			catch(Exception e){
				Debug.LogError("Problems with the copy: "+ e.Message);
			}
		}
		
		public static void copy_image_without_key(SDL_Surface dest,SDL_Surface src,int xdest, 
												  int ydest,Color key){
			try{
				for (int i=0; i<src.w;i++){
					for (int j=0; j<src.h;j++){
						Color c = src.Bitmap.GetPixel(i,j);
						if (c!=key){
							dest.Bitmap.SetPixel(xdest+i,ydest+j,c);
						}
					}
				}
				dest.Bitmap.Apply();
				if (dest.BitmapMaterial!=null){
					dest.BitmapMaterial.mainTexture = dest.Bitmap;
				}
				else{
					dest.BitmapMaterial = new Material(Shader.Find("Diffuse"));
					dest.BitmapMaterial.mainTexture = dest.Bitmap;	
				}
			}
			catch(Exception e){
				Debug.LogError(e.Message);
			}
			
		}
		
		public static void putPixelBlack(SDL_Surface surf){
			try{
				surf.Bitmap.SetPixel(0,0,Color.black);
				surf.Bitmap.SetPixel(surf.Bitmap.width,surf.Bitmap.height-1,Color.black);
				surf.Bitmap.SetPixel(surf.Bitmap.width,surf.Bitmap.height-2,Color.black);
				surf.Bitmap.SetPixel(surf.Bitmap.width-1,surf.Bitmap.height-1,Color.black);
				surf.Bitmap.SetPixel(surf.Bitmap.width-1,surf.Bitmap.height-2,Color.black);
				surf.Bitmap.SetPixel(0,surf.Bitmap.height-1,Color.black);
				surf.Bitmap.Apply();
			}
			catch(Exception e){
				Debug.LogError(e.Message);
			}
			

		}


        public static void SDL_SetColorKey(SDL_Surface dest, Color color_key)
        {
			try{
				Color[] c = dest.bitmap.GetPixels();
				for (int i=0; i<c.Length; i++){
					if (c[i]==color_key){
						c[i].b = c[i].b;
						c[i].r = c[i].r;
						c[i].g = c[i].g;
						c[i].a = 0;
						
					}
					else{
						c[i].b = c[i].b;
						c[i].r = c[i].r;
						c[i].g = c[i].g;
						c[i].a = 1;
					}
				}
				dest.Bitmap.SetPixels(c);
				dest.Bitmap.Apply();
				dest.BitmapMaterial=new Material(Shader.Find("Transparent/Cutout/Diffuse"));
				dest.BitmapMaterial.mainTexture = dest.Bitmap;
			}
            catch(Exception e){
				Debug.LogError(e.Message);
			}
        }

    }

    public class Anim
    {
        public static Anim anim_create(SDL_Surface surf)
        {
            return new Anim();
        }
    }
}

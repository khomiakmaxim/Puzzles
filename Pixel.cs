using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzlesProj
{
    // TODO: use <summary>
    //даний клас відповідає за кожний окремий піксель зображення
    public class Pixel
    {
        private int red;
        private int green;
        private int blue;

        public int Red { get { return red; } set { red = value; } }
        public int Green { get { return green; } set { green = value; } }
        public int Blue { get { return blue; } set { blue = value; } }        

        // TODO: remove useless constructor?
        public Pixel()
        {
        }

        public Pixel(int red = 0, int green = 0, int blue = 0)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }

        public  int gray_scale()
        {
            return (int) (0.3 * red + 0.59 * green + 0.11 * blue);
        }

    // TODO: fix formatting
    public override string ToString()
        {
            return gray_scale() + "\n";
        }
    }
}

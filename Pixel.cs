using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzlesProj
{
    class Pixel
    {
        public int red;
        public int green;
        public int blue;

        public double GrayScale()
        {
            return (0.3 * red) + (0.59 * green) + (0.11 * blue);
        }

        public Pixel(int red = 0, int green = 0, int blue = 0)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }
    }
}

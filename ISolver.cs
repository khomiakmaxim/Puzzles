using System.Collections.Generic;

namespace PuzzlesProj
{
    interface ISolver
    {
        List<int> GeneratePerm(List<List<List<Pixel>>> chunks);        
    }
}

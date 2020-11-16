using System.Collections.Generic;

namespace PuzzlesProj
{
    interface ISolver
    {
        // TODO: include solve method?
        // TODO: Do not shorten name (perm can stand for permanent, permission etc.)
        List<int> GeneratePerm(List<List<List<Pixel>>> chunks);        
    }
}

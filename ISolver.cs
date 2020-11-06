using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzlesProj
{
    interface ISolver
    {
        long totalCost(List<int> perm, List<List<int>> LR, List<List<int>> UD);
        List<int> Solve(List<List<int>> LR, List<List<int>> UD, double coeff, int start_chunk);        
    }
}

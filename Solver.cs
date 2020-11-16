using System;
using System.Collections.Generic;
using System.Linq;

namespace PuzzlesProj
{
    class Solver : ISolver
    {
        private int rows;
        private int columns;
        private int m;
        private int Height;
        private int Width;

        public Solver(int rows, int columns, int Width, int Height)
        {
            this.rows = rows;
            this.columns = columns;
            this.m = rows * columns;
            this.Width = Width;
            this.Height = Height;
        }

        private int LRCheck(List<List<Pixel>> l, List<List<Pixel>> r)
        {
            int n = Height;
            int res = 0;

            for (int i = 0; i < n; ++i)
            {
                //res += Math.Abs(l[i][Width - 1].gray_scale() - r[i][0].gray_scale());
                res += Math.Abs(l[i][Width - 1].Red - r[i][0].Red);
                res += Math.Abs(l[i][Width - 1].Green - r[i][0].Green);
                res += Math.Abs(l[i][Width - 1].Blue - r[i][0].Blue);
            }
            return res;
        }

        private int UDCheck(List<List<Pixel>> u, List<List<Pixel>> d)
        {
            int n = Width;
            int res = 0;

            for (int i = 0; i < n; ++i)
            {
                //res += Math.Abs(u[Height - 1][i].gray_scale() - d[0][i].gray_scale());
                res += Math.Abs(u[Height - 1][i].Red - d[0][i].Red);
                res += Math.Abs(u[Height - 1][i].Green - d[0][i].Green);
                res += Math.Abs(u[Height - 1][i].Blue - d[0][i].Blue);
            }

            return res;
        }

        private Tuple<List<List<int>>, List<List<int>>> Precalc(List<List<List<Pixel>>> chunks)
        {
            int m = chunks.Count;

            List<List<int>> UD = new List<List<int>>(m);
            List<List<int>> LR = new List<List<int>>(m);

            for (int i = 0; i < m; ++i)
            {
                UD.Add(new List<int>(m));
                LR.Add(new List<int>(m));
            }

            for (int i = 0; i < m; ++i)
            {
                for (int j = 0; j < m; ++j)
                {
                    if (i != j)
                    {
                        UD[i].Add(UDCheck(chunks[i], chunks[j]));
                        LR[i].Add(LRCheck(chunks[i], chunks[j]));
                    }
                    else
                    {
                        UD[i].Add(0);
                        LR[i].Add(0);
                    }
                }
            }

            return new Tuple<List<List<int>>, List<List<int>>>(LR, UD);
        }

        private long totalCost(List<int> perm, List<List<int>> LR, List<List<int>> UD)
        {
            long res = 0;

            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < columns; ++j)
                {
                    int c1 = perm[i * columns + j];
                    if (j + 1 < columns)
                        res += LR[c1][perm[(i * columns) + j + 1]];
                    if (i + 1 < rows)
                        res += UD[c1][perm[(i + 1) * columns + j]];
                }
            }

            return res;
        }

        private Tuple<long, List<int>> Min(Tuple<long, List<int>> a, Tuple<long, List<int>> b)
        {
            return a.Item1 < b.Item1 ? a : b;
        }

        private Tuple<double, double, double> Max(Tuple<double, double, double> a, Tuple<double, double, double> b)
        {
            if (a.Item1 > b.Item1)
                return a;
            else if (a.Item1 < b.Item1)
                return b;
            else
            {
                if (a.Item2 > b.Item2)
                    return a;
                else if (a.Item2 < b.Item2)
                    return b;
                else
                {
                    if (a.Item3 >= b.Item3)
                        return a;
                    else
                        return b;
                }
            }
        }

        // TODO: add link to algorithm used for reference
        private List<int> Solve(List<List<int>> LR, List<List<int>> UD, double coeff, int start_chunk)
        {            
            int mnI = rows - 1;
            int mxI = rows - 1;
            int mnJ = columns - 1;
            int mxJ = columns - 1;

            List<List<int>> ans = new List<List<int>>(rows * 2);
            for (int i = 0; i < rows * 2; ++i)
            {
                ans.Add(new List<int>(columns * 2));
                for (int j = 0; j < columns * 2; ++j)
                    ans[i].Add(-1);
            }

            List<bool> used = new List<bool>(m);

            for (int i = 0; i < m; ++i)
                used.Add(false);


            used[start_chunk] = true;
            ans[rows - 1][columns - 1] = start_chunk;

            // TODO: use type aliases or even create simple data class
            //усі можливі сусіди першого чанка(верхній, правий, ...)
            List<Tuple<int, int>> neighbours = new List<Tuple<int, int>>();
            List<Tuple<int, int, char>> MT = new List<Tuple<int, int, char>>
            {
                new Tuple<int, int, char>(1, 0, 'D'),//той, що знизу від центрального
                new Tuple<int, int, char>(0, 1, 'R'),//...
                new Tuple<int, int, char>(-1, 0, 'U'),
                new Tuple<int, int, char>(0, -1, 'L'),
            };

            foreach (var i in MT)
            {
                neighbours.Add(new Tuple<int, int>(rows - 1 + i.Item1, columns - 1 + i.Item2));
            }
            int progress = 1;
            while (progress < m)
            {
                //даний список тримає в набір всіх можливих вставок для всіх кандидатів(сусідів)
                List<SortedSet<Tuple<double, int>>> maksym = new List<SortedSet<Tuple<double, int>>>();
                List<Tuple<int, int>> good_neighbours = new List<Tuple<int, int>>();
                foreach (var i in neighbours)
                {
                    //розглядатимуться лише ті випадки, коли пазл не порушує констрейнт розміру картинки
                    if ((i.Item1 - mnI > rows - 1) || (mxI - i.Item1 > rows - 1) || (i.Item2 - mnJ > columns - 1) || (mxJ - i.Item2 > columns - 1))
                        continue;
                    good_neighbours.Add(i);
                }
                neighbours = good_neighbours;
                double mnCost = 1e15;
                int bestChunk = m + 1;
                //розглядаються лише доступні варіанти для вставки                
                foreach (var i in neighbours)
                {
                    maksym.Add(new SortedSet<Tuple<double, int>>());
                    //для кожного місця перебираються всі чанки, які можна туди поставити
                    for (int ch = 0; ch < m; ++ch)
                    {
                        if (used[ch])
                            continue;
                        long sm = 0;
                        int cnt = 0;

                        foreach (var j in MT)
                        {
                            int ni = i.Item1 + j.Item1;
                            int nj = i.Item2 + j.Item2;
                            if ((ni < 0) || (nj < 0) || (ni >= rows * 2) || (nj >= columns * 2))
                                continue;
                            if (ans[ni][nj] != -1)//якщо сусід сусіда вже має позицію
                            {
                                long score = 0;
                                int nc = ans[ni][nj];
                                if (j.Item3 == 'D')
                                    score = UD[ch][nc];
                                if (j.Item3 == 'R')
                                    score = LR[ch][nc];
                                if (j.Item3 == 'U')
                                    score = UD[nc][ch];
                                if (j.Item3 == 'L')
                                    score = LR[nc][ch];
                                ++cnt;
                                sm += score;
                            }
                        }
                        maksym.Last().Add(new Tuple<double, int>((double)sm / (double)cnt, ch));
                    }
                    mnCost = Math.Min(mnCost, maksym.Last().First().Item1);
                }
                double mid = mnCost * coeff;
                Tuple<double, double, double> best = new Tuple<double, double, double>(0, 0, 0);
                for (int x = 0; x < neighbours.Count; ++x)
                {
                    var a = maksym[x];
                    if (a.Count == 1)
                    {
                        double cost = a.First().Item1;
                        int ch = a.First().Item2;

                        best = new Tuple<double, double, double>(cost, ch, x);
                    }
                    else
                    {
                        double d = a.ElementAt(1).Item1 - a.First().Item1;
                        double cost = a.First().Item1;
                        double ch = a.First().Item2;
                        if (cost <= mid)
                        {
                            best = Max(best, new Tuple<double, double, double>(d, ch, x));
                        }
                    }
                }
                var z = neighbours[(int)best.Item3];
                int I = z.Item1;
                int J = z.Item2;
                int bch = (int)best.Item2;

                mnI = Math.Min(mnI, I);
                mxI = Math.Max(mxI, I);
                mnJ = Math.Min(mnJ, J);
                mxJ = Math.Max(mxJ, J);

                ans[I][J] = bch;
                if (used[bch]) throw new Exception();
                used[bch] = true;
                ++progress;

                neighbours.Remove(neighbours.Find(c => c.Item1 == I && c.Item2 == J));


                foreach (var k in MT)
                {
                    int ni = I + k.Item1;
                    int nj = J + k.Item2;
                    if ((ni - mnI + 1 > rows) || (mxI - ni + 1 > rows) || (nj - mnJ + 1 > columns) || (mxJ - nj + 1 > columns))
                        continue;
                    if (ans[ni][nj] == -1 && neighbours.Where(c => c.Item1 == ni && c.Item2 == nj).Count() == 0)
                    {
                        //добавлення нового кандидата до списку
                        neighbours.Add(new Tuple<int, int>(ni, nj));
                    }
                }
            }

            //відповідна перестановка
            List<int> perm = new List<int>(m);
            for (int i = 0; i < rows * 2; ++i)
            {
                for (int j = 0; j < columns * 2; ++j)
                {
                    if (ans[i][j] != -1)
                    {
                        perm.Add(ans[i][j]);
                    }
                }
            }


            return perm;//вихідна готова перестановка
        }

        public List<int> GeneratePerm(List<List<List<Pixel>>> chunks)
        {
            List<int> permResult;

            var tpl = Precalc(chunks);
            const long LINF = (long)1e18 + 47;
            Tuple<long, List<int>> best = new Tuple<long, List<int>>(LINF, Solve(tpl.Item1, tpl.Item2, 1, 0));
            for (int i = 1; i < chunks.Count; ++i)
            {
                Random rnd = new Random();
                int coeff = rnd.Next(15, 35);
                permResult = Solve(tpl.Item1, tpl.Item2, (double)coeff / 10, i);
                best = Min(best, new Tuple<long, List<int>>(totalCost(permResult, tpl.Item1, tpl.Item2), permResult));
            }

            return best.Item2;
        }
    }
}

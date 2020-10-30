using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using static PuzzlesProj.Piece;

namespace PuzzlesProj
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int IINF = 2 << 31 - 47;
        //шматок пазла, який на даний момент в руці
        Piece currentSelection;
        int selectionAngle = 0;
        List<Piece> pieces = new List<Piece>();
        List<Piece> shadowPieces = new List<Piece>();
        int columns;//кілкість колонок, на які все розбивається
        int rows;//кількість рядків
        new int Width;
        new int Height;
        double scale = 1.0;//коефіцієнт масштабування
        BitmapImage imageSource;
        string srcFileName = "";
        DropShadowBitmapEffect shadowEffect;//даний об'єкт дозволяє працювати з тінню
        System.Windows.Point lastCell = new System.Windows.Point(-1, 0);
        ScaleTransform stZoomed = new ScaleTransform//масштабування при виборі конкретного пазлу
        { ScaleX = 1.1,
            ScaleY = 1.1
        };

        //матриця пікселів для алгоритму
        List<List<List<Pixel>>> chunks = new List<List<List<Pixel>>>();
        //перестановка, згенерована алгоритмом
        List<int> permResult = new List<int>();

        PngBitmapEncoder png;
        double initialRectangleX = 0;
        double initialRectangleY = 0;
        System.Windows.Shapes.Rectangle rectSelection = new System.Windows.Shapes.Rectangle();
        public MainWindow()
        {
            InitializeComponent();

            cnvPuzzle.MouseLeftButtonUp += new MouseButtonEventHandler(cnvPuzzle_MouseLeftButtonUp);
            cnvPuzzle.MouseDown += new MouseButtonEventHandler(cnvPuzzle_MouseDown);
            cnvPuzzle.MouseMove += new MouseEventHandler(cnvPuzzle_MouseMove);
            cnvPuzzle.MouseEnter += new MouseEventHandler(cnvPuzzle_MouseEnter);
            cnvPuzzle.MouseLeave += new MouseEventHandler(cnvPuzzle_MouseLeave);

            shadowEffect = new DropShadowBitmapEffect()
            {
                Color = Colors.Black,
                Direction = 310,//кут, по якому тінь проектується
                ShadowDepth = 25,//відстань між пазлом і його тінню
                Softness = 1,
                Opacity = 0.5
            };
        }


        #region old algorithm

        long totalCost(List<int> perm, List<List<int>> LR, List<List<int>> UD)
        {
            long res = 0;

            for (int i = 0; i < perm.Count - 1; ++i)
            {
                if (i % columns - 1 != 0)
                    res += LR[i][i + 1];
            }

            for (int i = 0; i < perm.Count - 1; ++i)
            {
                if (i < perm.Count - 1 - columns)
                    res += UD[i][i + columns];
            }


            return res;
        }

        private int LRCheck(List<List<Pixel>> l, List<List<Pixel>> r)
        {
            int n = Height;
            int res = 0;

            for (int i = 0; i < n; ++i)
            {
                res += Math.Abs(l[i][Width - 1].gray_scale() - r[i][0].gray_scale());
                //або через rgb
            }
            return res;
        }

        private int UDCheck(List<List<Pixel>> u, List<List<Pixel>> d)
        {
            int n = Width;
            int res = 0;

            for (int i = 0; i < n; ++i)
            {
                res += Math.Abs(u[Height - 1][i].gray_scale() - d[0][i].gray_scale());
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

            return new Tuple<List<List<int>>, List<List<int>>>(UD, LR);
        }

        public Tuple<long, List<int>> Min(Tuple<long, List<int>> a, Tuple<long, List<int>> b)
        {
            return a.Item1 < b.Item1 ? a : b;
        }

        public Tuple<double, double, double> Max(Tuple<double, double, double> a, Tuple<double, double, double> b)
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


        private List<int> Solve(List<List<int>> LR, List<List<int>> UD, double coeff, int start_chunk = 0)
        {
            int m = chunks.Count;

            int mnI = rows - 1, mxI = rows - 1;//допустимі границі
            int mnJ = columns - 1, mxJ = columns - 1;

            List<List<int>> ans = new List<List<int>>(rows * 2);//прямокутний пазл, розміри якого вдвічі більші за той, що потрібно зібрати
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
                List<SortedSet<Tuple<double, int>>> maksym = new List<SortedSet<Tuple<double, int>>>();//даний список тримє в набір всіх можливих вставок для всіх кандидатів
                List<Tuple<int, int>> good_neighbours = new List<Tuple<int, int>>();
                foreach (var i in neighbours)
                {
                    //розглядатимуться лише ті випадки, коли пазл не порушує констрейнт розміру картинки
                    if ((i.Item1 - mnI + 1 > rows) || (mxI - i.Item1 + 1 > rows) || (i.Item2 - mnJ + 1 > columns) || (mxJ - i.Item2 + 1 > columns))
                        continue;
                    good_neighbours.Add(i);
                }
                neighbours = good_neighbours;
                double mnCost = 1e15;
                //розглядаються лише доступні конкуренти для вставки                
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
                            if (ni < 0 || nj < 0 || ni >= rows * 2 || nj >= columns * 2)
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
                        maksym.Last().Add(new Tuple<double, int>((double)sm / cnt, ch));
                    }
                    //значення мінімальної вартості вставки оновлюється при потребі
                    mnCost = Math.Min(mnCost, maksym.Last().First().Item1);
                }
                double mid = mnCost * coeff;//оптимізаційний момент
                Tuple<double, double, double> best = new Tuple<double, double, double>(0, 0, 0);
                Tuple<double, double, double> best2 = new Tuple<double, double, double>(0, 0, 0);
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
                        if (cost <= mid)//на деяких, не лише на найкращому спрацює
                        {
                            best = Max(best, new Tuple<double, double, double>(d, ch, x));
                        }
                        best2 = Max(best2, new Tuple<double, double, double>(d, ch, x));
                    }
                }
                if (best == new Tuple<double, double, double>(0, 0, 0))
                    best = best2;
                //best - жадібно найкращий варіант для вставки, x - індекс серед сусідів
                //ch - індекс чанку
                var z = neighbours[(int)best.Item3];
                int I = z.Item1;
                int J = z.Item2;
                int bch = (int)best.Item2;

                //допустимі границі мають бути перерахованими
                mnI = Math.Min(mnI, I);
                mxI = Math.Max(mxI, I);
                mnJ = Math.Min(mnJ, J);
                mxJ = Math.Max(mxJ, J);

                ans[I][J] = bch;
                used[bch] = true;
                ++progress;

                //видалення вставленого кандидата з списку
                neighbours.Remove(neighbours.Find(c => c == new Tuple<int, int>(I, J)));

                foreach (var k in MT)
                {
                    int ni = I + k.Item1;
                    int nj = J + k.Item2;
                    if (ni - mnI + 1 > rows || mxI - ni + 1 > rows || nj - mnJ + 1 > columns || mxJ - nj + 1 > columns)
                        continue;
                    if (ans[ni][nj] == -1)
                    {
                        //добавлення нового кандидата до списку
                        neighbours.Add(new Tuple<int, int>(ni, nj));
                    }
                }
            }

            //відповідна перестановка
            List<int> perm = new List<int>(m);
            for (int i = 0; i < columns * 2; ++i)
            {
                for (int j = 0; j < rows * 2; ++j)
                {
                    if (ans[j][i] != -1)
                    {
                        perm.Add(ans[j][i]);
                    }
                }
            }

            List<int> fullPerm = perm.OrderBy(i => i).ToList();

            List<int> actual = new List<int>(m);
            for (int x = 0; x < m; ++x) actual.Add(x);

            for (int x = 0; x < m; ++x)
            {
                if (x >= fullPerm.Count || fullPerm[x] != actual[x])
                {
                    fullPerm.Insert(x, actual[x]);
                    perm.Add(actual[x]);
                }
            }

            return perm;//вихідна готова перестановка
        }

        #endregion

        #region new algorithm

        private int DWT(List<Pixel> a, List<Pixel> b)
        {
            int size = a.Count;
            int w = (int)(size * 0.1);//10% від всієї довжини
            int[][] result = new int[size][];
            for (int i = 0; i < size; ++i) result[i] = new int[size];

            for (int n = 0; n < size; ++n)
            {
                for (int m = 0; m < size; ++m)
                {
                    if (Math.Max(0, n - w) <= m && Math.Min(size - 1, n + w - 2) >= m)
                    {
                        result[n][m] = 0;
                    }
                    else
                    {
                        result[n][m] = IINF;
                    }
                }
            }

            for (int i = 0; i < size; ++i)
            {
                if (result[i][0] == 0)
                {
                    result[i][0] = Math.Abs(a[i].gray_scale() - b[0].gray_scale());
                }

                if (result[0][i] == 0)
                {
                    result[0][i] = Math.Abs(a[0].gray_scale() - b[i].gray_scale());
                }
            }

            for (int n = 0; n < size; ++n)
            {
                for (int m = 0; m < size; ++m)
                {
                    if (result[n][m] == 0)
                    {
                        result[n][m] = Math.Abs(a[n].gray_scale() - b[m].gray_scale()) +
                            Math.Min(Math.Min(result[n - 1][m], result[n - 1][m - 1]), result[n][m - 1]);
                    }
                }
            }

            return result[size - 1][size - 1];
        }

        public int UDDCheck(List<List<Pixel>> a, List<List<Pixel>> b)
        {
            int res = 0;

            for (int i = 0; i < Width; ++i)
            {
                res += DWT(a[Height - 1], b[0]);
            }

            return res;
        }

        public int LRDCheck(List<List<Pixel>> a, List<List<Pixel>> b)
        {
            int res = 0;

            List<Pixel> left = new List<Pixel>();
            List<Pixel> right = new List<Pixel>();

            for (int i = 0; i < Height; ++i)
            {
                left.Add(b[i][0]);
            }

            for (int i = 0; i < Height; ++i)
            {
                right.Add(a[i][Width - 1]);
            }

            for (int i = 0; i < Height; ++i)
            {
                res += DWT(left, right);
            }

            return res;
        }

        private Tuple<List<List<int>>, List<List<int>>> Precalc2(List<List<List<Pixel>>> chunks)
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
                        UD[i].Add(UDDCheck(chunks[i], chunks[j]));
                        LR[i].Add(LRDCheck(chunks[i], chunks[j]));
                    }
                    else
                    {
                        UD[i].Add(IINF);
                        LR[i].Add(IINF);
                    }
                }
            }

            return new Tuple<List<List<int>>, List<List<int>>>(UD, LR);
        }

        //public List<int> HungarianSolve(List<List<int>> UD, List<List<int>> LR)
        //{
        //    int c = 0;
        //    for(int k = 0; k < )
        //}


        #endregion

        private void CreatePuzzle(Stream streamSource)
        {
            zoomSlider.Value = 0.50;                                             
            png = null;            
            
            imageSource = null;                        
            using (BinaryReader reader = new BinaryReader(streamSource))
            {
                imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.StreamSource = reader.BaseStream;
                imageSource.EndInit();
                imageSource.Freeze();
            }

            //List<Pixel> - ряд пікселів
            //List<List<Pixel>> - ряд рядів
            chunks = new List<List<List<Pixel>>>();
            Bitmap alg = BitmapImage2Bitmap(imageSource);

            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < columns; ++j)
                {
                    List<List<Pixel>> ch = new List<List<Pixel>>(Height);

                    for (int x = 0; x < Height; ++x)
                    {
                        ch.Add(new List<Pixel>(Width));                        
                    }

                    for (int x = 0; x < Height; ++x)
                    {
                        for (int y = 0; y < Width; ++y)
                        {
                            System.Drawing.Color clr = alg.GetPixel(j * Width +  y, i * Height + x);
                            ch[x].Add(new Pixel(clr.R, clr.G, clr.B));
                        }
                    }

                    chunks.Add(ch);
                }
            }                        

            imgShowImage.Source = imageSource;

            scvImage.Visibility = Visibility.Hidden;
            cnvPuzzle.Visibility = Visibility.Visible;

            var angles = new int[] { 0, 90, 180, 270 };            

            int index = 0;//присвоєння індекса кожному пазлу
            for (var y = 0; y < rows; ++y)
            {
                for (var x = 0; x < columns; ++x)
                {                                       
                    int angle = 0;                   

                    var piece = new Piece(imageSource, x, y, rows, columns, false, index, scale);
                    piece.SetValue(Canvas.ZIndexProperty, 1000 + x * rows + y);
                    //додаємо пару хендлерів
                    piece.MouseLeftButtonUp += new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                    piece.MouseRightButtonUp += new MouseButtonEventHandler(piece_MouseRightButtonUp);                        
                    piece.Rotate(angle);

                    //відповідна тінь
                    var shadowPiece = new Piece(imageSource, x, y, rows, columns, false, index, scale);
                    shadowPiece.SetValue(Canvas.ZIndexProperty, x * rows + y);//менше на 1000 від того, що її кидає
                    shadowPiece.Rotate(angle);
                        
                    pieces.Add(piece);
                    shadowPieces.Add(shadowPiece);
                    index++;               
                }
            }

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            var tpl = Precalc(chunks);
            const long LINF = (long)1e18 + 47;
            Tuple<long, List<int>> best = new Tuple<long, List<int>>(LINF, Solve(tpl.Item1, tpl.Item2, 1, 0));
            for (int i = 0; i < 1; ++i)
            {
                Random rnd = new Random();                
                int coeff = rnd.Next(10, 15);
                int chunk = rnd.Next(chunks.Count - 1);
                permResult = Solve(tpl.Item1, tpl.Item2, 1, chunk);
                best = Min(best, new Tuple<long, List<int>>(totalCost(permResult, tpl.Item1, tpl.Item2), permResult));
            }

            permResult = best.Item2;

            Mouse.OverrideCursor = null;


            var shuffledPieces = pieces.OrderBy(i => Guid.NewGuid()).ToList();
            int it = 0;            
            //заповнення панелі вибору
            foreach (var p in shuffledPieces)
            {
                Random random = new Random();

                p.ScaleTransform.ScaleX = 1.0;
                p.ScaleTransform.ScaleY = 1.0;                
                p.X = -1;
                p.Y = -1;
                p.IsSelected = false;                

                pnlPickUp.Children.Insert(it++, p);//заповнюється wrapPanel                                
            }
            pieces = shuffledPieces;
            List<int> actualPerm = new List<int>(columns * rows);
            for (int iter = 0; iter < columns * rows; ++iter)
            {
                var i = pieces.Where(p => p.Index == permResult[iter]).FirstOrDefault();
                actualPerm.Add(pieces.IndexOf(i));
            }

            permResult = actualPerm;
            
            rectSelection.SetValue(Canvas.ZIndexProperty, 5000);            
            cnvPuzzle.Children.Add(rectSelection);

            //SetAll();
        }

        private void SetAll()
        {
            int it = 0;

            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < columns; ++j)
                {
                    var p = pieces.ElementAt(permResult[it++]);
                    pnlPickUp.Children.Remove(p);
                    cnvPuzzle.Children.Add(p);
                    SetCurrentPiecePosition(p,this.Width * (j + 1), this.Height * (i + 1));

                    if (IsPuzzleCompleted())
                    {
                        allGood.Text = "Complited";
                        allGood.Foreground = System.Windows.Media.Brushes.DarkRed;
                    }
                    else
                    {
                        allGood.Text = "Not Complited";
                        allGood.Foreground = System.Windows.Media.Brushes.Black;
                    }
                }
            }
        }

        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {            
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        //скидання всіх можливих прив'язок, потірбне при створенні нового пазлу
        private void DestroyReferences()
        {
            for (var i = cnvPuzzle.Children.Count - 1; i >= 0; i--)
            {                
                if (cnvPuzzle.Children[i] is Piece)
                {
                    Piece p = (Piece)cnvPuzzle.Children[i];
                    p.MouseLeftButtonUp -= new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                    p.ClearImage();
                    cnvPuzzle.Children.Remove(p);
                }
            }

            cnvPuzzle.Children.Clear();
            SetSelectionRectangle(-1, -1, -1, -1);

            for (var i = pnlPickUp.Children.Count - 1; i >= 0; i--)
            {
                Piece p = (Piece)pnlPickUp.Children[i];
                p.ClearImage();
                p.MouseLeftButtonUp -= new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                pnlPickUp.Children.Remove(p);
            }

            pnlPickUp.Children.Clear();

            for (var i = pieces.Count - 1; i >= 0; i--)
            {
                pieces[i].ClearImage();
            }

            for (var i = shadowPieces.Count - 1; i >= 0; i--)
            {
                shadowPieces[i].ClearImage();
            }

            shadowPieces.Clear();
            pieces.Clear();
            imgShowImage.Source = null;
            imageSource = null;
        }
                
        private Stream LoadImage(string srcFileName)
        {            
            imageSource = new BitmapImage(new Uri(srcFileName));

            this.Width = (int)imageSource.Width / columns;
            this.Height = (int)imageSource.Height / rows;
            //копія попереднього
            var bi = new BitmapImage(new Uri(srcFileName));
            //кисть якою замальовується пазл
            var imgBrush = new ImageBrush(bi);
            
            imgBrush.AlignmentX = AlignmentX.Left;
            imgBrush.AlignmentY = AlignmentY.Top;
            imgBrush.Stretch = Stretch.None;
            
            //даний об'єк дає можливість перетворити ресурс Visual в bmp
            RenderTargetBitmap rtb = new RenderTargetBitmap(columns * Width, rows * Height, bi.DpiX, bi.DpiY, PixelFormats.Pbgra32);                        

            var rectImage = new System.Windows.Shapes.Rectangle();
            rectImage.Width = imageSource.PixelWidth;
            rectImage.Height = imageSource.PixelHeight;
            rectImage.HorizontalAlignment = HorizontalAlignment.Left;
            rectImage.VerticalAlignment = VerticalAlignment.Top;
            rectImage.Fill = imgBrush;
            rectImage.Arrange(new Rect(0, 0, imageSource.PixelWidth, imageSource.PixelHeight));

            //"ореол" довкола пазла
            rectImage.Margin = new Thickness(
                (columns * Width - imageSource.PixelWidth) / 2,
                (rows * Height - imageSource.PixelHeight) / 2,
                (rows * Height - imageSource.PixelHeight) / 2,
                (columns * Width - imageSource.PixelWidth) / 2);
            
            rtb.Render(rectImage);

            png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));

            Stream ret = new MemoryStream();
            png.Save(ret);

            return ret;
        }

        
        private bool IsPuzzleCompleted()
        {            
            var query = from p in pieces
                        where p.Angle != 0
                        select p;            
            
            if (query.Any())
                return false;                           

            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - 1
                    where (p1.Index % columns < columns - 1) && (p1.X + 1 != p2.X)
                    select p1;
            
            if (query.Any())
                return false;
            
            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - columns
                    where (p1.Y + 1 != p2.Y) || (p1.X != p2.X)
                    select p1;

            if (query.Any())
                return false;

            return true;            
        }
        
        private void ResetZIndexes()
        {
            int zIndex = 0;
            foreach (var p in shadowPieces)
            {
                p.SetValue(Canvas.ZIndexProperty, zIndex);
                zIndex++;
            }
            foreach (var p in pieces)
            {
                p.SetValue(Canvas.ZIndexProperty, zIndex);
                zIndex++;
            }
        }
             
        private bool TrySetCurrentPiecePosition(double newX, double newY)
        {
            bool ret = true;

            //cellX і cellY - позиціонування в пазлових одиницях
            double cellX = (int)((newX) / Width);
            double cellY = (int)((newY) / Height);                        
            
            var q = from p in pieces//фільтруємо шматки
                    where (                            
                            (p.Index != currentSelection.Index) &&//очевидно що їх перевіряти не потрібно
                            (!p.IsSelected) &&                                
                            (((p.X == cellX) && (p.Y == cellY)))                              
                           )
                    select p;

            if (q.Any())//якщо хоча б один шматок підпадає - сюди вставлти не можна
            {
                ret = false;                    
            }            

            return ret;
        }

        //проблема з перевернутим пазлом
        private System.Windows.Point SetCurrentPiecePosition(Piece currentPiece, double newX, double newY)
        {
            double cellX = (int)((newX) / Width);
            double cellY = (int)((newY) / Height);            

            currentPiece.X = cellX;//переписуємо позиціонування пазла
            currentPiece.Y = cellY;

            //якраз таки ставимо пазлик на відповідну позицію
            currentPiece.SetValue(Canvas.LeftProperty, currentPiece.X * Width);
            currentPiece.SetValue(Canvas.TopProperty, currentPiece.Y * Height);

            //цей ставиться туди ж
            shadowPieces[currentPiece.Index].SetValue(Canvas.LeftProperty, currentPiece.X * Width);
            shadowPieces[currentPiece.Index].SetValue(Canvas.TopProperty, currentPiece.Y * Height);

            return new System.Windows.Point(cellX, cellY);
        }
                
        private void SetSelectionRectangle(double x1, double y1, double x2, double y2)
        {            
            double x = (x2 >= x1) ? x1 : x2;
            double y = (y2 >= y1) ? y1 : y2;
            double width = Math.Abs(x2 - x1);
            double height = Math.Abs(y2 - y1);
            rectSelection.Visibility = Visibility.Hidden;
            rectSelection.Width = width;
            rectSelection.Height = height;
            rectSelection.StrokeThickness = 4;
            rectSelection.Stroke = new SolidColorBrush(Colors.Red);
                   
            rectSelection.SetValue(Canvas.LeftProperty, x);
            rectSelection.SetValue(Canvas.TopProperty, y);
        }
             
        //extendable
        private void MouseUp()
        {
            if (currentSelection == null)
            {
                double x1 = (double)rectSelection.GetValue(Canvas.LeftProperty);
                double y1 = (double)rectSelection.GetValue(Canvas.TopProperty);
                double x2 = x1 + rectSelection.Width;
                double y2 = y1 + rectSelection.Height;
                
                int cellX1 = (int)(x1 / Width);
                int cellY1 = (int)(y1 / Height);
                int cellX2 = (int)(x2 / Width);
                int cellY2 = (int)(y2 / Height);

                var query = from p in pieces
                            where
                            (p.X >= cellX1) && (p.X <= cellX2) &&
                            (p.Y >= cellY1) && (p.Y <= cellY2)
                            select p;
                
                foreach (var currentPiece in query)
                {
                    currentSelection = currentPiece;

                    currentPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[currentPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    currentPiece.BitmapEffect = shadowEffect;

                    currentPiece.RenderTransform = stZoomed;
                    currentPiece.IsSelected = true;
                    shadowPieces[currentPiece.Index].RenderTransform = stZoomed;
                }
                SetSelectionRectangle(-1, -1, -1, -1);
            }
            else
            {
                var newX = Mouse.GetPosition(cnvPuzzle).X;
                var newY = Mouse.GetPosition(cnvPuzzle).Y;
                if (TrySetCurrentPiecePosition(newX, newY))
                {                                        
                    currentSelection.BitmapEffect = null;
                    ScaleTransform st = new ScaleTransform() { ScaleX = 1.0, ScaleY = 1.0 };
                    currentSelection.RenderTransform = st;
                    currentSelection.IsSelected = false;
                    shadowPieces[currentSelection.Index].RenderTransform = st;

                    lastCell = SetCurrentPiecePosition(currentSelection, newX, newY);

                    ResetZIndexes();                                       

                    currentSelection = null;

                    if (IsPuzzleCompleted())//кожен раз, коли відпускаємо руку з пазлом, перевіряємо, чи пазл зібраний
                    {
                        allGood.Text = "Complited";
                        allGood.Foreground = System.Windows.Media.Brushes.DarkRed;
                    }
                    else
                    {
                        allGood.Text = "Not Complited";
                        allGood.Foreground = System.Windows.Media.Brushes.Black;
                    }
                }
                selectionAngle = 0;
            }
        }        
        
        //якщо пазл в руці, то відбудеться його поворот
        void piece_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (currentSelection != null)
            {                
                currentSelection.Rotate(90);
                shadowPieces[currentSelection.Index].Rotate(90);
                
                selectionAngle += 90;
                if (selectionAngle == 360)
                    selectionAngle = 0;
            }
        }
        
        void piece_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            alg.IsEnabled = false;
            var chosenPiece = (Piece)sender;

            if (chosenPiece.Parent is WrapPanel)
            {
                if (currentSelection != null)
                {
                    var p = currentSelection;
                    cnvPuzzle.Children.Remove(p);
                    cnvPuzzle.Children.Remove(shadowPieces[p.Index]);

                    var tt = new TranslateTransform() { X = -1, Y = -1 };
                    p.ScaleTransform.ScaleX = 1.0;
                    p.ScaleTransform.ScaleY = 1.0;
                    p.RenderTransform = tt;                    
                    p.IsSelected = false;
                    p.SetValue(Canvas.ZIndexProperty, 0);
                    p.BitmapEffect = null;
                    p.Visibility = Visibility.Visible;
                    pnlPickUp.Children.Add(p);

                    currentSelection = null;
                }
                else//якщо в руці пазла немає, то кладемо в неї пазл, на який наведена мишка
                {
                    pnlPickUp.Children.Remove(chosenPiece);//видалення з панелі вибору
                    cnvPuzzle.Children.Add(shadowPieces[chosenPiece.Index]);//додавання на полотно
                    chosenPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[chosenPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    chosenPiece.BitmapEffect = shadowEffect;
                    chosenPiece.RenderTransform = stZoomed;
                    shadowPieces[chosenPiece.Index].RenderTransform = stZoomed;
                    cnvPuzzle.Children.Add(chosenPiece);
                    chosenPiece.Visibility = Visibility.Hidden;
                    shadowPieces[chosenPiece.Index].Visibility = Visibility.Hidden;
                    chosenPiece.IsSelected = true;
                    currentSelection = chosenPiece;
                }
            }
        }
        
        void cnvPuzzle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            alg.IsEnabled = false;
            MouseUp();
        }
        
        void cnvPuzzle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentSelection != null)
            {                
                currentSelection.Visibility = Visibility.Visible;
                if (shadowPieces.Count > currentSelection.Index)
                    shadowPieces[currentSelection.Index].Visibility = Visibility.Visible;
                
            }
        }                
        
        void cnvPuzzle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            initialRectangleX = Mouse.GetPosition((IInputElement)sender).X;
            initialRectangleY = Mouse.GetPosition((IInputElement)sender).Y;            
            SetSelectionRectangle(initialRectangleX, initialRectangleY, initialRectangleX, initialRectangleY);
        }
        
        void cnvPuzzle_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMoving();
        }
        
        private void MouseMoving()
        {
            var newX = Mouse.GetPosition((IInputElement)cnvPuzzle).X;
            var newY = Mouse.GetPosition((IInputElement)cnvPuzzle).Y;           

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                SetSelectionRectangle(initialRectangleX, initialRectangleY, initialRectangleX, initialRectangleX);//оце якраз той вибір і ті координати
            }            
            else
            {
                if (currentSelection != null)//якщо щось є в руці
                {                                                                

                    //дані пару рядків відпоівдають за позиціонування пазла в повітрі, коли користувач має його в руці                        
                    currentSelection.SetValue(Canvas.LeftProperty, newX);
                    currentSelection.SetValue(Canvas.TopProperty, newY);

                    shadowPieces[currentSelection.Index].SetValue(Canvas.LeftProperty, newX);
                    shadowPieces[currentSelection.Index].SetValue(Canvas.TopProperty, newY);                    
                }
            }
        }

        //мишка перестає бути на полотні
        void cnvPuzzle_MouseLeave(object sender, MouseEventArgs e)
        {
            SetSelectionRectangle(-1, -1, -1, -1);
            if (currentSelection != null)//якщо щось є в руці
            {                
                    var p = currentSelection;
                    cnvPuzzle.Children.Remove(p);
                    cnvPuzzle.Children.Remove(shadowPieces[p.Index]);

                    var tt = new TranslateTransform() { X = 0, Y = 0 };
                    p.ScaleTransform.ScaleX = 1.0;
                    p.ScaleTransform.ScaleY = 1.0;
                    p.RenderTransform = tt;
                    p.X = -1;
                    p.Y = -1;
                    p.IsSelected = false;
                    p.SetValue(Canvas.ZIndexProperty, 0);
                    p.BitmapEffect = null;
                    p.Visibility = System.Windows.Visibility.Visible;
                    pnlPickUp.Children.Add(p);

                    currentSelection = null;
            }
            MouseUp();
        }
        
        private void btnNewPuzzle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                columns = int.Parse(txtColumns.Text);
                rows = int.Parse(txtRows.Text);

                if (columns < 1 || rows < 1)
                {
                    throw new Exception("Innapropriate size.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Check your rows and columns input.\n" + ex.Message);
                return;
            }

            

            var ofd = new OpenFileDialog()
            {
                Filter = "All Image Files ( JPEG,GIF,BMP,PNG)|*.jpg;*.jpeg;*.gif;*.bmp;*.png|JPEG Files ( *.jpg;*.jpeg )|*.jpg;*.jpeg|GIF Files ( *.gif )|*.gif|BMP Files ( *.bmp )|*.bmp|PNG Files ( *.png )|*.png",
                Title = "Select an image file for generating the puzzle"
            };

            bool? result = ofd.ShowDialog(this);
            
            if (result.Value)
            {
                try
                {
                    DestroyReferences();
                    srcFileName = ofd.FileName;
                    using (Stream streamSource = LoadImage(srcFileName))//
                    {
                        CreatePuzzle(streamSource);
                    }
                    btnShowImage.IsEnabled = true;
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
            }

            alg.IsEnabled = true;
        }
        
        private void btnShowImage_Click(object sender, RoutedEventArgs e)
        {
            grdPuzzle.Visibility = Visibility.Hidden;
            scvImage.Visibility = Visibility.Visible;            
            btnShowImage.Visibility = Visibility.Collapsed;
            btnShowPuzzle.Visibility = Visibility.Visible;
        }
        
        private void btnShowPuzzle_Click(object sender, RoutedEventArgs e)
        {            
            grdPuzzle.Visibility = Visibility.Visible;
            scvImage.Visibility = Visibility.Hidden;            
            btnShowImage.Visibility = Visibility.Visible;
            btnShowPuzzle.Visibility = Visibility.Collapsed;
        }                                                                  

        private void algButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.imageSource == null)
            {
                MessageBox.Show("Add some image first");
            }
            else
            {
                alg.IsEnabled = false;
                SetAll();
            }
        }
     
        public enum ViewMode
        { 
            Picture,
            Puzzle
        }

    }
}

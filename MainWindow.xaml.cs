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
        //даний вибір - список всіх кусочків
        Piece currentSelection = null;
        int selectionAngle = 0;
        List<Piece> pieces = new List<Piece>();
        List<Piece> shadowPieces = new List<Piece>();
        int columns;//кілкість колонок, на які все розбивається
        int rows;//кількість рядків
        int Width;//ширина пазла
        int Height;//висота пазла
        double scale = 1.0;//коефіцієнт масштабування
        BitmapImage imageSource;
        string srcFileName = "";
        DropShadowBitmapEffect shadowEffect;//даний об'єкт дозволяє працювати з тінню
        System.Windows.Point lastCell = new System.Windows.Point(-1, 0);
        ScaleTransform stZoomed = new ScaleTransform//трансформація яка відповідає за вибір пазла
        {   ScaleX = 1.1,
            ScaleY = 1.1 
        };

        List<List<List<Pixel>>> chunks = new List<List<List<Pixel>>>();
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
            cnvPuzzle.MouseWheel += new MouseWheelEventHandler(cnvPuzzle_MouseWheel);
            cnvPuzzle.MouseEnter += new MouseEventHandler(cnvPuzzle_MouseEnter);
            cnvPuzzle.MouseLeave += new MouseEventHandler(cnvPuzzle_MouseLeave);            
            
            
            shadowEffect = new DropShadowBitmapEffect()
            {
                Color = Colors.Black,
                Direction = 310,//встановлює кут, по якому тінь проектується
                ShadowDepth = 25,//встановлює відстань між пазлом і його тінню
                Softness = 1,
                Opacity = 0.5
            };
        }


        #region solver

        private int LRCheck(List<List<Pixel>> l, List<List<Pixel>> r)
        {
            int n = Height;
            int res = 0;

            for (int i = 0; i < n; ++i)
            {
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
                        UD[i].Add(0);//такий підхід наразі не приведе до помилки
                        LR[i].Add(0);//
                    }
                }
            }

            return new Tuple<List<List<int>>, List<List<int>>>(UD, LR);
        }

        public double min(double a, double b)
        {
            return a < b ? a : b;
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

        public int max(int a, int b)
        {
            return a > b ? a : b;
        }

        public int min(int a, int b)
        {
            return a < b ? a : b;
        }

        private List<int> Solve(List<List<int>> LR, List<List<int>> UD, double coeff, int start_chunk = 0)
        {
            int m = chunks.Count;

            int mnI = rows - 1, mxI = rows - 1;
            int mnJ = columns - 1, mxJ = columns - 1;

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

            //усі можливі сусіди першого чанка(верхній, правий, ...)
            List<Tuple<int, int>> neighbours = new List<Tuple<int, int>>();
            List<Tuple<int, int, char>> MT = new List<Tuple<int, int, char>>
            { 
                new Tuple<int, int, char>(1, 0, 'D'),//той, що знизу від центрального
                new Tuple<int, int, char>(0, 1, 'R'),//...
                new Tuple<int, int, char>(-1, 0, 'U'),
                new Tuple<int, int, char>(0, -1, 'L'),
            };

            foreach(var i in MT)
            {//виглядає добре
                neighbours.Add(new Tuple<int, int>(rows - 1 + i.Item1, columns - 1 + i.Item2));
            }
            int progress = 1;
            while (progress < m)//допоки всі чанки(пазли) не отримають місце
            {
                List<SortedSet<Tuple<double, int>>> maksym = new List<SortedSet<Tuple<double, int>>>();//даний список тримє в набір всіх можливих вставок для всіх сусідів
                List<Tuple<int, int>> good_neighbours = new List<Tuple<int, int>>();
                foreach (var i in neighbours)
                {
                    //розглядатимуться лише ті випадки, коли пазл не порушує констрейнт розміру картинки
                    if ((i.Item1 - mnI + 1 > rows) || (mxI - i.Item1 + 1 > rows) || (i.Item2 - mnJ + 1 > columns) || (mxJ - i.Item2 + 1 > columns))
                        continue;
                    good_neighbours.Add(i);
                }
                neighbours = good_neighbours;
                double mnCost = 1e18;
                //розглядаються лише "хороші" місця для вставки
                foreach(var i in neighbours)
                {
                    maksym.Add(new SortedSet<Tuple<double, int>>());
                    //для кожного місця перебираються всі чанки, які можна туди поставити
                    for (int ch = 0; ch < m; ++ch)
                    { 
                        if(used[ch])                        
                            continue;
                        long sm = 0;
                        int cnt = 0;

                        foreach(var j in MT)
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
                    mnCost = min(mnCost, maksym.Last().First().Item1);                    
                }
                double mid = mnCost * coeff;
                Tuple<double, double, double> best = new Tuple<double, double, double>(0, 0, 0);
                Tuple<double, double, double> best2 = new Tuple<double, double, double>(0, 0, 0);
                for (int x = 0; x < neighbours.Count; ++x)//ще раз проходимся по всіх можливих місцях вставки
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
                //best - найкращий варіант для вставки x - індекс серед сусідів
                //ch - індекс чанку
                var z = neighbours[(int)best.Item3];
                int I = z.Item1;
                int J = z.Item2;
                int bch = (int)best.Item2;
                mnI = min(mnI, I);
                mxI = max(mxI, I);
                mnJ = min(mnJ, J);
                mxJ = max(mxJ, J);

                ans[I][J] = bch;
                used[bch] = true;
                ++progress;

                //видалення того сусіда
                neighbours.Remove(neighbours.Find(c => c == new Tuple<int, int>(I, J)));

                foreach (var k in MT)
                {
                    int ni = I + k.Item1;
                    int nj = J + k.Item2;
                    if (ni - mnI + 1 > rows || mxI - ni + 1 > rows || nj - mnJ + 1 > columns || mxJ - nj + 1 > columns)
                        continue;
                    if (ans[ni][nj] == -1)
                    {
                        neighbours.Add(new Tuple<int, int>(ni, nj));
                    }
                }                
            }
            
            List<int> perm = new List<int>(m);

            for (int i = 0; i < rows * 2; ++i)
            {
                for (int j = 0; j < columns * 2; ++j)
                {
                    if (ans[j][i] != -1)
                    {
                        perm.Add(ans[j][i]);
                    }
                }
            }

            return perm;
        }

        #endregion

        private void CreatePuzzle(Stream streamSource)
        {
            Random rnd = new Random();                                  
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

            var tpl = Precalc(chunks);
            permResult = Solve(tpl.Item1, tpl.Item2, 1.5);

            var printed = "";
            //for (int i = 0; i < permResult.Count; ++i)
            //{
            //    printed += permResult[i].ToString() + " ";
            //}



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

            for (int iter = 0; iter < columns * rows; ++iter)
            {
                var i = pieces.Where(p => p.Index == permResult[iter]).FirstOrDefault();
                printed += pieces.IndexOf(i).ToString();
            }

            MessageBox.Show(printed);


            
            rectSelection.SetValue(Canvas.ZIndexProperty, 5000);            
            cnvPuzzle.Children.Add(rectSelection);
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

        //скидання всіх можливих прив'язок(потірбне при створенні нового пазлу)
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
        
        //
        private Stream LoadImage(string srcFileName)
        {
            //передається назва файлу в системі
            imageSource = new BitmapImage(new Uri(srcFileName));

            this.Width = (int)imageSource.Width / columns;
            this.Height = (int)imageSource.Height / rows;
            //створюємо якесь нове зображення(копія старого)
            var bi = new BitmapImage(new Uri(srcFileName));
            //створюємо нову кисть, яка буде замальовувати пазли нашим зображенням
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

        private void Algorithm()
        {
            //cnvPuzzle.Children.Clear();//чистимо всіх з полотна
            //pnlPickUp.Children.Clear();//нікого не залишиться на панелі вибору

            //int i = 0;

            //for (int x = 0; x < rows; ++x)
            //{
            //    for (int y = 0; y < columns; ++y)
            //    {
            //        var put = pieces.Where(p => p.Index == i).FirstOrDefault();
            //    }
            //}
        }
        
        private System.Windows.Point SetCurrentPiecePosition(Piece currentPiece, double newX, double newY)
        {
            double cellX = (int)((newX) / Width);//переведення в пазликові одиниці
            double cellY = (int)((newY) / Height);            

            currentPiece.X = cellX;//переписуємо позиціонування пазла
            currentPiece.Y = cellY;

            //якраз таки ставимо пазлик на відповідну позицію
            currentPiece.SetValue(Canvas.LeftProperty, currentPiece.X * Width);
            currentPiece.SetValue(Canvas.TopProperty, currentPiece.Y * Height);

            //робимо це ж саме з тіневим елементом(він ставиться туди ж)
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

                    currentPiece.RenderTransform = stZoomed;//зум даного пазла
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
                        var result = MessageBox.Show("Пазл складено!", "Puzzle Completed", MessageBoxButton.OK, MessageBoxImage.Information);                        
                    }
                }
                selectionAngle = 0;
            }
        }        
        
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
                    //p.X = -1;
                    //p.Y = -1;
                    p.IsSelected = false;
                    p.SetValue(Canvas.ZIndexProperty, 0);
                    p.BitmapEffect = null;
                    p.Visibility = Visibility.Visible;
                    pnlPickUp.Children.Add(p);//додаємо його в панель пікапу

                    currentSelection = null;
                }
                else//якщо в руці пазла немає, то кладемо в неї пазл, на який наведена мишка
                {
                    pnlPickUp.Children.Remove(chosenPiece);//видаляємо його з панелі
                    cnvPuzzle.Children.Add(shadowPieces[chosenPiece.Index]);
                    chosenPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[chosenPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    chosenPiece.BitmapEffect = shadowEffect;
                    chosenPiece.RenderTransform = stZoomed;//збільшуємо зображення
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
        
        void cnvPuzzle_MouseWheel(object sender, MouseWheelEventArgs e)
        {

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

            int cellX = (int)((newX) / Width);
            int cellY = (int)((newY) / Height);

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
        
        private void grdTop_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = WindowStyle.ThreeDBorderWindow;
            grdWindow.RowDefinitions[0].Height = new GridLength(0);
        }
        
        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }
        
        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }                
        
        private void txtMinimize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        
        private void txtMaximize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (txtMaximize.Text == "1")
            {
                txtMaximize.Text = "2";
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                txtMaximize.Text = "1";
                this.WindowState = WindowState.Normal;
            }
        }
        
        private void txtClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();           
        }
     
        public enum ViewMode
        { 
            Picture,
            Puzzle
        }

    }
}

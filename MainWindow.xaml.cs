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
using Microsoft.Win32;

namespace PuzzlesProj
{    
    public partial class MainWindow : Window
    {        
        //шматок пазла, який на даний момент в руці-
        Piece currentSelection;      
        List<Piece> pieces = new List<Piece>();//всі шматки, які є наявними
        List<Piece> shadowPieces = new List<Piece>();//всі тіневі шматки, які є наявними
        int columns;//кілкість колонок, на які все розбивається
        int rows;
        new int Width;//ширина кожного пазла
        new int Height;
        double scale = 1.0;//коефіцієнт масштабування

        //BitmapSource represents a single, constant set of pixels at a certain size and resolution.
        BitmapImage imageSource;//картинка, яка розбивається на пазли
        string srcFileName = "";//розміщення картинки
        DropShadowBitmapEffect shadowEffect;//даний об'єкт дозволяє працювати з тінню
        ScaleTransform stZoomed = new ScaleTransform//масштабування при виборі конкретного пазлу
        {   ScaleX = 1.1,
            ScaleY = 1.1
        };        
        //список матриць пікселів для алгоритму
        List<List<List<Pixel>>> chunks = new List<List<List<Pixel>>>();
        //перестановка, згенерована алгоритмом
        List<int> permResult = new List<int>();

        PngBitmapEncoder png;
        double initialRectangleX = 0;
        double initialRectangleY = 0;        
        System.Windows.Shapes.Rectangle rSelection = new System.Windows.Shapes.Rectangle();
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
        
        private void CreatePuzzle(Stream streamSource)//потік, який має зображення
        {
            zoomSlider.Value = .5;                                          
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
            //List<List<Pixel>> - ряд рядів пікселів
            chunks = new List<List<List<Pixel>>>();

            //перетворення bitmapImage в bitmap
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
                            //отримання кольору для кожного пікселя
                            System.Drawing.Color clr = alg.GetPixel(j * Width +  y, i * Height + x);
                            ch[x].Add(new Pixel(clr.R, clr.G, clr.B));
                        }
                    }

                    chunks.Add(ch);
                }
            }

            rSelection.SetValue(Canvas.ZIndexProperty, 5000);
            cnvPuzzle.Children.Add(rSelection);            

            imgShowImage.Source = imageSource;

            scvImage.Visibility = Visibility.Hidden;
            cnvPuzzle.Visibility = Visibility.Visible;

            var angles = new int[] { 0, 90, 180, 270 };            

            //нарізання пазлу
            int index = 0;
            for (var y = 0; y < rows; ++y)
            {
                for (var x = 0; x < columns; ++x)
                {                                                                             

                    var piece = new Piece(imageSource, x, y, rows, columns, false, index, scale);
                    piece.SetValue(Canvas.ZIndexProperty, 1000 + x * rows + y);
                    
                    piece.MouseLeftButtonUp += new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                    piece.MouseRightButtonUp += new MouseButtonEventHandler(piece_MouseRightButtonUp);                                            

                    //відповідна тінь
                    var shadowPiece = new Piece(imageSource, x, y, rows, columns, false, index, scale);
                    shadowPiece.SetValue(Canvas.ZIndexProperty, x * rows + y);                   
                        
                    pieces.Add(piece);
                    shadowPieces.Add(shadowPiece);
                    index++;             
                }
            }
            

            List<Tuple<List<List<Pixel>>, Piece>> honesty = new List<Tuple<List<List<Pixel>>, Piece>>();
            for (int i = 0; i < pieces.Count; ++i)
            {
                honesty.Add(new Tuple<List<List<Pixel>>, Piece>(chunks[i], pieces[i]));
            }
            
            var shuffled = honesty.OrderBy(i => Guid.NewGuid()).ToList();
            int it = 0;
            //заповнення панелі вибору
            foreach (var p in shuffled)
            {
                Random random = new Random();

                p.Item2.ScaleTransform.ScaleX = 1.0;
                p.Item2.ScaleTransform.ScaleY = 1.0;
                p.Item2.X = -1;
                p.Item2.Y = -1;
                p.Item2.IsSelected = false;

                pnlPickUp.Children.Insert(it++, p.Item2);//заповнюється wrapPanel                                
            }

            for (int i = 0; i < pieces.Count; ++i)
            {
                pieces[i] = shuffled[i].Item2;
            }

            for (int i = 0; i < chunks.Count; ++i)
            {
                chunks[i] = shuffled[i].Item1;
            }

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            ISolver slvr = new Solver(rows, columns, Width, Height);            
            permResult = slvr.GeneratePerm(chunks);
            Mouse.OverrideCursor = null;
            
            //permResult - така перестановка, яка побудувалася, коли на вхід алгоритму прийшли пошафлені дані і на вихід вийшла така перестановка,
            //яку алгоритм вважає складеним пазлом                        
        }

        //складання пазлу в залежності від перестановки, яку видав алгоритм
        private void SetAll()//permResult - перестановка wrapPanel, яку алгоритм вважає оптимальною
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
                }
            }

            if (IsPuzzleCompleted())
            {
                allGood.Text = "Complited";
                allGood.Foreground = System.Windows.Media.Brushes.BlanchedAlmond;
            }
            else
            {
                allGood.Text = "Not Complited";
                allGood.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        //перетворення BitmapImage в Bitmap
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

        //скидання всіх можливих прив'язок, потрібне при створенні нового пазлу
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
            //Represents a single, constant set of pixels at a certain size and resolution.
            //Also optimized for loading images using Extensible Application Markup Language (XAML).
            imageSource = new BitmapImage(new Uri(srcFileName));

            this.Width = (int)imageSource.Width / columns;
            this.Height = (int)imageSource.Height / rows;


            //var bi = new BitmapImage(new Uri(srcFileName));
            //кисть якою замальовується пазл
            var imgBrush = new ImageBrush(imageSource);
            
            imgBrush.AlignmentX = AlignmentX.Left;
            imgBrush.AlignmentY = AlignmentY.Top;
            imgBrush.Stretch = Stretch.None;
                        
            RenderTargetBitmap rtb = new RenderTargetBitmap(columns * Width, rows * Height, 96, 96, PixelFormats.Pbgra32);                        

            var rectImage = new System.Windows.Shapes.Rectangle();
            rectImage.Width = imageSource.PixelWidth;
            rectImage.Height = imageSource.PixelHeight;
            rectImage.HorizontalAlignment = HorizontalAlignment.Left;
            rectImage.VerticalAlignment = VerticalAlignment.Top;
            rectImage.Fill = imgBrush;
            rectImage.Arrange(new Rect(0, 0, imageSource.PixelWidth, imageSource.PixelHeight));            
            
            rtb.Render(rectImage);

            png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));


            var ret = new MemoryStream();

            //переганяє bitmapImage в відповідний потік
            png.Save(ret);

            return ret;
        }

        
        //перевірка на те, чи пазл складено
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
            //cellX і cellY - позиціонування в пазлових одиницях
            double cellX = (int)((newX) / Width);
            double cellY = (int)((newY) / Height);                        
            
            var q = from p in pieces//фільтруємо шматки
                    where (                            
                            (p.Index != currentSelection.Index) &&
                            (!p.IsSelected) &&                                
                            (((p.X == cellX) && (p.Y == cellY)))                              
                           )
                    select p;

            if (q.Any())            
                return false;            

            return true;
        }
        
        //позиціонування відповідного пазла на полотні
        private System.Windows.Point SetCurrentPiecePosition(Piece currentPiece, double newX, double newY)
        {
            double cellX = (int)((newX) / Width);//позиції вставки
            double cellY = (int)((newY) / Height);//в пазлових одиницях

            currentPiece.X = cellX;//переписуємо позиціонування пазла
            currentPiece.Y = cellY;

            //якраз таки ставимо пазлик на відповідну позицію
            currentPiece.SetValue(Canvas.LeftProperty, currentPiece.X * Width);
            currentPiece.SetValue(Canvas.TopProperty, currentPiece.Y * Height);
            
            shadowPieces[currentPiece.Index].SetValue(Canvas.LeftProperty, currentPiece.X * Width);
            shadowPieces[currentPiece.Index].SetValue(Canvas.TopProperty, currentPiece.Y * Height);

            return new System.Windows.Point(cellX, cellY);
        }
        
        
        private void SetSelectionRectangle(double x1, double y1, double x2, double y2)
        {            
            double x = (x2 >= x1) ? x1 : x2;//верхня точка
            double y = (y2 >= y1) ? y1 : y2;//
            double width = Math.Abs(x2 - x1);
            double height = Math.Abs(y2 - y1);
            rSelection.Visibility = Visibility.Hidden;
            rSelection.Width = width;
            rSelection.Height = height;            
                   
            rSelection.SetValue(Canvas.LeftProperty, x);
            rSelection.SetValue(Canvas.TopProperty, y);
        }
             
        //extendable
        private new void MouseUp()
        {
            if (currentSelection == null)//якщо рука пуста, то в неї береться пазл
            {
                //оце можна замінити точкою
                double x1 = (double)rSelection.GetValue(Canvas.LeftProperty);
                double y1 = (double)rSelection.GetValue(Canvas.TopProperty);
                double x2 = x1 + rSelection.Width;
                double y2 = y1 + rSelection.Height;
                
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
            else//якщо ж непуста, то пазл ставться
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

                    SetCurrentPiecePosition(currentSelection, newX, newY);
                    ResetZIndexes();                                       

                    currentSelection = null;

                    if (IsPuzzleCompleted())//кожен раз, коли відпускається рука з пазлом, перевіряється, чи пазл зібраний
                    {
                        allGood.Text = "Complited";
                        allGood.Foreground = System.Windows.Media.Brushes.BlanchedAlmond;
                    }
                    else
                    {
                        allGood.Text = "Not Complited";
                        allGood.Foreground = System.Windows.Media.Brushes.Black;
                    }
                }                
            }
        }        
        
        //якщо пазл в руці, то відбудеться його поворот
        void piece_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (currentSelection != null)
            {                
                currentSelection.Rotate(90);
                shadowPieces[currentSelection.Index].Rotate(90);                                
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
        
        //підбирання шматка з Canvas
        void cnvPuzzle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            allGood.Text = "Not Complited";
            allGood.Foreground = System.Windows.Media.Brushes.Black;
            alg.IsEnabled = false;
            MouseUp();
        }
        
        void cnvPuzzle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentSelection != null)
            {                
                currentSelection.Visibility = Visibility.Visible;                
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
                Title = "Select image for a puzzle"
            };

            bool? result = ofd.ShowDialog(this);
            
            if (result.Value)
            {
                try
                {
                    DestroyReferences();
                    srcFileName = ofd.FileName;
                    using (Stream streamSource = LoadImage(srcFileName))
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
    }
}
